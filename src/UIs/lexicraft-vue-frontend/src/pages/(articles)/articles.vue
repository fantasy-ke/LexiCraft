<script lang="ts" setup>
import {myDictList} from '@/apis'
import Progress from '@/components/base/Progress.vue'
import Toast from '@/components/base/toast/Toast.ts'
import BaseButton from '@/components/BaseButton.vue'
import BaseIcon from '@/components/BaseIcon.vue'
import BasePage from '@/components/BasePage.vue'
import Book from '@/components/Book.vue'
import DeleteIcon from '@/components/icon/DeleteIcon.vue'
import PopConfirm from '@/components/PopConfirm.vue'
import {AppEnv, DICT_LIST, Host, LIB_JS_URL, TourConfig} from '@/config/env.ts'
import {useBaseStore} from '@/stores/base.ts'
import {useRuntimeStore} from '@/stores/runtime.ts'
import {useSettingStore} from '@/stores/setting.ts'
import {getDefaultDict} from '@/types/func.ts'
import type {DictResource} from '@/types/types.ts'
import {_getDictDataByUrl, _nextTick, isMobile, loadJsLib, msToHourMinute, resourceWrap, total, useNav,} from '@/utils'
import {getPracticeArticleCache} from '@/utils/cache.ts'
import {useFetch} from '@vueuse/core'
import dayjs from 'dayjs'
import isBetween from 'dayjs/plugin/isBetween'
import isoWeek from 'dayjs/plugin/isoWeek'
import {watch} from 'vue'
import {useRouter} from 'vue-router'
import {DictType} from '@/types/enum.ts'

dayjs.extend(isoWeek)
dayjs.extend(isBetween)

const {nav} = useNav()
const base = useBaseStore()
const store = useBaseStore()
const settingStore = useSettingStore()
const router = useRouter()
const runtimeStore = useRuntimeStore()
let isSaveData = $ref(false)

watch(
    () => store.load,
    n => {
      if (n) init()
    },
    {immediate: true}
)

async function init() {
  if (AppEnv.CAN_REQUEST) {
    let res = await myDictList({type: 'article'})
    if (res.success) {
      store.setState(Object.assign(store.$state, res.data))
    }
  }
  if (store.article.studyIndex >= 1) {
    if (!store.sbook.custom && !store.sbook.articles.length) {
      store.article.bookList[store.article.studyIndex] = await _getDictDataByUrl(
          store.sbook,
          DictType.article
      )
    }
  }
  let d = getPracticeArticleCache()
  if (d) {
    isSaveData = true
  }
}

