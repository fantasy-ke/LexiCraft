<script lang="ts" setup>
import {useBaseStore} from '@/stores/base.ts'
import {useRouter} from 'vue-router'
import BaseIcon from '@/components/BaseIcon.vue'
import {
  _getAccomplishDate,
  _getDictDataByUrl,
  _nextTick,
  isMobile,
  loadJsLib,
  resourceWrap,
  shuffle,
  useNav,
} from '@/utils'
import BasePage from '@/components/BasePage.vue'
import type {DictResource} from '@/types/types.ts'
import {watch} from 'vue'
import {getCurrentStudyWord} from '@/hooks/dict.ts'
import {useRuntimeStore} from '@/stores/runtime.ts'
import Book from '@/components/Book.vue'
import PopConfirm from '@/components/PopConfirm.vue'
import Progress from '@/components/base/Progress.vue'
import Toast from '@/components/base/toast/Toast.ts'
import BaseButton from '@/components/BaseButton.vue'
import {getDefaultDict} from '@/types/func.ts'
import DeleteIcon from '@/components/icon/DeleteIcon.vue'
import PracticeSettingDialog from '@/components/word/components/PracticeSettingDialog.vue'
import ChangeLastPracticeIndexDialog from '@/components/word/components/ChangeLastPracticeIndexDialog.vue'
import {useSettingStore} from '@/stores/setting.ts'
import {useFetch} from '@vueuse/core'
import {AppEnv, DICT_LIST, Host, LIB_JS_URL, TourConfig, WordPracticeModeNameMap} from '@/config/env.ts'
import {myDictList} from '@/apis'
import PracticeWordListDialog from '@/components/word/components/PracticeWordListDialog.vue'
import ShufflePracticeSettingDialog from '@/components/word/components/ShufflePracticeSettingDialog.vue'
import {deleteDict} from '@/apis/dict.ts'
import OptionButton from '@/components/base/OptionButton.vue'
import {getPracticeWordCache, setPracticeWordCache} from '@/utils/cache.ts'
import {WordPracticeMode} from '@/types/enum.ts'

const store = useBaseStore()
const settingStore = useSettingStore()
const router = useRouter()
const {nav} = useNav()
const runtimeStore = useRuntimeStore()
let loading = $ref(true)
let isSaveData = $ref(false)

let currentStudy = $ref({
  new: [],
  review: [],
  write: [],
  shuffle: [],
})

watch(
    () => store.load,
    n => {
      if (n) {
        init()
        _nextTick(async () => {
          const Shepherd = await loadJsLib('Shepherd', LIB_JS_URL.SHEPHERD)
          const tour = new Shepherd.Tour(TourConfig)
          tour.on('cancel', () => {
            localStorage.setItem('tour-guide', '1')
          })
          tour.addStep({
            id: 'step1',
            text: '点击这里选择一本词典开始学习',
            attachTo: {
              element: '#step1',
              on: 'bottom',
            },
            buttons: [
              {
                text: `下一步（1/${TourConfig.total}）`,
                action() {
                  tour.next()
                  router.push('/app/dict-list')
                },
              },
            ],
          })
          const r = localStorage.getItem('tour-guide')
          if (settingStore.first && !r && !isMobile()) tour.start()
        }, 500)
      }
    },
    {immediate: true}
)

async function init() {
  if (AppEnv.CAN_REQUEST) {
    let res = await myDictList({type: 'word'})
    if (res.success) {
      store.setState(Object.assign(store.$state, res.data))
    }
  }
  if (store.word.studyIndex >= 3) {
    if (!store.sdict.custom && !store.sdict.words.length) {
      store.word.bookList[store.word.studyIndex] = await _getDictDataByUrl(store.sdict)
    }
  }
  if (!currentStudy.new.length && store.sdict.words.length) {
    let d = getPracticeWordCache()
    if (d) {
      currentStudy = d.taskWords
      isSaveData = true
    } else {
      currentStudy = getCurrentStudyWord()
    }
  }
  loading = false
}

