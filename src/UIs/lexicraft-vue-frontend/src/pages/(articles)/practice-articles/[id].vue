<script lang="ts" setup>
import {addStat, setUserDictProp} from '@/apis'
import Toast from '@/components/base/toast/Toast.ts'
import Tooltip from '@/components/base/Tooltip.vue'
import BaseIcon from '@/components/BaseIcon.vue'
import ConflictNotice from '@/components/ConflictNotice.vue'
import ArticleList from '@/components/list/ArticleList.vue'
import Panel from '@/components/Panel.vue'
import PracticeLayout from '@/components/PracticeLayout.vue'
import ShortcutHints from '@/components/ShortcutHints.vue'
import SettingDialog from '@/components/setting/SettingDialog.vue'
import {AppEnv, DICT_LIST, LIB_JS_URL, TourConfig} from '@/config/env.ts'
import {genArticleSectionData, usePlaySentenceAudio} from '@/hooks/article.ts'
import {useArticleOptions} from '@/hooks/dict.ts'
import {useDisableEventListener, useOnKeyboardEventListener, useStartKeyboardEventListener} from '@/hooks/event.ts'
import useTheme from '@/hooks/theme.ts'
import ArticleAudio from '@/components/article/components/ArticleAudio.vue'
import EditSingleArticleModal from '@/components/article/components/EditSingleArticleModal.vue'
import TypingArticle from '@/components/article/components/TypingArticle.vue'
import {useBaseStore} from '@/stores/base.ts'
import {usePracticeStore} from '@/stores/practice.ts'
import {useRuntimeStore} from '@/stores/runtime.ts'
import {useSettingStore} from '@/stores/setting.ts'
import {getDefaultArticle, getDefaultDict, getDefaultWord} from '@/types/func.ts'
import type {Article, ArticleItem, ArticleWord, Dict, Statistics, Word} from '@/types/types.ts'
import {_getDictDataByUrl, _nextTick, cloneDeep, isMobile, loadJsLib, msToMinute, resourceWrap, total} from '@/utils'
import {getPracticeArticleCache, setPracticeArticleCache} from '@/utils/cache.ts'
import {emitter, EventKey, useEvents} from '@/utils/eventBus.ts'
import {computed, onMounted, onUnmounted, provide, watch} from 'vue'
import {useRoute, useRouter} from 'vue-router'
import {nanoid} from 'nanoid'
import {DictType, PracticeArticleWordType, ShortcutKey} from '@/types/enum.ts'

const store = useBaseStore()
const runtimeStore = useRuntimeStore()
const settingStore = useSettingStore()
const statStore = usePracticeStore()
const {toggleTheme} = useTheme()

let articleData = $ref({
  list: [],
  article: getDefaultArticle(),
})
let showEditArticle = $ref(false)
let typingArticleRef = $ref<any>()
let showConflictNotice = $ref(false)
let loading = $ref<boolean>(false)
let allWrongWords = new Set()
let editArticle = $ref<Article>(getDefaultArticle())
let audioRef = $ref<HTMLAudioElement>()
let timer = $ref(0)
let isFocus = true
let isTyped = $ref(false)

function write() {
  // console.log('write')
  settingStore.dictation = true
  repeat()
}

//TODO 需要判断是否已忽略
//todo 使用场景是？
function repeat() {
  // console.log('repeat')
  getCurrentPractice()
}

function prev() {
  // console.log('next')
  if (store.sbook.lastLearnIndex === 0) {
    Toast.warning('已经在第一章了~')
  } else {
    store.sbook.lastLearnIndex--
    getCurrentPractice()
  }
}

const toggleShowTranslate = () => (settingStore.translate = !settingStore.translate)
const toggleDictation = () => (settingStore.dictation = !settingStore.dictation)
const togglePanel = () => (settingStore.showPanel = !settingStore.showPanel)
const skip = () => typingArticleRef?.nextSentence()
const collect = () => toggleArticleCollect(articleData.article)
const shortcutKeyEdit = () => edit()

function toggleConciseMode() {
  settingStore.showToolbar = !settingStore.showToolbar
  settingStore.showPanel = settingStore.showToolbar
}

function next() {
  setPracticeArticleCache(null)
  if (store.sbook.lastLearnIndex >= articleData.list.length - 1) {
    store.sbook.complete = true
    store.sbook.lastLearnIndex = 0
    //todo 这里应该弹窗
  } else store.sbook.lastLearnIndex++
  getCurrentPractice()
}

const router = useRouter()
const route = useRoute()

