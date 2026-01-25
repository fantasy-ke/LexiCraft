<script lang="ts" setup>
import {onMounted, onUnmounted, provide, ref, watch} from 'vue'

import Statistics from '@/components/word/components/Statistics.vue'
import {emitter, EventKey, useEvents} from '@/utils/eventBus.ts'
import {useSettingStore} from '@/stores/setting.ts'
import {useRuntimeStore} from '@/stores/runtime.ts'
import type {Dict, PracticeData, TaskWords, Word} from '@/types/types.ts'
import {useDisableEventListener, useOnKeyboardEventListener, useStartKeyboardEventListener} from '@/hooks/event.ts'
import useTheme from '@/hooks/theme.ts'
import {getCurrentStudyWord, useWordOptions} from '@/hooks/dict.ts'
import {_getDictDataByUrl, _nextTick, cloneDeep, isMobile, loadJsLib, resourceWrap, shuffle} from '@/utils'
import {useRoute, useRouter} from 'vue-router'
import Footer from '@/components/word/components/Footer.vue'
import Panel from '@/components/Panel.vue'
import BaseIcon from '@/components/BaseIcon.vue'
import Tooltip from '@/components/base/Tooltip.vue'
import WordList from '@/components/list/WordList.vue'
import TypeWord from '@/components/word/components/TypeWord.vue'
import Empty from '@/components/Empty.vue'
import {useBaseStore} from '@/stores/base.ts'
import {usePracticeStore} from '@/stores/practice.ts'
import Toast from '@/components/base/toast/Toast.ts'
import {getDefaultDict, getDefaultWord} from '@/types/func.ts'
import ConflictNotice from '@/components/ConflictNotice.vue'
import PracticeLayout from '@/components/PracticeLayout.vue'
import ShortcutHints from '@/components/ShortcutHints.vue'

import {AppEnv, DICT_LIST, LIB_JS_URL, TourConfig, WordPracticeModeStageMap} from '@/config/env.ts'
import {ToastInstance} from '@/components/base/toast/type.ts'
import {watchOnce} from '@vueuse/core'
import {setUserDictProp} from '@/apis'
import GroupList from '@/components/word/components/GroupList.vue'
import {getPracticeWordCache, setPracticeWordCache} from '@/utils/cache.ts'
import {ShortcutKey, WordPracticeMode, WordPracticeStage, WordPracticeType} from '@/types/enum.ts'

const {isWordCollect, toggleWordCollect, isWordSimple, toggleWordSimple} = useWordOptions()
const settingStore = useSettingStore()
const runtimeStore = useRuntimeStore()
const {toggleTheme} = useTheme()
const router = useRouter()
const route = useRoute()
const store = useBaseStore()
const statStore = usePracticeStore()
const typingRef: any = $ref()
let showConflictNotice = $ref(false)
let allWrongWords = new Set()
let showStatDialog = $ref(false)
let loading = $ref(false)
let timer = $ref(0)
let isFocus = true
let taskWords = $ref<TaskWords>({
  new: [],
  review: [],
  write: [],
  shuffle: [],
})

let data = $ref<PracticeData>({
  index: 0,
  words: [],
  wrongWords: [],
  excludeWords: [],
})
let isTypingWrongWord = ref(false)

provide('isTypingWrongWord', isTypingWrongWord)
provide('practiceData', data)
provide('practiceTaskWords', taskWords)

async function loadDict() {
  // console.log('load好了开始加载')
  let dict = getDefaultDict()
  let dictId = route.params.id
  if (dictId) {
    //先在自己的词典列表里面找，如果没有再在资源列表里面找
    dict = store.word.bookList.find(v => v.id === dictId)
    let r = await fetch(resourceWrap(DICT_LIST.WORD.ALL))
    let dict_list = await r.json()
    if (!dict) dict = dict_list.flat().find(v => v.id === dictId) as Dict
    if (dict && dict.id) {
      //如果是不是自定义词典，就请求数据
      if (!dict.custom) dict = await _getDictDataByUrl(dict)
      if (!dict.words.length) {
        router.push('/word')
        return Toast.warning('没有单词可学习！')
      }
      store.changeDict(dict)
      initData(getCurrentStudyWord(), true)
      loading = false
    } else {
      router.push('/word')
    }
  } else {
    router.push('/word')
  }
}