function startPractice(practiceMode: WordPracticeMode, resetCache: boolean = false): void {
  if (store.sdict.id) {
    if (!store.sdict.words.length) {
      Toast.warning('没有单词可学习！')
      return
    }

    if (resetCache) {
      setPracticeWordCache(null)
    }
    settingStore.wordPracticeMode = practiceMode

    window.umami?.track('startStudyWord', {
      name: store.sdict.name,
      index: store.sdict.lastLearnIndex,
      perDayStudyNumber: store.sdict.perDayStudyNumber,
      custom: store.sdict.custom,
      complete: store.sdict.complete,
      wordPracticeMode: settingStore.wordPracticeMode,
    })
    //把是否是第一次设置为false
    settingStore.first = false
    nav('/app/practice-words/' + store.sdict.id, {}, {taskWords: currentStudy})
  } else {
    window.umami?.track('no-dict')
    Toast.warning('请先选择一本词典')
  }
}

function freePractice() {
  startPractice(WordPracticeMode.Free, settingStore.wordPracticeMode !== WordPracticeMode.Free && isSaveData)
}

function systemPractice() {
  startPractice(
      settingStore.wordPracticeMode === WordPracticeMode.Free ? WordPracticeMode.System : settingStore.wordPracticeMode,
      settingStore.wordPracticeMode === WordPracticeMode.Free && isSaveData
  )
}

let showPracticeSettingDialog = $ref(false)
let showShufflePracticeSettingDialog = $ref(false)
let showChangeLastPracticeIndexDialog = $ref(false)
let showPracticeWordListDialog = $ref(false)

async function goDictDetail(val: DictResource) {
  if (!val.id) return nav('/app/dict-list')
  runtimeStore.editDict = getDefaultDict(val)
  nav('/app/dict-detail', {})
}

let isManageDict = $ref(false)
let selectIds = $ref([])

async function handleBatchDel() {
  if (AppEnv.CAN_REQUEST) {
    let res = await deleteDict(null, selectIds)
    if (res.success) {
      init()
    } else {
      Toast.error(res.msg)
    }
  } else {
    selectIds.forEach(id => {
      let r = store.word.bookList.findIndex(v => v.id === id)
      if (r !== -1) {
        if (store.word.studyIndex === r) {
          store.word.studyIndex = -1
        }
        if (store.word.studyIndex > r) {
          store.word.studyIndex--
        }
        store.word.bookList.splice(r, 1)
      }
    })
    selectIds = []
    Toast.success('删除成功！')
  }
}

function toggleSelect(item) {
  let rIndex = selectIds.findIndex(v => v === item.id)
  if (rIndex > -1) {
    selectIds.splice(rIndex, 1)
  } else {
    selectIds.push(item.id)
  }
}

const progressTextLeft = $computed(() => {
  if (store.sdict.complete) return '已学完，进入总复习阶段'
  return '当前进度：已学' + store.currentStudyProgress + '%'
})

function check(cb: Function) {
  if (!store.sdict.id) {
    Toast.warning('请先选择一本词典')
  } else {
    runtimeStore.editDict = getDefaultDict(store.sdict)
    cb()
  }
}

async function savePracticeSetting() {
  Toast.success('修改成功')
  isSaveData = false
  setPracticeWordCache(null)
  await store.changeDict(runtimeStore.editDict)
  currentStudy = getCurrentStudyWord()
}

async function onShufflePracticeSettingOk(total) {
  window.umami?.track('startShuffleStudyWord', {
    name: store.sdict.name,
    index: store.sdict.lastLearnIndex,
    perDayStudyNumber: store.sdict.perDayStudyNumber,
    total,
    custom: store.sdict.custom,
    complete: store.sdict.complete,
  })
  isSaveData = false
  setPracticeWordCache(null)
  settingStore.wordPracticeMode = WordPracticeMode.Shuffle
  let ignoreList = [store.allIgnoreWords, store.knownWords][settingStore.ignoreSimpleWord ? 0 : 1]
  currentStudy.shuffle = shuffle(
      store.sdict.words.slice(0, store.sdict.lastLearnIndex).filter(v => !ignoreList.includes(v.word))
  ).slice(0, total)
  nav(
      '/app/practice-words/' + store.sdict.id,
      {},
      {
        taskWords: currentStudy,
        total, //用于再来一组时，随机出正确的长度，因为练习中可能会点击已掌握，导致重学一遍之后长度变少，如果再来一组，此时长度就不正确
      }
  )
}