async function init() {
  // console.log('load好了开始加载')
  let dict = getDefaultDict()
  let dictId = route.params.id
  if (dictId) {
    //先在自己的词典列表里面找，如果没有再在资源列表里面找
    dict = store.article.bookList.find(v => v.id == dictId)
    let r = await fetch(resourceWrap(DICT_LIST.ARTICLE.ALL))
    let book_list = await r.json()
    if (!dict) dict = book_list.find(v => v.id === dictId) as Dict
    if (dict && dict.id) {
      //如果是不是自定义词典，就请求数据
      if (!dict.custom) dict = await _getDictDataByUrl(dict, DictType.article)
      if (!dict.articles.length) {
        router.push('/articles')
        return Toast.warning('没有文章可学习！')
      }
      await store.changeBook(dict)
      articleData.list = cloneDeep(store.sbook.articles)
      getCurrentPractice()
      loading = false
    } else {
      router.push('/articles')
    }
  } else {
    router.push('/articles')
  }
}

const initAudio = () => {
  _nextTick(() => {
    audioRef.volume = settingStore.articleSoundVolume / 100
    audioRef.playbackRate = settingStore.articleSoundSpeed
  })
}

const handleVolumeUpdate = (volume: number) => {
  settingStore.articleSoundVolume = volume
}

const handleSpeedUpdate = (speed: number) => {
  settingStore.articleSoundSpeed = speed
}

watch(
    [() => store.load, () => loading],
    ([a, b]) => {
      if (a && b) init()
    },
    {immediate: true}
)