watch(
    () => store.load,
    n => {
      if (n && loading) loadDict()
    },
    {immediate: true}
)

onMounted(() => {
  //如果是从单词学习主页过来的，就直接使用；否则等待加载
  if (runtimeStore.routeData) {
    initData(runtimeStore.routeData.taskWords, true)
  } else {
    loading = true
  }
  if (route.query.guide) {
    showConflictNotice = false
  } else {
    showConflictNotice = true
  }
  document.addEventListener('visibilitychange', () => {
    isFocus = !document.hidden
  })
})

onUnmounted(() => {
  let cache = getPracticeWordCache()
  if (cache) {
    savePracticeData()
  }
  timer && clearInterval(timer)
})

watchOnce(
    () => data.words.length,
    (newVal, oldVal) => {
      //如果是从无值变有值，代表是开始
      if (!oldVal && newVal) {
        _nextTick(async () => {
          const Shepherd = await loadJsLib('Shepherd', LIB_JS_URL.SHEPHERD)
          const tour = new Shepherd.Tour(TourConfig)
          tour.on('cancel', () => {
            localStorage.setItem('tour-guide', '1')
          })
          tour.addStep({
            id: 'step5',
            text: '这里可以练习拼写单词，只需要按下键盘上对应的按键即可，没有输入框！',
            attachTo: {element: '#word', on: 'bottom'},
            buttons: [
              {
                text: `下一步（5/${TourConfig.total}）`,
                action: tour.next,
              },
            ],
          })

          tour.addStep({
            id: 'step6',
            text: '这里是文章练习',
            attachTo: {element: '#article', on: 'top'},
            buttons: [
              {
                text: `下一步（6/${TourConfig.total}）`,
                action() {
                  tour.next()
                  router.push('/articles')
                },
              },
            ],
          })

          const r = localStorage.getItem('tour-guide')
          if (settingStore.first && !r && !isMobile()) {
            tour.start()
          }
        }, 500)
      }
    }
)

useStartKeyboardEventListener()
useDisableEventListener(() => loading)

function initData(initVal: TaskWords, init: boolean = false) {
  let d = getPracticeWordCache()
  if (d && init) {
    taskWords = Object.assign(taskWords, d.taskWords)
    //这里直接赋值的话，provide后的inject获取不到最新值
    data = Object.assign(data, d.practiceData)
    statStore.$patch(d.statStoreData)
  } else {
    // taskWords = initVal
    //不能直接赋值，会导致 inject 的数据为默认值
    taskWords = Object.assign(taskWords, initVal)

    if (settingStore.wordPracticeMode === WordPracticeMode.Shuffle) {
      settingStore.wordPracticeType = WordPracticeType.Dictation
      data.words = taskWords.shuffle
      statStore.stage = WordPracticeStage.Shuffle
      statStore.total = taskWords.shuffle.length
      statStore.newWordNumber = 0
      statStore.reviewWordNumber = 0
      statStore.writeWordNumber = statStore.total
    } else if (settingStore.wordPracticeMode === WordPracticeMode.Review) {
      if (taskWords.review.length) {
        data.words = taskWords.review
        statStore.stage = WordPracticeStage.IdentifyReview
      } else if (taskWords.write.length) {
        data.words = taskWords.write
        statStore.stage = WordPracticeStage.IdentifyReviewAll
      }
      statStore.total = taskWords.review.length + taskWords.write.length
      statStore.newWordNumber = 0
      statStore.reviewWordNumber = taskWords.review.length
      statStore.writeWordNumber = taskWords.write.length
    } else {
      if (taskWords.new.length === 0) {
        if (taskWords.review.length) {
          data.words = taskWords.review
          if (settingStore.wordPracticeMode === WordPracticeMode.System) {
            statStore.stage = WordPracticeStage.IdentifyReview
          } else if (settingStore.wordPracticeMode === WordPracticeMode.Free) {
            statStore.stage = WordPracticeModeStageMap[settingStore.wordPracticeMode][0]
          } else if (settingStore.wordPracticeMode === WordPracticeMode.IdentifyOnly) {
            statStore.stage = WordPracticeStage.IdentifyReview
          } else if (settingStore.wordPracticeMode === WordPracticeMode.DictationOnly) {
            statStore.stage = WordPracticeStage.DictationReview
          } else if (settingStore.wordPracticeMode === WordPracticeMode.ListenOnly) {
            statStore.stage = WordPracticeStage.ListenReview
          }
        } else {
          if (taskWords.write.length) {
            data.words = taskWords.write
            if (settingStore.wordPracticeMode === WordPracticeMode.System) {
              statStore.stage = WordPracticeStage.IdentifyReviewAll
            } else if (settingStore.wordPracticeMode === WordPracticeMode.Free) {
              statStore.stage = WordPracticeModeStageMap[settingStore.wordPracticeMode][0]
            } else if (settingStore.wordPracticeMode === WordPracticeMode.IdentifyOnly) {
              statStore.stage = WordPracticeStage.IdentifyReviewAll
            } else if (settingStore.wordPracticeMode === WordPracticeMode.DictationOnly) {
              statStore.stage = WordPracticeStage.DictationReviewAll
            } else if (settingStore.wordPracticeMode === WordPracticeMode.ListenOnly) {
              statStore.stage = WordPracticeStage.ListenReviewAll
            }
          } else {
            Toast.warning('没有可学习的单词！')
            router.push('/word')
          }
        }
      } else {
        data.words = taskWords.new
        statStore.stage = WordPracticeModeStageMap[settingStore.wordPracticeMode][0]
      }
      statStore.total = taskWords.review.length + taskWords.new.length + taskWords.write.length
      statStore.newWordNumber = taskWords.new.length
      statStore.reviewWordNumber = taskWords.review.length
      statStore.writeWordNumber = taskWords.write.length
    }

    data.index = 0
    data.wrongWords = []
    data.excludeWords = []
    allWrongWords.clear()
    statStore.startDate = Date.now()
    statStore.inputWordNumber = 0
    statStore.wrong = 0
    statStore.spend = 0
    isTypingWrongWord.value = false
  }
  clearInterval(timer)
  timer = setInterval(() => {
    if (isFocus) {
      statStore.spend += 1000
    }
  }, 1000)
}