watch(
    () => store?.sbook?.id,
    n => {
      if (!n) {
        _nextTick(async () => {
          const Shepherd = await loadJsLib('Shepherd', LIB_JS_URL.SHEPHERD)
          const tour = new Shepherd.Tour(TourConfig)
          tour.on('cancel', () => {
            localStorage.setItem('tour-guide', '1')
          })
          tour.addStep({
            id: 'step7',
            text: '点击这里选择一本书籍开始学习，步骤前面选词典相同，让我们跳过中间步骤，直接开始练习吧',
            attachTo: {
              element: '#no-book',
              on: 'bottom',
            },
            buttons: [
              {
                text: `下一步（7/${TourConfig.total}）`,
                action() {
                  tour.next()
                  nav('/app/practice-articles/article_nce2', {guide: 1})
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
    },
    {immediate: true}
)

function startStudy() {
  // console.log(store.sbook.articles[1])
  // genArticleSectionData(cloneDeep(store.sbook.articles[1]))
  // return
  if (base.sbook.id) {
    if (!base.sbook.articles.length) {
      return Toast.warning('没有文章可学习！')
    }
    window.umami?.track('startStudyArticle', {
      name: base.sbook.name,
      custom: base.sbook.custom,
      complete: base.sbook.complete,
      s: `name:${base.sbook.name},index:${base.sbook.lastLearnIndex},title:${base.sbook.articles[base.sbook.lastLearnIndex].title}`,
    })
    nav('/app/practice-articles/' + store.sbook.id)
  } else {
    window.umami?.track('no-book')
    Toast.warning('请先选择一本书籍')
  }
}

let isMultiple = $ref(false)
let selectIds = $ref([])

function handleBatchDel() {
  selectIds.forEach(id => {
    let r = base.article.bookList.findIndex(v => v.id === id)
    if (r !== -1) {
      if (base.article.studyIndex === r) {
        base.article.studyIndex = -1
      }
      if (base.article.studyIndex > r) {
        base.article.studyIndex--
      }
      base.article.bookList.splice(r, 1)
    }
  })
  selectIds = []
  Toast.success('删除成功！')
}

function toggleSelect(item) {
  let rIndex = selectIds.findIndex(v => v === item.id)
  if (rIndex > -1) {
    selectIds.splice(rIndex, 1)
  } else {
    selectIds.push(item.id)
  }
}

async function goBookDetail(val: DictResource) {
  runtimeStore.editDict = getDefaultDict(val)
  nav('/app/book-detail', {id: val.id})
}

const totalSpend = $computed(() => {
  if (base.sbook.statistics?.length) {
    return msToHourMinute(total(base.sbook.statistics, 'spend'))
  }
  return 0
})
const todayTotalSpend = $computed(() => {
  if (base.sbook.statistics?.length) {
    return msToHourMinute(
        total(
            base.sbook.statistics.filter(v => dayjs(v.startDate).isSame(dayjs(), 'day')),
            'spend'
        )
    )
  }
  return 0
})

const totalDay = $computed(() => {
  if (base.sbook.statistics?.length) {
    return new Set(base.sbook.statistics.map(v => dayjs(v.startDate).format('YYYY-MM-DD'))).size
  }
  return 0
})

const weekList = $computed(() => {
  const list = Array(7).fill(false)

  // 获取本周的起止时间
  const startOfWeek = dayjs().startOf('isoWeek') // 周一
  const endOfWeek = dayjs().endOf('isoWeek') // 周日

  store.sbook.statistics?.forEach(item => {
    const date = dayjs(item.startDate)
    if (date.isBetween(startOfWeek, endOfWeek, null, '[]')) {
      let idx = date.day()
      // dayjs().day() 0=周日, 1=周一, ..., 6=周六
      // 需要转换为 0=周一, ..., 6=周日
      if (idx === 0) {
        idx = 6 // 周日放到最后
      } else {
        idx = idx - 1 // 其余前移一位
      }
      list[idx] = true
    }
  })
  return list
})

const {data: recommendBookList, isFetching} = useFetch(
    resourceWrap(DICT_LIST.ARTICLE.RECOMMENDED)
).json()

let isNewHost = $ref(window.location.host === Host)
</script>

<template>
  <BasePage class="learning-page reading-page">
    <header class="page-intro">
      <div><p>文章阅读</p><h1>从当前这篇继续读。</h1><span>书籍、阅读进度和继续按钮保持在同一条主线上。</span></div>
      <BaseButton type="info" @click="router.push('/app/book-list')">浏览书籍</BaseButton>
    </header>

    <section class="card-white reading-overview">
      <div class="current-book">
        <span class="section-kicker">当前书籍</span>
        <div class="current-book__content">
          <Book v-if="base.sbook.id" :is-add="false" :item="base.sbook" :show-progress="false" quantifier="篇" @click="goBookDetail(base.sbook)"/>
          <Book v-else :is-add="true" add-text="选择书籍" @click="router.push('/app/book-list')"/>
          <div class="book-summary">
            <h2>{{ base.sbook.name || '尚未选择书籍' }}</h2>
            <p v-if="base.sbook.id">当前阅读 {{ base.sbook?.lastLearnIndex || 0 }} / {{ base.sbook?.length || 0 }} 篇</p>
            <p v-else>选择一本书后，这里会显示本周记录和阅读进度。</p>
            <BaseButton v-if="base.sbook.id" size="small" type="info" @click="router.push('/app/book-list')"><IconFluentArrowSwap24Regular/>更换书籍</BaseButton>
          </div>
        </div>
      </div>

      <div class="reading-status">
        <div class="weekly-heading"><div><span class="section-kicker">本周记录</span><h2>保持稳定的阅读节奏</h2></div><small>{{ weekList.filter(Boolean).length }} / 7 天</small></div>
        <div class="week-list" aria-label="本周学习记录">
          <div v-for="(item, i) in weekList" :key="i" :class="{done: item}"><span>{{ ['一', '二', '三', '四', '五', '六', '日'][i] }}</span><IconFluentCheckmark12Filled v-if="item"/></div>
        </div>
        <div class="reading-stats"><div><strong>{{ todayTotalSpend }}</strong><span>今日时长</span></div><div><strong>{{ totalDay }}</strong><span>累计天数</span></div><div><strong>{{ totalSpend }}</strong><span>总计时长</span></div></div>
        <div class="reading-progress">
          <div class="reading-progress__bar"><div><span>整本进度</span><strong>{{ base.currentBookProgress }}%</strong></div><small>{{ base.sbook?.lastLearnIndex || 0 }} / {{ base.sbook?.length || 0 }} 篇</small><Progress :percentage="base.currentBookProgress" :show-text="false"/></div>
          <BaseButton :disabled="!base.sbook.name" @click="startStudy">{{ isSaveData ? '继续阅读' : '开始阅读' }} <IconFluentPlay24Filled/></BaseButton>
        </div>
      </div>
    </section>

    <section class="card-white learning-section">
      <header class="learning-section__head">
        <div><p class="section-kicker">我的内容</p><h2>我的书架</h2><span>点击书籍查看详情，需要整理时再进入批量管理。</span></div>
        <div class="section-actions">
          <PopConfirm v-if="selectIds.length" title="确认要移出选中的书籍吗？" @confirm="handleBatchDel"><BaseButton type="info" size="small"><DeleteIcon/>移出所选</BaseButton></PopConfirm>
          <BaseButton type="info" size="small" @click="isMultiple = !isMultiple; selectIds = []">{{ isMultiple ? '完成管理' : '批量管理' }}</BaseButton>
          <BaseButton size="small" @click="nav('/app/book-detail', {isAdd: true})">添加书籍</BaseButton>
        </div>
      </header>
      <div class="learning-grid">
        <Book v-for="(item, j) in base.article.bookList" :key="item.id" :checked="selectIds.includes(item.id)" :is-add="false" :is-user="true" :item="item" :show-checkbox="isMultiple && j >= 1" quantifier="篇" @check="() => toggleSelect(item)" @click="goBookDetail(item)"/>
        <Book :is-add="true" add-text="添加书籍" @click="router.push('/app/book-list')"/>
      </div>
    </section>

    <section v-loading="isFetching" class="card-white learning-section">
      <header class="learning-section__head"><div><p class="section-kicker">推荐内容</p><h2>精选书籍</h2><span>完成当前阅读后，再从这里选择下一本书。</span></div><BaseButton type="info" size="small" @click="router.push('/app/book-list')">查看全部</BaseButton></header>
      <div class="learning-grid"><Book v-for="item in recommendBookList" :key="item.id" :is-add="false" :item="item as any" quantifier="篇" @click="goBookDetail(item as any)"/></div>
    </section>
  </BasePage>
</template>

<style lang="scss" scoped>
.learning-page { max-width: 1220px; margin: 0 auto; padding-bottom: 48px; font-family: var(--font-sans); }
.page-intro { display: flex; align-items: flex-end; justify-content: space-between; gap: 28px; margin-bottom: 22px; padding: 14px 2px 0; }
.page-intro p, .section-kicker { margin: 0; color: var(--accent); font-size: 10px; font-weight: 750; letter-spacing: .14em; text-transform: uppercase; }
.page-intro h1 { margin: 8px 0 0; color: var(--text-primary); font-family: var(--font-heading); font-size: clamp(30px, 4vw, 44px); font-weight: 620; letter-spacing: -.025em; }
.page-intro span, .learning-section__head > div > span { display: block; margin-top: 8px; color: var(--text-secondary); font-size: 13px; line-height: 1.65; }
.reading-overview { display: grid; grid-template-columns: minmax(300px, .72fr) minmax(0, 1.28fr); margin-bottom: 22px; overflow: hidden; }
.current-book, .reading-status { padding: clamp(24px, 3vw, 36px); }
.current-book { border-right: 1px solid var(--border-color); background: color-mix(in srgb, var(--surface-muted) 54%, var(--surface-card)); }
.current-book__content { display: grid; grid-template-columns: minmax(120px, 150px) 1fr; gap: 22px; align-items: start; margin-top: 15px; }
.book-summary h2, .weekly-heading h2, .learning-section__head h2 { margin: 5px 0 0; color: var(--text-primary); font-family: var(--font-heading); font-size: 23px; font-weight: 650; }
.book-summary p { margin: 10px 0 18px; color: var(--text-secondary); font-size: 12px; line-height: 1.65; }
.weekly-heading { display: flex; align-items: flex-end; justify-content: space-between; gap: 20px; }
.weekly-heading small { color: var(--text-tertiary); font-size: 11px; }
.week-list { display: grid; grid-template-columns: repeat(7, 1fr); gap: 7px; margin-top: 22px; }
.week-list > div { display: grid; min-height: 46px; place-items: center; border: 1px solid var(--border-color); border-radius: var(--radius-control); color: var(--text-tertiary); background: var(--surface-muted); font-size: 11px; }
.week-list > div.done { border-color: var(--accent); color: var(--accent); background: var(--accent-soft); }
.week-list svg { width: 13px; height: 13px; }
.reading-stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; margin-top: 18px; }
.reading-stats > div { padding: 14px; border: 1px solid var(--border-color); border-radius: var(--radius-control); background: var(--surface-card); }
.reading-stats strong, .reading-stats span { display: block; }
.reading-stats strong { color: var(--text-primary); font-size: 17px; }
.reading-stats span { margin-top: 5px; color: var(--text-tertiary); font-size: 10px; }
.reading-progress { display: flex; align-items: flex-end; gap: 18px; margin-top: 20px; padding-top: 20px; border-top: 1px solid var(--border-color); }
.reading-progress__bar { min-width: 0; flex: 1; }
.reading-progress__bar > div { display: flex; align-items: baseline; justify-content: space-between; gap: 16px; margin-bottom: 5px; }
.reading-progress__bar span, .reading-progress__bar small { color: var(--text-tertiary); font-size: 10px; }
.reading-progress__bar strong { color: var(--text-primary); font-size: 19px; }
.reading-progress__bar small { display: block; margin-bottom: 10px; }
.learning-section { margin-bottom: 22px; padding: clamp(24px, 3vw, 34px); }
.learning-section:last-child { margin-bottom: 0; }
.learning-section__head { display: flex; align-items: flex-end; justify-content: space-between; gap: 24px; margin-bottom: 28px; padding-bottom: 20px; border-bottom: 1px solid var(--border-color); }
.learning-section__head h2 { font-size: 24px; }
.section-actions { display: flex; flex-wrap: wrap; gap: 8px; }
.section-actions :deep(.base-button + .base-button) { margin-left: 0; }
.learning-grid { display: grid; grid-template-columns: repeat(6, minmax(0, 1fr)); gap: 16px; }
@media (max-width: 1080px) { .learning-grid { grid-template-columns: repeat(4, minmax(0, 1fr)); } }
@media (max-width: 900px) { .reading-overview { grid-template-columns: 1fr; } .current-book { border-right: 0; border-bottom: 1px solid var(--border-color); } .learning-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); } }
@media (max-width: 640px) { .page-intro, .learning-section__head { align-items: stretch; flex-direction: column; } .page-intro :deep(.base-button) { align-self: flex-start; } .current-book, .reading-status, .learning-section { padding: 22px 18px; } .current-book__content { grid-template-columns: 118px 1fr; gap: 15px; } .reading-progress { align-items: stretch; flex-direction: column; } .reading-progress :deep(.base-button) { width: 100%; } .learning-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; } .section-actions { width: 100%; } }
</style>