watch(
    () => articleData?.article?.id,
    id => {
      if (id) {
        _nextTick(async () => {
          const Shepherd = await loadJsLib('Shepherd', LIB_JS_URL.SHEPHERD)
          const tour = new Shepherd.Tour(TourConfig)
          tour.on('cancel', () => {
            localStorage.setItem('tour-guide', '1')
          })
          tour.addStep({
            id: 'step8',
            text: '这里可以练习文章，只需要按下键盘上对应的按键即可，没有输入框！',
            attachTo: {
              element: '#article-content',
              on: 'auto',
            },
            buttons: [
              {
                text: `关闭`,
                action() {
                  settingStore.first = false
                  tour.next()
                  setTimeout(() => {
                    showConflictNotice = true
                  }, 1500)
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

watch(
    () => settingStore.$state,
    n => {
      initAudio()
    },
    {immediate: true, deep: true}
)

onMounted(() => {
  if (store.sbook?.articles?.length) {
    articleData.list = cloneDeep(store.sbook.articles)
    getCurrentPractice()
  } else {
    loading = true
  }
  if (route.query.guide) {
    showConflictNotice = false
  } else {
    showConflictNotice = true
  }
})

onUnmounted(() => {
  runtimeStore.disableEventListener = false
  let cache = getPracticeArticleCache()
  //如果有缓存，则更新花费的时间；因为用户不输入不会保存数据
  if (cache) {
    cache.statStoreData.spend = statStore.spend
    setPracticeArticleCache(cache)
  }
  clearInterval(timer)
})

useStartKeyboardEventListener()
useDisableEventListener(() => loading)

function setArticle(val: Article) {
  statStore.wrong = 0
  statStore.total = 0
  statStore.startDate = Date.now()
  statStore.spend = 0
  allWrongWords = new Set()
  articleData.list[store.sbook.lastLearnIndex] = val
  articleData.article = val
  let ignoreList = [store.allIgnoreWords, store.knownWords][settingStore.ignoreSimpleWord ? 0 : 1]
  articleData.article.sections.map((v, i) => {
    v.map(w => {
      w.words.map(s => {
        if (!ignoreList.includes(s.word.toLowerCase()) && s.type === PracticeArticleWordType.Word) {
          statStore.total++
        }
      })
    })
  })

  isTyped = false
  clearInterval(timer)
  timer = setInterval(() => {
    if (isFocus) {
      statStore.spend += 1000
    }
  }, 1000)

  _nextTick(typingArticleRef?.init)
}

async function complete() {
  clearInterval(timer)
  //延时删除缓存，因为可能还有输入，需要保存
  setTimeout(() => {
    setPracticeArticleCache(null)
  }, 1500)

  //todo 有空了改成实时保存
  let data: Partial<Statistics> & { title: string; articleId: number } = {
    articleId: articleData.article.id,
    title: articleData.article.title,
    spend: statStore.spend,
    startDate: statStore.startDate,
    total: statStore.total,
    wrong: statStore.wrong,
  }

  let reportData = {
    name: store.sbook.name,
    index: store.sbook.lastLearnIndex,
    custom: store.sbook.custom,
    complete: store.sbook.complete,
    title: articleData.article.title,
    spend: Number(statStore.spend / 1000 / 60).toFixed(1),
    s: '',
  }
  reportData.s = `name:${store.sbook.name},title:${store.sbook.lastLearnIndex}.${data.title},spend:${Number(statStore.spend / 1000 / 60).toFixed(1)}`
  window.umami?.track('endStudyArticle', reportData)

  if (store.sbook.lastLearnIndex >= store.sbook.length - 1) {
    store.sdict.complete = true
  }
  if (AppEnv.CAN_REQUEST) {
    let res = await addStat({
      ...data,
      type: 'article',
      complete: store.sdict.complete,
    })
    if (!res.success) {
      Toast.error(res.msg)
    }
  }

  store.sbook.statistics.push(data as any)

  //重置
  statStore.wrong = 0
  statStore.startDate = Date.now()
}

function getCurrentPractice() {
  emitter.emit(EventKey.resetWord)
  let currentArticle = articleData.list[store.sbook.lastLearnIndex]
  let article = getDefaultArticle(currentArticle)
  if (article.sections.length) {
    setArticle(article)
  } else {
    genArticleSectionData(article)
    setArticle(article)
  }
}

function saveArticle(val: Article) {
  console.log('saveArticle', val, JSON.stringify(val.lrcPosition))
  console.log('saveArticle', val.textTranslate)
  showEditArticle = false
  let rIndex = store.sbook.articles.findIndex(v => v.id === val.id)
  if (rIndex > -1) {
    store.sbook.articles[rIndex] = cloneDeep(val)
  }
  setArticle(val)
  store.sbook.custom = true
  if (!store.sbook.id.includes('_custom')) {
    store.sbook.id += '_custom_' + nanoid(6)
  }
}

function edit(val: Article = articleData.article) {
  editArticle = val
  showEditArticle = true
}

function wrong(word: Word) {
  let temp = word.word.toLowerCase()
  //过滤简单词
  if (settingStore.ignoreSimpleWord) {
    if (store.simpleWords.includes(temp)) return
  }
  if (!allWrongWords.has(temp)) {
    allWrongWords.add(temp)
    statStore.wrong++
  }

  if (!store.wrong.words.find((v: Word) => v.word.toLowerCase() === temp)) {
    store.wrong.words.push(getDefaultWord(word))
    store.wrong.length = store.wrong.words.length
  }
}

function nextWord(word: ArticleWord) {
  if (!store.allIgnoreWords.includes(word.word.toLowerCase()) && word.type === PracticeArticleWordType.Word) {
    statStore.inputWordNumber++
  }
}

async function changeArticle(val: ArticleItem) {
  setPracticeArticleCache(null)
  let rIndex = articleData.list.findIndex(v => v.id === val.item.id)
  if (rIndex > -1) {
    store.sbook.lastLearnIndex = rIndex
    getCurrentPractice()

    if (AppEnv.CAN_REQUEST) {
      let res = await setUserDictProp(null, store.sbook)
      if (!res.success) {
        Toast.error(res.msg)
      }
    }
  }
  initAudio()
}

const handlePlayNext = (nextArticle: Article) => {
  let rIndex = articleData.list.findIndex(v => v.id === nextArticle.id)
  if (rIndex > -1) {
    store.sbook.lastLearnIndex = rIndex
    getCurrentPractice()
  }
}

const {isArticleCollect, toggleArticleCollect} = useArticleOptions()

function play() {
  typingArticleRef?.play()
}

function show() {
  typingArticleRef?.showSentence()
}

function onKeyUp() {
  typingArticleRef?.hideSentence()
}

async function onKeyDown(e: KeyboardEvent) {
  switch (e.key) {
    case 'Backspace':
      typingArticleRef.del()
      break
  }
}

useOnKeyboardEventListener(onKeyDown, onKeyUp)

useEvents([
  [EventKey.write, write],
  [EventKey.repeatStudy, repeat],
  [EventKey.continueStudy, next],

  [ShortcutKey.PreviousChapter, prev],
  [ShortcutKey.RepeatChapter, repeat],
  [ShortcutKey.DictationChapter, write],
  [ShortcutKey.ToggleShowTranslate, toggleShowTranslate],
  [ShortcutKey.ToggleDictation, toggleDictation],
  [ShortcutKey.ToggleTheme, toggleTheme],
  [ShortcutKey.ToggleConciseMode, toggleConciseMode],
  [ShortcutKey.TogglePanel, togglePanel],
  [ShortcutKey.NextChapter, next],
  [ShortcutKey.PlayWordPronunciation, play],
  [ShortcutKey.ShowWord, show],
  [ShortcutKey.Next, skip],
  [ShortcutKey.ToggleCollect, collect],
  [ShortcutKey.EditArticle, shortcutKeyEdit],
])

onMounted(() => {
  document.addEventListener('visibilitychange', () => {
    isFocus = !document.hidden
  })
})

onUnmounted(() => {
  timer && clearInterval(timer)
})

const {playSentenceAudio} = usePlaySentenceAudio()

function play2(e) {
  _nextTick(() => {
    if (settingStore.articleSound || e.handle) {
      playSentenceAudio(e.sentence, audioRef)
    }
  })
}

const currentPractice = computed(() => {
  if (store.sbook.statistics?.length) {
    return store.sbook.statistics.filter((v: any) => v.title === articleData.article.title)
  }
  return []
})

provide('currentPractice', currentPractice)
</script>
<template>
  <PracticeLayout v-loading="loading" maxWidth="var(--article-width)">
    <template v-slot:hints>
      <ShortcutHints
          :hints="[
          { key: ShortcutKey.PlayWordPronunciation, label: '播音' },
          { key: ShortcutKey.ToggleDictation, label: '内容' },
          { key: ShortcutKey.ToggleShowTranslate, label: '翻译' },
          { key: ShortcutKey.Next, label: '跳过' },
          { key: ShortcutKey.ShowWord, label: '提示' },
        ]"
      />
    </template>
    <template v-slot:practice>
      <TypingArticle
          ref="typingArticleRef"
          :article="articleData.article"
          @complete="complete"
          @next="next"
          @nextWord="nextWord"
          @play="play2"
          @replay="setArticle(articleData.article)"
          @wrong="wrong"
      />
    </template>
    <template v-slot:panel>
      <Panel :style="{ width: 'var(--article-panel-width)' }">
        <template v-slot:title>
          <span>{{ store.sbook.name }} ({{ store.sbook.lastLearnIndex + 1 }} / {{ articleData.list.length }})</span>
        </template>
        <div class="panel-page-item pl-4">
          <ArticleList
              :active-id="articleData.article.id ?? ''"
              :isActive="settingStore.showPanel"
              :list="articleData.list"
              :show-translate="settingStore.translate"
              :static="false"
              @click="changeArticle"
          >
          </ArticleList>
        </div>
      </Panel>
    </template>
    <template v-slot:footer>
      <div class="footer pb-3">
        <div class="center h-10">
          <Tooltip :title="settingStore.showToolbar ? '收起' : '展开'">
            <IconFluentChevronLeft20Filled
                :class="!settingStore.showToolbar && 'down'"
                class="arrow"
                color="#999"
                @click="settingStore.showToolbar = !settingStore.showToolbar"
            />
          </Tooltip>
        </div>
        <div class="bottom">
          <div class="flex justify-between items-center gap-2">
            <div class="stat">
              <div class="row">
                <div class="num">{{ currentPractice.length }}次/{{ msToMinute(total(currentPractice, 'spend')) }}</div>
                <div class="line"></div>
                <div class="name">记录</div>
              </div>
              <div class="row">
                <!--                <div class="num">{{statStore.spend }}分钟</div>-->
                <div class="num">{{ Math.floor(statStore.spend / 1000 / 60) }}分钟</div>
                <div class="line"></div>
                <div class="name">时间</div>
              </div>
              <div class="row">
                <div class="num center gap-1">
                  {{ statStore.total }}
                  <Tooltip>
                    <IconFluentQuestionCircle20Regular width="18"/>
                    <template #reference>
                      <div>
                        统计词数{{ settingStore.ignoreSimpleWord ? '不包含' : '包含' }}简单词，不包含已掌握
                        <div>简单词可在设置 -> 练习设置 -> 简单词过滤中修改</div>
                      </div>
                    </template>
                  </Tooltip>
                </div>
                <div class="line"></div>
                <div class="name">单词总数</div>
              </div>
            </div>
            <ArticleAudio
                ref="audioRef"
                :article="articleData.article"
                @update-speed="handleSpeedUpdate"
                @update-volume="handleVolumeUpdate"
            ></ArticleAudio>
            <div class="flex flex-col items-center justify-center gap-1">
              <div class="flex gap-2 center">
                <SettingDialog type="article"/>

                <BaseIcon :title="`下一句(${settingStore.shortcutKeyMap[ShortcutKey.Next]})`" @click="skip">
                  <IconFluentArrowBounce20Regular class="transform-rotate-180"/>
                </BaseIcon>
                <BaseIcon
                    :title="`播放当前句子(${settingStore.shortcutKeyMap[ShortcutKey.PlayWordPronunciation]})`"
                    @click="play"
                >
                  <IconFluentReplay20Regular/>
                </BaseIcon>
                <BaseIcon
                    :title="`开关默写模式(${settingStore.shortcutKeyMap[ShortcutKey.ToggleDictation]})`"
                    @click="settingStore.dictation = !settingStore.dictation"
                >
                  <IconFluentEyeOff16Regular v-if="settingStore.dictation"/>
                  <IconFluentEye16Regular v-else/>
                </BaseIcon>

                <BaseIcon
                    :title="`开关释义显示(${settingStore.shortcutKeyMap[ShortcutKey.ToggleShowTranslate]})`"
                    @click="settingStore.translate = !settingStore.translate"
                >
                  <IconFluentTranslate16Regular v-if="settingStore.translate"/>
                  <IconFluentTranslateOff16Regular v-else/>
                </BaseIcon>

                <!--              <BaseIcon-->
                <!--                  :title="`编辑(${settingStore.shortcutKeyMap[ShortcutKey.EditArticle]})`"-->
                <!--                  icon="tabler:edit"-->
                <!--                  @click="emitter.emit(ShortcutKey.EditArticle)"-->
                <!--              />-->
                <BaseIcon
                    :title="`面板(${settingStore.shortcutKeyMap[ShortcutKey.TogglePanel]})`"
                    @click="settingStore.showPanel = !settingStore.showPanel"
                >
                  <IconFluentTextListAbcUppercaseLtr20Regular/>
                </BaseIcon>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </PracticeLayout>

  <EditSingleArticleModal v-model="showEditArticle" :article="editArticle" @save="saveArticle"/>

  <ConflictNotice v-if="showConflictNotice"/>
</template>

<style lang="scss" scoped>
.footer {
  width: var(--article-toolbar-width);
  max-width: 1000px;
  @apply bg-primary;

  .bottom {
    @apply relative w-full box-border rounded-lg bg-second shadow-lg z-2;
    padding: 0.5rem var(--space);
    border: 1px solid var(--color-item-border);

    .stat {
      margin-top: 0.5rem;
      display: flex;
      justify-content: space-around;
      gap: var(--stat-gap);

      .row {
        @apply flex flex-col items-center gap-1 text-gray-500;

        .num,
        .name {
          word-break: keep-all;
          padding: 0 0.4rem;
        }

        .line {
          height: 1px;
          width: 100%;
          background: var(--color-sub-gray);
        }
      }
    }
  }

  .arrow {
    cursor: pointer;
    transition: all 0.5s;
    transform: rotate(-90deg);
    padding: 0.5rem;
    font-size: 1.2rem;

    &.down {
      transform: rotate(90deg);
    }
  }
}

// 移动端适配
@media (max-width: 768px) {
  // 优化练习区域布局
  .practice-article {
    padding-top: 3rem; // 为固定标题留出空间
  }

  // 优化标题区域
  .typing-article {
    header {
      position: fixed;
      top: 4.5rem; // 避开顶部导航栏
      left: 0;
      right: 0;
      z-index: 100;
      background: var(--bg-color);
      padding: 0.5rem 1rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      margin-bottom: 0;

      .title {
        font-size: 1rem;
        line-height: 1.4;
        word-break: break-word;

        .font-family {
          font-size: 0.9rem;
        }
      }

      .titleTranslate {
        font-size: 0.8rem;
        margin-top: 0.2rem;
        opacity: 0.8;
      }
    }

    .article-content {
      margin-top: 2rem; // 为固定标题留出空间
    }
  }

  .footer {
    width: 100%;

    .bottom {
      padding: 0.3rem 0.5rem 0.5rem 0.5rem;
      border-radius: 0.4rem;

      .stat {
        margin-top: 0.3rem;
        gap: 0.2rem;
        flex-direction: row;
        overflow-x: auto;

        .row {
          min-width: 3.5rem;
          gap: 0.2rem;

          .num {
            font-size: 0.8rem;
            font-weight: bold;
          }

          .name {
            font-size: 0.7rem;
          }
        }
      }

      .flex.flex-col.items-center.justify-center.gap-1 {
        .flex.gap-2.center {
          display: grid;
          grid-template-columns: repeat(3, 1fr);
          gap: 0.4rem;

          .base-icon {
            padding: 0.3rem;
            font-size: 1rem;
            min-height: 44px;
            min-width: 44px;
            display: flex;
            align-items: center;
            justify-content: center;
          }
        }
      }
    }

    .arrow {
      font-size: 1rem;
      padding: 0.3rem;
    }
  }
}
</style>