const word = $computed<Word>(() => {
  return data.words[data.index] ?? getDefaultWord()
})
const prevWord: Word = $computed(() => {
  return data.words?.[data.index - 1] ?? undefined
})
const nextWord: Word = $computed(() => {
  return data.words?.[data.index + 1] ?? undefined
})

watch(
    () => settingStore.wordPracticeType,
    n => {
      if (settingStore.wordPracticeMode === WordPracticeMode.Free) return
      switch (n) {
        case WordPracticeType.Spell:
        case WordPracticeType.Dictation:
          settingStore.dictation = true
          settingStore.translate = true
          break
        case WordPracticeType.Listen:
          settingStore.dictation = true
          settingStore.translate = false
          break
        case WordPracticeType.FollowWrite:
          settingStore.dictation = false
          settingStore.translate = true
          break
        case WordPracticeType.Identify:
          settingStore.dictation = false
          settingStore.translate = false
          break
      }
    },
    {immediate: true}
)

watch(
    () => statStore.stage,
    n => {
      switch (n) {
        case WordPracticeStage.DictationNewWord:
        case WordPracticeStage.DictationReview:
        case WordPracticeStage.DictationReviewAll:
        case WordPracticeStage.Shuffle:
          settingStore.wordPracticeType = WordPracticeType.Dictation
          break
        case WordPracticeStage.ListenNewWord:
        case WordPracticeStage.ListenReview:
        case WordPracticeStage.ListenReviewAll:
          settingStore.wordPracticeType = WordPracticeType.Listen
          break
        case WordPracticeStage.FollowWriteNewWord:
        case WordPracticeStage.FollowWriteReview:
        case WordPracticeStage.FollowWriteReviewAll:
          settingStore.wordPracticeType = WordPracticeType.FollowWrite
          break
        case WordPracticeStage.IdentifyNewWord:
        case WordPracticeStage.IdentifyReview:
        case WordPracticeStage.IdentifyReviewAll:
          settingStore.wordPracticeType = WordPracticeType.Identify
          break
      }
    },
    {immediate: true}
)

const groupSize = 7

