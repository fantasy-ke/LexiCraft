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
  <BasePage class="learning-page vocabulary-page">
    <header class="page-intro">
      <div><p>词汇学习</p><h1>今天只完成眼前这一组。</h1><span>先确认当前词典和任务量，再开始或继续练习。</span></div>
      <BaseButton type="info" @click="router.push('/app/dict-list')">浏览词典</BaseButton>
    </header>

    <section class="card-white study-overview">
      <div class="current-study">
        <div class="study-title-row"><div class="study-icon"><IconFluentBookNumber24Filled/></div><div><span class="section-kicker">当前词典</span><h2 @click="goDictDetail(store.sdict)">{{ store.sdict.name || '尚未选择词典' }}</h2></div></div>
        <template v-if="store.sdict.id">
          <div class="learning-progress">
            <div class="progress-heading"><div><span>学习进度</span><strong>{{ store.currentStudyProgress }}%</strong></div><small>{{ store.sdict?.lastLearnIndex }} / {{ store.sdict.words.length }} 词</small></div>
            <Progress :percentage="store.currentStudyProgress" :show-text="false"/>
            <div class="progress-meta"><span>{{ progressTextLeft }}</span><span>预计完成：{{ _getAccomplishDate(store.sdict.words.length - store.sdict.lastLearnIndex, store.sdict.perDayStudyNumber) }}</span></div>
          </div>
          <div class="learning-actions">
            <BaseButton size="small" type="info" @click="router.push('/app/dict-list')"><IconFluentArrowSwap24Regular/>更换词典</BaseButton>
            <PopConfirm :disabled="!isSaveData" title="更改进度将重新生成任务，是否继续？" @confirm="check(() => (showChangeLastPracticeIndexDialog = true))">
              <BaseButton size="small" type="info"><IconFluentSlideTextTitleEdit24Regular/>调整学习位置</BaseButton>
            </PopConfirm>
          </div>
        </template>
        <div v-else class="learning-empty"><p>选择一本词典后，这里会显示学习进度和预计完成时间。</p><BaseButton id="step1" @click="router.push('/app/dict-list')"><IconFluentAdd24Filled/>选择词典</BaseButton></div>
      </div>

      <aside class="practice-panel" :class="{'is-disabled': !store.sdict.id}">
        <div class="practice-heading"><div><span class="section-kicker">今日任务</span><h2>{{ isSaveData ? '继续未完成的练习' : '完成今天的学习目标' }}</h2></div><button type="button" @click="check(() => (showPracticeSettingDialog = true))">每日 {{ store.sdict.id ? store.sdict.perDayStudyNumber : 0 }} 词 · 调整</button></div>
        <button class="word-list-link" type="button" @click="showPracticeWordListDialog = true">查看本组完整词表 <IconFluentChevronRight20Regular/></button>
        <div class="task-stats"><div><strong>{{ currentStudy.new.length }}</strong><span>新词</span></div><div><strong>{{ currentStudy.review.length }}</strong><span>复习</span></div><div><strong>{{ currentStudy.write.length }}</strong><span>巩固</span></div></div>
        <div class="practice-actions">
          <BaseButton :disabled="!store.sdict.id" :loading="loading" @click="systemPractice">{{ systemPracticeText }} <IconFluentChevronCircleRight24Regular/></BaseButton>
          <BaseButton :disabled="!store.sdict.id" :loading="loading" type="info" @click="freePractice">自由练习</BaseButton>
        </div>
      </aside>
    </section>

    <section class="card-white learning-section">
      <header class="learning-section__head">
        <div><p class="section-kicker">我的内容</p><h2>我的词典</h2><span>管理已经添加的词典，点击卡片查看详情。</span></div>
        <div class="section-actions">
          <PopConfirm v-if="selectIds.length" title="确认要删除选中的词典吗？" @confirm="handleBatchDel"><BaseButton type="info" size="small"><DeleteIcon/>删除所选</BaseButton></PopConfirm>
          <BaseButton type="info" size="small" @click="isManageDict = !isManageDict; selectIds = []">{{ isManageDict ? '完成管理' : '批量管理' }}</BaseButton>
          <BaseButton size="small" @click="nav('/app/dict-detail', {isAdd: true})">新建词典</BaseButton>
        </div>
      </header>
      <div class="learning-grid">
        <Book v-for="(item, j) in store.word.bookList" :key="item.id" :checked="selectIds.includes(item.id)" :is-add="false" :item="item" :show-checkbox="isManageDict && j >= 3" quantifier="词" @check="() => toggleSelect(item)" @click="goDictDetail(item)"/>
        <Book :is-add="true" add-text="添加词典" @click="router.push('/app/dict-list')"/>
      </div>
    </section>

    <section v-loading="isFetching" class="card-white learning-section">
      <header class="learning-section__head"><div><p class="section-kicker">推荐内容</p><h2>推荐词典</h2><span>需要新内容时再从这里挑选，不打断当前任务。</span></div><BaseButton type="info" size="small" @click="router.push('/app/dict-list')">查看全部</BaseButton></header>
      <div class="learning-grid"><Book v-for="item in recommendDictList" :key="item.id" :is-add="false" :item="item as any" quantifier="词" @click="goDictDetail(item as any)"/></div>
    </section>
  </BasePage>
  <PracticeSettingDialog v-model="showPracticeSettingDialog" :show-left-option="false" @ok="savePracticeSetting"/>
  <ChangeLastPracticeIndexDialog v-model="showChangeLastPracticeIndexDialog" @ok="saveLastPracticeIndex"/>
  <PracticeWordListDialog v-model="showPracticeWordListDialog" :data="currentStudy"/>
  <ShufflePracticeSettingDialog v-model="showShufflePracticeSettingDialog" @ok="onShufflePracticeSettingOk"/>