async function saveLastPracticeIndex(e) {
  Toast.success('修改成功')
  runtimeStore.editDict.lastLearnIndex = e
  // runtimeStore.editDict.complete = e >= runtimeStore.editDict.length - 1
  showChangeLastPracticeIndexDialog = false
  isSaveData = false
  setPracticeWordCache(null)
  await store.changeDict(runtimeStore.editDict)
  currentStudy = getCurrentStudyWord()
}

const {data: recommendDictList, isFetching} = useFetch(resourceWrap(DICT_LIST.WORD.RECOMMENDED)).json()

let isNewHost = $ref(window.location.host === Host)

const systemPracticeText = $computed(() => {
  if (settingStore.wordPracticeMode === WordPracticeMode.Free) {
    return '开始学习'
  } else {
    return isSaveData
        ? '继续' + WordPracticeModeNameMap[settingStore.wordPracticeMode]
        : '开始' + WordPracticeModeNameMap[settingStore.wordPracticeMode]
  }
})
</script>

<template>
  <BasePage>
    <!-- Current Dictionary Progress Card -->
    <div class="card-white p-8 relative group mb-6">
      <div class="absolute inset-0 overflow-hidden rounded-xl pointer-events-none">
        <div class="absolute -right-20 -top-20 w-64 h-64 bg-blue-500/10 rounded-full blur-3xl group-hover:bg-blue-500/20 transition-colors duration-700"></div>
      </div>
      
      <div class="flex flex-col lg:flex-row gap-8 relative z-10 items-stretch">
        <div class="flex-1 space-y-6">
          <div class="flex items-center gap-4">
            <div class="w-14 h-14 rounded-2xl bg-gradient-to-br from-blue-500 to-indigo-600 center shadow-xl text-white shadow-blue-500/20">
              <IconFluentBookNumber24Filled class="text-3xl"/>
            </div>
            <div class="flex-1">
              <h2 class="text-3xl font-black grad-text m-0 cursor-pointer" @click="goDictDetail(store.sdict)">
                {{ store.sdict.name || '开启词典学习之旅' }}
              </h2>
              <div v-if="store.sdict.id" class="text-slate-400 font-bold text-sm tracking-widest uppercase mt-1">
                {{ store.sdict.complete ? '字典已完成' : '学习进度进行中' }}
              </div>
            </div>
          </div>

          <template v-if="store.sdict.id">
            <div class="bg-slate-50 dark:bg-slate-800/40 p-6 rounded-3xl border border-slate-100 dark:border-slate-800">
              <div class="flex justify-between items-end mb-4">
                <div class="space-y-1">
                  <div class="text-sm font-bold text-slate-400 uppercase tracking-wider">预期完成</div>
                  <div class="text-xl font-black text-slate-900 dark:text-white">
                    {{ _getAccomplishDate(store.sdict.words.length - store.sdict.lastLearnIndex, store.sdict.perDayStudyNumber) }}
                  </div>
                </div>
                <div class="text-right">
                  <div class="text-4xl font-black text-indigo-600 dark:text-indigo-400 leading-none">{{ store.currentStudyProgress }}<span class="text-lg ml-1 opacity-50">%</span></div>
                  <div class="text-xs font-bold text-slate-400 uppercase tracking-widest mt-1">总进度</div>
                </div>
              </div>
              
              <Progress :percentage="store.currentStudyProgress" :show-text="false" class="h-3 rounded-full" />
              
              <div class="flex justify-between mt-4 text-sm font-bold text-slate-500">
                <span>{{ progressTextLeft }}</span>
                <span class="text-slate-900 dark:text-slate-300">{{ store.sdict?.lastLearnIndex }} / {{ store.sdict.words.length }} 词</span>
              </div>
            </div>

            <div class="flex gap-4">
              <BaseButton size="small" type="info" class="rounded-xl px-6 h-11 bg-slate-50 border-slate-100 hover:bg-blue-50 hover:border-blue-200 hover:text-blue-600 transition-all duration-300" @click="router.push('/app/dict-list')">
                <div class="flex items-center gap-2">
                  <IconFluentArrowSwap24Regular class="text-lg"/>
                  <span class="font-bold">更换词典</span>
                </div>
              </BaseButton>
              <PopConfirm :disabled="!isSaveData" title="更改进度将重新生成任务，是否继续？" @confirm="check(() => (showChangeLastPracticeIndexDialog = true))">
                <BaseButton v-if="store.sdict.id" size="small" type="info" class="rounded-xl px-6 h-11 bg-slate-50 border-slate-100 hover:bg-indigo-50 hover:border-indigo-200 hover:text-indigo-600 transition-all duration-300">
                  <div class="flex items-center gap-2">
                    <IconFluentSlideTextTitleEdit24Regular class="text-lg"/>
                    <span class="font-bold">调整位置</span>
                  </div>
                </BaseButton>
              </PopConfirm>
            </div>
          </template>

          <div v-else class="py-10 text-center lg:text-left bg-slate-50/50 dark:bg-slate-800/20 rounded-3xl border-2 border-dashed border-slate-200 dark:border-slate-800 p-8">
            <h3 class="text-xl font-bold text-slate-400 mb-6">您当前还没有正在学习的词库</h3>
            <BaseButton id="step1" size="large" class="h-14 px-10 text-lg shadow-2xl" @click="router.push('/app/dict-list')">
              <div class="flex items-center gap-3">
                <IconFluentAdd24Filled/>
                <span>挑选一本词典</span>
              </div>
            </BaseButton>
          </div>
        </div>

        <!-- Daily Task Area -->
        <div class="flex-1 min-w-[340px] flex flex-col" :class="!store.sdict.id && 'opacity-20 grayscale pointer-events-none'">
          <div class="bg-gradient-to-br from-slate-900 to-slate-800 dark:from-slate-800 dark:to-slate-900 rounded-[2.5rem] p-8 text-white shadow-2xl shadow-slate-900/20 flex flex-col justify-between flex-1">
            <div class="flex justify-between items-start mb-8">
              <div class="space-y-1">
                <div class="flex items-center gap-2">
                  <div class="w-8 h-8 rounded-lg bg-red-500/20 center text-red-400">
                    <IconFluentTarget24Filled class="text-xl"/>
                  </div>
                  <h3 class="text-xl font-black m-0">{{ isSaveData ? '继续任务' : '今日目标' }}</h3>
                </div>
                <div class="text-xs font-bold text-slate-400 uppercase tracking-widest pl-10" @click="showPracticeWordListDialog = true">
                   查看完整词表 <IconFluentChevronRight20Regular class="inline text-xs"/>
                </div>
              </div>
              
              <div class="text-right">
                <div class="text-3xl font-black text-amber-400 leading-none">
                  {{ store.sdict.id ? store.sdict.perDayStudyNumber : 0 }}
                </div>
                <div class="text-[10px] font-black text-slate-500 uppercase tracking-[0.2em] mt-1">DAILY COUNT</div>
                <BaseButton size="small" type="info" class="mt-2 h-7 px-3 bg-white/10 border-white/10 hover:bg-white/20 text-[10px]" @click="check(() => (showPracticeSettingDialog = true))">调整</BaseButton>
              </div>
            </div>

            <div class="grid grid-cols-3 gap-3 mb-8">
              <div class="task-stat-box">
                <div class="num text-blue-400">{{ currentStudy.new.length }}</div>
                <div class="label">新词学习</div>
              </div>
              <div class="task-stat-box">
                <div class="num text-indigo-400">{{ currentStudy.review.length }}</div>
                <div class="label">复习新词</div>
              </div>
              <div class="task-stat-box">
                <div class="num text-emerald-400">{{ currentStudy.write.length }}</div>
                <div class="label">巩固单词</div>
              </div>
            </div>

            <div class="flex gap-4">
              <BaseButton
                class="flex-[2] h-14 rounded-2xl font-black text-lg shadow-xl shadow-indigo-600/30 transition-all duration-300 hover:scale-[1.02] hover:shadow-indigo-600/40 active:scale-[0.98]"
                :class="settingStore.wordPracticeMode !== WordPracticeMode.Free ? 'bg-gradient-to-r from-indigo-600 to-blue-600 border-none' : 'bg-white/10 border-white/20 hover:bg-white/15'"
                :loading="loading"
                @click="systemPractice"
              >
                <div class="center gap-3">
                  <span>{{ systemPracticeText }}</span>
                  <IconFluentChevronCircleRight24Regular class="text-2xl"/>
                </div>
              </BaseButton>

              <BaseButton
                class="flex-1 h-14 rounded-2xl bg-white/5 border-white/10 backdrop-blur-md transition-all duration-300 hover:bg-white/10 hover:border-white/30 hover:scale-[1.02] active:scale-[0.98]"
                :loading="loading"
                @click="freePractice()"
              >
                <div class="center gap-2">
                   <IconStreamlineColorPenDrawFlat class="text-2xl text-blue-300"/>
                </div>
              </BaseButton>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- My Dicts Section -->
    <div class="card-white p-8 mb-6">
      <div class="flex justify-between items-center mb-10">
        <div>
          <h2 class="text-2xl font-black m-0 grad-text">我的学习库</h2>
          <p class="text-sm font-bold text-slate-400 mt-1 uppercase tracking-widest">PERSONAL DICTIONARIES</p>
        </div>
        <div class="flex gap-4 items-center">
          <PopConfirm v-if="selectIds.length" title="确认要删除选中的词典吗？" @confirm="handleBatchDel">
            <div class="w-10 h-10 rounded-xl bg-rose-50 text-rose-500 center cursor-pointer hover:bg-rose-100 transition-colors">
              <DeleteIcon class="text-xl"/>
            </div>
          </PopConfirm>

          <BaseButton type="info" size="small" class="rounded-xl px-4" @click="isManageDict = !isManageDict; selectIds = []">
            {{ isManageDict ? '完成管理' : '批量管理' }}
          </BaseButton>
          
          <BaseButton type="primary" size="small" class="rounded-xl px-6 bg-gradient-to-r from-blue-600 to-indigo-600 border-none font-bold" @click="nav('/app/dict-detail', { isAdd: true })">
            新建库
          </BaseButton>
        </div>
      </div>
      
      <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-6">
        <Book
          v-for="(item, j) in store.word.bookList"
          :checked="selectIds.includes(item.id)"
          :is-add="false"
          :item="item"
          :show-checkbox="isManageDict && j >= 3"
          quantifier="词"
          class="hover:-translate-y-2 transition-transform duration-300"
          @check="() => toggleSelect(item)"
          @click="goDictDetail(item)"
        />
        <Book :is-add="true" class="hover:-translate-y-2 transition-transform duration-300" @click="router.push('/app/dict-list')"/>
      </div>
    </div>

    <!-- Recommendations Section -->
    <div v-loading="isFetching" class="card-white p-8 relative overflow-hidden">
      <div class="flex justify-between items-center mb-10 border-b border-slate-50 dark:border-slate-800 pb-6">
        <div>
          <h2 class="text-2xl font-black m-0">热门推荐</h2>
          <p class="text-sm font-bold text-slate-400 mt-1 uppercase tracking-widest">BEST SELECTION FOR YOU</p>
        </div>
        <BaseButton type="info" size="small" class="rounded-xl px-6" @click="router.push('/app/dict-list')">
          浏览更多
        </BaseButton>
      </div>

      <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-6">
        <Book
          v-for="(item, j) in recommendDictList"
          :is-add="false"
          :item="item as any"
          quantifier="词"
          class="hover:-translate-y-2 transition-transform duration-300"
          @click="goDictDetail(item as any)"
        />
      </div>
    </div>
  </BasePage>

  <PracticeSettingDialog v-model="showPracticeSettingDialog" :show-left-option="false" @ok="savePracticeSetting"/>

  <ChangeLastPracticeIndexDialog v-model="showChangeLastPracticeIndexDialog" @ok="saveLastPracticeIndex"/>

  <PracticeWordListDialog v-model="showPracticeWordListDialog" :data="currentStudy"/>

  <ShufflePracticeSettingDialog v-model="showShufflePracticeSettingDialog" @ok="onShufflePracticeSettingOk"/>
</template>

<style lang="scss" scoped>
.task-stat-box {
  @apply flex flex-col items-center justify-center p-4 rounded-3xl bg-white/5 border border-white/10 backdrop-blur-md transition-all duration-300;
  
  .num {
    @apply text-3xl font-black mb-1;
  }
  
  .label {
    @apply text-[10px] font-black text-slate-500 uppercase tracking-widest whitespace-nowrap;
  }
  
  &:hover {
    @apply bg-white/10 -translate-y-1;
  }
}

.fade-item-enter-active, .fade-item-leave-active {
  transition: all 0.5s ease;
}
.fade-item-enter-from, .fade-item-leave-to {
  opacity: 0;
  transform: translateY(20px);
}
</style>