function wordLoop() {
  // 学习模式
  if (settingStore.wordPracticeType === WordPracticeType.FollowWrite) {
    data.index++
    // 到达一个组末尾，就切换到拼写模式
    if (data.index % groupSize === 0) {
      settingStore.wordPracticeType = WordPracticeType.Spell
      data.index -= groupSize // 回到刚学单词开头
    }
  } else {
    // 拼写模式
    data.index++
    // 拼写走完一组，切回跟写模式
    if (data.index % groupSize === 0) {
      settingStore.wordPracticeType = WordPracticeType.FollowWrite
    }
  }
}

let toastInstance: ToastInstance = null

function nextStage(originList: Word[], log: string = '', toast: boolean = false) {
  //每次都判断，因为每次都可能新增已掌握的单词
  let list = originList.filter(v => !data.excludeWords.includes(v.word))
  console.log(log)
  statStore.stage = statStore.nextStage
  if (list.length) {
    if (toast) {
      if (toastInstance) toastInstance.close()
      toastInstance = Toast.info('输入完成后按空格键切换下一个', {duration: 5000})
    }
    data.words = list
    data.index = 0
  } else {
    console.log(log + ':无单词略过')
    next(false)
  }
}

async function next(isTyping: boolean = true) {
  debugger
  if (isTyping) statStore.inputWordNumber++
  if (settingStore.wordPracticeMode === WordPracticeMode.Free) {
    if (data.index === data.words.length - 1) {
      data.wrongWords = data.wrongWords.filter(v => !data.excludeWords.includes(v.word))
      if (data.wrongWords.length) {
        isTypingWrongWord.value = true
        settingStore.wordPracticeType = WordPracticeType.FollowWrite
        console.log('当前学完了，但还有错词')
        data.words = shuffle(cloneDeep(data.wrongWords))
        data.index = 0
        data.wrongWords = []
      } else {
        console.log('自由模式，全完学完了')
        showStatDialog = true
        clearInterval(timer)
        setTimeout(() => setPracticeWordCache(null), 300)
      }
    } else {
      data.index++
    }
  } else {
    if (data.index === data.words.length - 1) {
      //如果手动敲的，才轮询
      if ((statStore.stage === WordPracticeStage.FollowWriteNewWord || isTypingWrongWord.value) && isTyping) {
        if (settingStore.wordPracticeType !== WordPracticeType.Spell) {
          //回到最后一组的开始位置
          data.index = Math.floor(data.index / groupSize) * groupSize
          emitter.emit(EventKey.resetWord)
          settingStore.wordPracticeType = WordPracticeType.Spell
          //如果单词是已掌握的，则跳过
          if (isWordSimple(word)) next(false)
          return
        }
      }
      data.wrongWords = data.wrongWords.filter(v => !data.excludeWords.includes(v.word))
      if (data.wrongWords.length) {
        isTypingWrongWord.value = true
        settingStore.wordPracticeType = WordPracticeType.FollowWrite
        console.log('当前学完了，但还有错词')
        data.words = shuffle(cloneDeep(data.wrongWords))
        data.index = 0
        data.wrongWords = []
      } else {
        isTypingWrongWord.value = false
        console.log('当前学完了，没错词', statStore.total, statStore.stage, data.index)

        const complete = () => {
          console.log('全完学完了')
          showStatDialog = true
          clearInterval(timer)
          setTimeout(() => setPracticeWordCache(null), 300)
        }

        if (settingStore.wordPracticeMode === WordPracticeMode.System) {
          if (statStore.stage === WordPracticeStage.FollowWriteNewWord) {
            nextStage(shuffle(taskWords.new), '开始听写新词', true)
          } else if (statStore.stage === WordPracticeStage.ListenNewWord) {
            nextStage(shuffle(taskWords.new), '开始默写新词')
          } else if (statStore.stage === WordPracticeStage.DictationNewWord) {
            nextStage(taskWords.review, '开始自测昨日')
          } else if (statStore.stage === WordPracticeStage.IdentifyReview) {
            nextStage(shuffle(taskWords.review), '开始听写上次', true)
          } else if (statStore.stage === WordPracticeStage.ListenReview) {
            nextStage(shuffle(taskWords.review), '开始默写上次')
          } else if (statStore.stage === WordPracticeStage.DictationReview) {
            nextStage(taskWords.write, '开始自测之前')
          } else if (statStore.stage === WordPracticeStage.IdentifyReviewAll) {
            nextStage(shuffle(taskWords.write), '开始听写之前', true)
          } else if (statStore.stage === WordPracticeStage.ListenReviewAll) {
            nextStage(shuffle(taskWords.write), '开始默写之前')
          } else if (statStore.stage === WordPracticeStage.DictationReviewAll) {
            complete()
          }
        } else if (settingStore.wordPracticeMode === WordPracticeMode.ListenOnly) {
          if (statStore.stage === WordPracticeStage.ListenNewWord) {
            nextStage(taskWords.review, '开始听写昨日', true)
          } else if (statStore.stage === WordPracticeStage.ListenReview) {
            nextStage(taskWords.write, '开始听写之前')
          } else if (statStore.stage === WordPracticeStage.ListenReviewAll) complete()
        } else if (settingStore.wordPracticeMode === WordPracticeMode.DictationOnly) {
          if (statStore.stage === WordPracticeStage.DictationNewWord) {
            nextStage(taskWords.review, '开始默写昨日', true)
          } else if (statStore.stage === WordPracticeStage.DictationReview) {
            nextStage(taskWords.write, '开始默写之前')
          } else if (statStore.stage === WordPracticeStage.DictationReviewAll) complete()
        } else if (settingStore.wordPracticeMode === WordPracticeMode.IdentifyOnly) {
          if (statStore.stage === WordPracticeStage.IdentifyNewWord) {
            nextStage(taskWords.review, '开始自测昨日')
          } else if (statStore.stage === WordPracticeStage.IdentifyReview) {
            nextStage(taskWords.write, '开始自测之前')
          } else if (statStore.stage === WordPracticeStage.IdentifyReviewAll) complete()
        } else if (settingStore.wordPracticeMode === WordPracticeMode.Shuffle) {
          if (statStore.stage === WordPracticeStage.Shuffle) complete()
        } else if (settingStore.wordPracticeMode === WordPracticeMode.Review) {
          if (statStore.stage === WordPracticeStage.IdentifyReview) {
            nextStage(shuffle(taskWords.review), '开始听写昨日', true)
          } else if (statStore.stage === WordPracticeStage.ListenReview) {
            nextStage(shuffle(taskWords.review), '开始默写昨日')
          } else if (statStore.stage === WordPracticeStage.DictationReview) {
            nextStage(taskWords.write, '开始自测之前')
          } else if (statStore.stage === WordPracticeStage.IdentifyReviewAll) {
            nextStage(shuffle(taskWords.write), '开始听写之前', true)
          } else if (statStore.stage === WordPracticeStage.ListenReviewAll) {
            nextStage(shuffle(taskWords.write), '开始默写之前')
          } else if (statStore.stage === WordPracticeStage.DictationReviewAll) complete()
        }
      }
    } else {
      if (statStore.stage === WordPracticeStage.FollowWriteNewWord) {
        wordLoop()
      } else {
        if (isTypingWrongWord.value) wordLoop()
        else data.index++
      }
    }
  }
  //如果单词是已掌握的，则跳过
  if (isWordSimple(word)) next(false)
}