</template>

<style lang="scss" scoped>
.learning-page { max-width: 1220px; margin: 0 auto; padding-bottom: 48px; font-family: var(--font-sans); }
.page-intro { display: flex; align-items: flex-end; justify-content: space-between; gap: 28px; margin-bottom: 22px; padding: 14px 2px 0; }
.page-intro p, .section-kicker { margin: 0; color: var(--accent); font-size: 10px; font-weight: 750; letter-spacing: .14em; text-transform: uppercase; }
.page-intro h1 { margin: 8px 0 0; color: var(--text-primary); font-family: var(--font-heading); font-size: clamp(30px, 4vw, 44px); font-weight: 620; letter-spacing: -.025em; }
.page-intro span, .learning-section__head > div > span { display: block; margin-top: 8px; color: var(--text-secondary); font-size: 13px; line-height: 1.65; }
.study-overview { display: grid; grid-template-columns: minmax(0, 1fr) minmax(340px, .82fr); gap: 0; margin-bottom: 22px; overflow: hidden; }
.current-study, .practice-panel { padding: clamp(24px, 3vw, 36px); }
.current-study { border-right: 1px solid var(--border-color); }
.study-title-row { display: flex; align-items: center; gap: 14px; }
.study-icon { display: grid; width: 44px; height: 44px; flex: 0 0 auto; place-items: center; border: 1px solid var(--border-color); border-radius: var(--radius-control); color: var(--accent); background: var(--surface-muted); font-size: 23px; }
.study-title-row h2, .practice-heading h2, .learning-section__head h2 { margin: 5px 0 0; color: var(--text-primary); font-family: var(--font-heading); font-size: 23px; font-weight: 650; }
.study-title-row h2 { cursor: pointer; }
.learning-progress { margin-top: 28px; padding: 20px; border: 1px solid var(--border-color); border-radius: var(--radius-control); background: var(--surface-muted); }
.progress-heading { display: flex; align-items: flex-end; justify-content: space-between; gap: 20px; margin-bottom: 14px; }
.progress-heading span, .progress-heading strong { display: block; }
.progress-heading span { color: var(--text-secondary); font-size: 11px; }
.progress-heading strong { margin-top: 3px; color: var(--text-primary); font-size: 25px; }
.progress-heading small, .progress-meta { color: var(--text-tertiary); font-size: 11px; }
.progress-meta { display: flex; justify-content: space-between; gap: 18px; margin-top: 12px; }
.learning-actions, .practice-actions, .section-actions { display: flex; flex-wrap: wrap; gap: 8px; }
.learning-actions { margin-top: 16px; }
.learning-actions :deep(.base-button + .base-button), .practice-actions :deep(.base-button + .base-button), .section-actions :deep(.base-button + .base-button) { margin-left: 0; }
.learning-empty { margin-top: 28px; padding: 24px; border: 1px dashed var(--border-strong); border-radius: var(--radius-control); background: var(--surface-muted); }
.learning-empty p { margin: 0 0 18px; color: var(--text-secondary); line-height: 1.7; }
.practice-panel { display: flex; min-width: 0; flex-direction: column; justify-content: space-between; background: color-mix(in srgb, var(--surface-muted) 64%, var(--surface-card)); }
.practice-panel.is-disabled { opacity: .62; }
.practice-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 20px; }
.practice-heading button, .word-list-link { border: 0; color: var(--text-secondary); background: transparent; cursor: pointer; font: inherit; font-size: 11px; }
.practice-heading button:hover, .word-list-link:hover { color: var(--accent); }
.word-list-link { display: flex; align-items: center; gap: 4px; width: max-content; margin-top: 16px; padding: 0; }
.task-stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; margin: 28px 0; }
.task-stats > div { padding: 15px; border: 1px solid var(--border-color); border-radius: var(--radius-control); background: var(--surface-card); }
.task-stats strong, .task-stats span { display: block; }
.task-stats strong { color: var(--text-primary); font-size: 22px; }
.task-stats span { margin-top: 5px; color: var(--text-tertiary); font-size: 11px; }
.practice-actions :deep(.base-button:first-child) { flex: 1.4; }
.practice-actions :deep(.base-button:last-child) { flex: 1; }
.learning-section { margin-bottom: 22px; padding: clamp(24px, 3vw, 34px); }
.learning-section:last-child { margin-bottom: 0; }
.learning-section__head { display: flex; align-items: flex-end; justify-content: space-between; gap: 24px; margin-bottom: 28px; padding-bottom: 20px; border-bottom: 1px solid var(--border-color); }
.learning-section__head h2 { font-size: 24px; }
.learning-grid { display: grid; grid-template-columns: repeat(6, minmax(0, 1fr)); gap: 16px; }
@media (max-width: 1080px) { .learning-grid { grid-template-columns: repeat(4, minmax(0, 1fr)); } }
@media (max-width: 860px) { .study-overview { grid-template-columns: 1fr; } .current-study { border-right: 0; border-bottom: 1px solid var(--border-color); } .learning-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); } }
@media (max-width: 640px) { .page-intro, .learning-section__head { align-items: stretch; flex-direction: column; } .page-intro :deep(.base-button) { align-self: flex-start; } .current-study, .practice-panel, .learning-section { padding: 22px 18px; } .practice-heading { flex-direction: column; } .progress-meta { flex-direction: column; gap: 5px; } .learning-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; } .section-actions { width: 100%; } }
</style>