function skipStep() {
  data.index = data.words.length - 1
  data.wrongWords = []
  next(false)
}

function onWordKnow() {
  //标记模式时，用户认识的单词加入到排除里面，后续不再复习
  let rIndex = data.excludeWords.findIndex(v => v === word.word)
  if (rIndex < 0) {
    data.excludeWords.push(word.word)
  }
}

function onTypeWrong() {
  let temp = word.word.toLowerCase()
  if (!allWrongWords.has(word.word.toLowerCase())) {
    allWrongWords.add(word.word.toLowerCase())
    statStore.wrong++
  }
  if (!store.wrong.words.find((v: Word) => v.word.toLowerCase() === temp)) {
    store.wrong.words.push(word)
    store.wrong.length = store.wrong.words.length
  }
  if (!data.wrongWords.find((v: Word) => v.word.toLowerCase() === temp)) {
    data.wrongWords.push(word)
  }
  savePracticeData()
}

function savePracticeData() {
  setPracticeWordCache({
    taskWords,
    practiceData: data,
    statStoreData: statStore.$state,
  })
}

watch(() => data.index, savePracticeData)

function onKeyUp(e: KeyboardEvent) {
  // console.log('onKeyUp', e)
  typingRef.hideWord()
}

function onKeyDown(e: KeyboardEvent) {
  // console.log('onKeyDown', e)
  switch (e.key) {
    case 'Backspace':
      typingRef.del()
      break
  }
}

useOnKeyboardEventListener(onKeyDown, onKeyUp)

function repeat() {
  console.log('重学一遍')
  setPracticeWordCache(null)
  let temp = cloneDeep(taskWords)
  let ignoreList = [store.allIgnoreWords, store.knownWords][settingStore.ignoreSimpleWord ? 0 : 1]
  //随机练习单独处理
  if (settingStore.wordPracticeMode === WordPracticeMode.Shuffle) {
    temp.shuffle = shuffle(temp.shuffle.filter(v => !ignoreList.includes(v.word)))
  } else {
    //将学习进度减回去
    store.sdict.lastLearnIndex = store.sdict.lastLearnIndex - statStore.newWordNumber
    //排除已掌握单词
    temp.new = temp.new.filter(v => !ignoreList.includes(v.word))
    temp.review = temp.review.filter(v => !ignoreList.includes(v.word))
    temp.write = temp.write.filter(v => !ignoreList.includes(v.word))
  }
  emitter.emit(EventKey.resetWord)
  initData(temp)
}

function prev() {
  if (data.index === 0) {
    Toast.warning('已经是第一个了~')
  } else {
    data.index--
  }
}

function skip(e: KeyboardEvent) {
  next(false)
  // e.preventDefault()
}

function show(e: KeyboardEvent) {
  if (![WordPracticeType.FollowWrite].includes(settingStore.wordPracticeType)) onTypeWrong()
  typingRef.showWord()
}

function collect(e: KeyboardEvent) {
  toggleWordCollect(word)
}

function play() {
  typingRef.play()
}

function toggleWordSimpleWrapper() {
  if (!isWordSimple(word)) {
    //延迟一下，不知道为什么不延迟会导致当前条目不自动定位到列表中间
    setTimeout(() => next(false))
  }
  let rIndex = data.excludeWords.findIndex(v => v === word.word)
  if (rIndex > -1) {
    data.excludeWords.splice(rIndex, 1)
  } else {
    data.excludeWords.push(word.word)
  }
  toggleWordSimple(word)
}

function toggleTranslate() {
  settingStore.translate = !settingStore.translate
}

function toggleDictation() {
  settingStore.dictation = !settingStore.dictation
}

function toggleConciseMode() {
  settingStore.showToolbar = !settingStore.showToolbar
  settingStore.showPanel = settingStore.showToolbar
}

function togglePanel() {
  settingStore.showPanel = !settingStore.showPanel
}

async function continueStudy() {
  setPracticeWordCache(null)
  let temp = cloneDeep(taskWords)
  let ignoreList = [store.allIgnoreWords, store.knownWords][settingStore.ignoreSimpleWord ? 0 : 1]

  //随机练习单独处理
  if (settingStore.wordPracticeMode === WordPracticeMode.Shuffle) {
    temp.shuffle = shuffle(store.sdict.words.filter(v => !ignoreList.includes(v.word))).slice(
        0,
        runtimeStore.routeData.total ?? temp.shuffle.length
    )
    if (showStatDialog) showStatDialog = false
  } else {
    //这里判断是否显示结算弹框，如果显示了结算弹框的话，就不用加进度了
    if (!showStatDialog) {
      console.log('没学完，强行跳过')
      store.sdict.lastLearnIndex = store.sdict.lastLearnIndex + statStore.newWordNumber
      // 忽略单词数
      const ignoreCount = ignoreList.filter(word => store.sdict.words.some(w => w.word.toLowerCase() === word)).length
      // 如果lastLearnIndex已经超过可学单词数，则判定完成
      if (store.sdict.lastLearnIndex + ignoreCount >= store.sdict.length) {
        store.sdict.complete = true
        store.sdict.lastLearnIndex = store.sdict.length
      }
    } else {
      console.log('学完了，正常下一组')
      showStatDialog = false
    }

    temp = getCurrentStudyWord()
  }
  emitter.emit(EventKey.resetWord)
  initData(temp)

  if (AppEnv.CAN_REQUEST) {
    let res = await setUserDictProp(null, {...store.sdict, type: 'word'})
    if (!res.success) {
      Toast.error(res.msg)
    }
  }
}

async function jumpToGroup(group: number) {
  setPracticeWordCache(null)
  console.log('没学完，强行跳过', group)
  store.sdict.lastLearnIndex = (group - 1) * store.sdict.perDayStudyNumber
  emitter.emit(EventKey.resetWord)
  initData(getCurrentStudyWord())

  if (AppEnv.CAN_REQUEST) {
    let res = await setUserDictProp(null, {...store.sdict, type: 'word'})
    if (!res.success) {
      Toast.error(res.msg)
    }
  }
}

function randomWrite() {
  console.log('随机默写')
  data.words = shuffle(data.words)
  data.index = 0
  settingStore.dictation = true
}

useEvents([
  [EventKey.repeatStudy, repeat],
  [EventKey.continueStudy, continueStudy],
  [ShortcutKey.ShowWord, show],
  [ShortcutKey.Previous, prev],
  [ShortcutKey.Next, skip],
  [ShortcutKey.ToggleCollect, collect],
  [ShortcutKey.ToggleSimple, toggleWordSimpleWrapper],
  [ShortcutKey.PlayWordPronunciation, play],

  [ShortcutKey.RepeatChapter, repeat],
  [ShortcutKey.NextChapter, continueStudy],
  [ShortcutKey.ToggleShowTranslate, toggleTranslate],
  [ShortcutKey.ToggleDictation, toggleDictation],
  [ShortcutKey.ToggleTheme, toggleTheme],
  [ShortcutKey.ToggleConciseMode, toggleConciseMode],
  [ShortcutKey.TogglePanel, togglePanel],
  [ShortcutKey.RandomWrite, randomWrite],
])
</script>

<template>
  <PracticeLayout v-loading="loading" maxWidth="var(--toolbar-width)">
    <template v-slot:hints>
      <ShortcutHints
          :hints="[
          { key: ShortcutKey.PlayWordPronunciation, label: '播音' },
          { key: ShortcutKey.ToggleSimple, label: '掌握' },
          { key: ShortcutKey.ToggleCollect, label: '生词' },
          { key: ShortcutKey.Next, label: '跳过' },
          { key: ShortcutKey.ShowWord, label: '提示' },
        ]"
      />
    </template>
    <!-- 导航区域 -->
    <template v-slot:navigation>
      <div
          v-if="settingStore.showNearWord"
          class="navigation-buttons"
      >
        <div v-if="prevWord" class="nav-btn nav-btn-prev" @click="prev">
          <IconFluentArrowLeft16Regular class="nav-arrow" width="22"/>
          <Tooltip :title="`上一个(${settingStore.shortcutKeyMap[ShortcutKey.Previous]})`">
            <div class="nav-word">{{ prevWord.word }}</div>
          </Tooltip>
        </div>
        <div v-else class="nav-placeholder"></div>

        <div v-if="nextWord" class="nav-btn nav-btn-next" @click="next(false)">
          <Tooltip :title="`下一个(${settingStore.shortcutKeyMap[ShortcutKey.Next]})`">
            <div :class="settingStore.dictation && 'word-shadow'" class="nav-word">
              {{ nextWord.word }}
            </div>
          </Tooltip>
          <IconFluentArrowRight16Regular class="nav-arrow" width="22"/>
        </div>
        <div v-else class="nav-placeholder"></div>
      </div>
    </template>

    <!-- 练习内容区域 -->
    <template v-slot:practice>
      <div class="practice-word">
        <TypeWord ref="typingRef" :word="word" @complete="next" @know="onWordKnow" @wrong="onTypeWrong"/>
      </div>
    </template>
    <template v-slot:panel>
      <Panel>
        <template v-slot:title>
          <div class="center gap-1">
            <span>{{ store.sdict.name }}</span>

            <GroupList
                v-if="taskWords.new.length && settingStore.wordPracticeMode !== WordPracticeMode.Shuffle"
                @click="jumpToGroup"
            />
            <BaseIcon
                v-if="
                taskWords.new.length &&
                ![WordPracticeMode.Review, WordPracticeMode.Shuffle].includes(settingStore.wordPracticeMode)
              "
                :title="`下一组(${settingStore.shortcutKeyMap[ShortcutKey.NextChapter]})`"
                @click="continueStudy"
            >
              <IconFluentArrowRight16Regular class="arrow" width="22"/>
            </BaseIcon>

            <BaseIcon :title="`随机默写(${settingStore.shortcutKeyMap[ShortcutKey.RandomWrite]})`" @click="randomWrite">
              <IconFluentArrowShuffle16Regular class="arrow" width="22"/>
            </BaseIcon>
          </div>
        </template>
        <div class="panel-page-item pl-4">
          <WordList
              v-if="data.words.length"
              :activeIndex="data.index"
              :is-active="settingStore.showPanel"
              :list="data.words"
              :show-translate="settingStore.translate"
              :show-word="!settingStore.dictation"
              :static="false"
              @click="(val: any) => (data.index = val.index)"
          >
          </WordList>
          <Empty v-else/>
        </div>
      </Panel>
    </template>
    <template v-slot:footer>
      <Footer
          :is-collect="isWordCollect(word)"
          :is-simple="isWordSimple(word)"
          @skip="next(false)"
          @skipStep="skipStep"
          @toggle-simple="toggleWordSimpleWrapper"
          @toggle-collect="toggleWordCollect(word)"
      />
    </template>
  </PracticeLayout>
  <Statistics v-model="showStatDialog"/>
  <ConflictNotice v-if="showConflictNotice"/>
</template>

<style lang="scss" scoped>
.practice-wrapper {
  width: 100%;
  height: 100vh;
  display: flex;
  justify-content: center;
  overflow: hidden;
}

// 练习单词容器
.practice-word {
  width: 100%;
  flex: 1;
  display: flex;
  flex-direction: column;
  position: relative;
}

// 导航按钮样式 - 分布在两侧，保持稳定高度
.navigation-buttons {
  display: flex;
  justify-content: space-between; // 改为两侧分布
  align-items: center;
  width: 100%;
  max-width: var(--toolbar-width);
  height: 60px;
  padding: 0 1rem; // 添加内边距，防止贴边
  box-sizing: border-box;
}

.nav-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: rgba(255, 255, 255, 0.9);
  border: 1px solid rgba(0, 0, 0, 0.1);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  backdrop-filter: blur(10px);

  &:hover {
    background: rgba(255, 255, 255, 0.95);
    border-color: rgba(0, 0, 0, 0.2);
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  }
}

.nav-btn-prev {
  // 左侧按钮样式
}

.nav-btn-next {
  // 右侧按钮样式
}

.nav-placeholder {
  width: 120px; // 与按钮大致等宽，保持占位
  visibility: hidden;
}

.nav-arrow {
  color: #6b7280;
  transition: color 0.2s;

  .nav-btn:hover & {
    color: #3b82f6;
  }
}

.nav-word {
  font-weight: 500;
  color: #374151;
  font-size: 0.875rem;

  &.word-shadow {
    color: transparent;
    text-shadow: 0 0 8px rgba(0, 0, 0, 0.5);
  }
}

// 移动端适配
@media (max-width: 768px) {
  .navigation-buttons {
    max-width: 100%;
    height: 50px;
  }

  .nav-btn {
    padding: 0.4rem 0.8rem;
    min-height: 44px; // 确保触摸友好

    .nav-word {
      font-size: 0.8rem;
    }

    .nav-arrow {
      width: 18px;
      height: 18px;
    }
  }
}

// 超小屏幕适配
@media (max-width: 480px) {
  .navigation-buttons {
    height: 40px;
  }

  .nav-btn {
    padding: 0.3rem 0.6rem;

    .nav-word {
      font-size: 0.75rem;
      max-width: 4rem;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .nav-arrow {
      width: 16px;
      height: 16px;
    }
  }
}

// 暗色主题支持
@media (prefers-color-scheme: dark) {
  .nav-btn {
    background: rgba(31, 41, 55, 0.9);
    border-color: rgba(255, 255, 255, 0.1);

    &:hover {
      background: rgba(31, 41, 55, 0.95);
      border-color: rgba(255, 255, 255, 0.2);
    }
  }

  .nav-word {
    color: #d1d5db;
  }

  .nav-arrow {
    color: #9ca3af;

    .nav-btn:hover & {
      color: #60a5fa;
    }
  }
}

.word-panel-wrapper {
  position: absolute;
  left: var(--panel-margin-left);
  top: 0.8rem;
  z-index: 1;
  height: calc(100% - 1.5rem);
}
</style>
