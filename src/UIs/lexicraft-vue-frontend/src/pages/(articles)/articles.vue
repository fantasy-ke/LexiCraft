<script setup lang="ts">
import { myDictList } from '@/apis'
import Progress from '@/components/base/Progress.vue'
import Toast from '@/components/base/toast/Toast.ts'
import BaseButton from '@/components/BaseButton.vue'
import BaseIcon from '@/components/BaseIcon.vue'
import BasePage from '@/components/BasePage.vue'
import Book from '@/components/Book.vue'
import DeleteIcon from '@/components/icon/DeleteIcon.vue'
import PopConfirm from '@/components/PopConfirm.vue'
import { AppEnv, DICT_LIST, Host, LIB_JS_URL, TourConfig } from '@/config/env.ts'
import { useBaseStore } from '@/stores/base.ts'
import { useRuntimeStore } from '@/stores/runtime.ts'
import { useSettingStore } from '@/stores/setting.ts'
import { getDefaultDict } from '@/types/func.ts'
import type { DictResource } from '@/types/types.ts'
import {
  _getDictDataByUrl,
  _nextTick,
  isMobile,
  loadJsLib,
  msToHourMinute,
  resourceWrap,
  total,
  useNav,
} from '@/utils'
import { getPracticeArticleCache } from '@/utils/cache.ts'
import { useFetch } from '@vueuse/core'
import dayjs from 'dayjs'
import isBetween from 'dayjs/plugin/isBetween'
import isoWeek from 'dayjs/plugin/isoWeek'
import { watch } from 'vue'
import { useRouter } from 'vue-router'
import { DictType } from '@/types/enum.ts'

dayjs.extend(isoWeek)
dayjs.extend(isBetween)

const { nav } = useNav()
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
  { immediate: true }
)

async function init() {
  if (AppEnv.CAN_REQUEST) {
    let res = await myDictList({ type: 'article' })
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
                nav('/app/practice-articles/article_nce2', { guide: 1 })
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
  { immediate: true }
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
  nav('/app/book-detail',{id: val.id})
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

const { data: recommendBookList, isFetching } = useFetch(
  resourceWrap(DICT_LIST.ARTICLE.RECOMMENDED)
).json()

let isNewHost = $ref(window.location.host === Host)
</script>

<template>
  <BasePage>
    <div class="card flex flex-col md:flex-row justify-between gap-space p-4 md:p-6">
      <div class="">
        <Book
          v-if="base.sbook.id"
          :is-add="false"
          quantifier="篇"
          :item="base.sbook"
          :show-progress="false"
          @click="goBookDetail(base.sbook)"
        />
        <Book v-else :is-add="true" @click="router.push('/app/book-list')" />
      </div>
      <div class="flex-1">
        <div class="flex justify-between items-start">
          <div class="flex items-center min-w-0">
            <div class="title mr-4 truncate">本周学习记录</div>
            <div class="flex gap-4 color-gray">
              <div
                class="w-6 h-6 md:w-8 md:h-8 rounded-md center text-sm md:text-base"
                :class="item ? 'bg-[#409eff] color-white' : 'bg-gray-200'"
                v-for="(item, i) in weekList"
                :key="i"
              >
                {{ i + 1 }}
              </div>
            </div>
          </div>
          <div class="flex gap-4 items-center" v-opacity="base.sbook.id">
            <div class="color-link cursor-pointer" @click="router.push('/app/book-list')">更换</div>
          </div>
        </div>
        <div class="flex flex-col sm:flex-row gap-3 items-center mt-3 gap-space w-full">
          <div
            class="w-full sm:flex-1 rounded-xl p-4 box-border relative bg-[var(--bg-history)] border border-gray-200"
          >
            <div class="text-[#409eff] text-xl font-bold">{{ todayTotalSpend }}</div>
            <div class="text-gray-500">今日学习时长</div>
          </div>
          <div
            class="w-full sm:flex-1 rounded-xl p-4 box-border relative bg-[var(--bg-history)] border border-gray-200"
          >
            <div class="text-[#409eff] text-xl font-bold">{{ totalDay }}</div>
            <div class="text-gray-500">总学习天数</div>
          </div>
          <div
            class="w-full sm:flex-1 rounded-xl p-4 box-border relative bg-[var(--bg-history)] border border-gray-200"
          >
            <div class="text-[#409eff] text-xl font-bold">{{ totalSpend }}</div>
            <div class="text-gray-500">总学习时长</div>
          </div>
        </div>
        <div class="flex gap-3 mt-3">
          <Progress
            class="w-full md:w-auto"
            size="large"
            :percentage="base.currentBookProgress"
            :format="() => `${base.sbook?.lastLearnIndex || 0}/${base.sbook?.length || 0}篇`"
            :show-text="true"
          ></Progress>

          <BaseButton
            size="large"
            class="w-full md:w-auto"
            @click="startStudy"
            :disabled="!base.sbook.name"
          >
            <div class="flex items-center gap-2 justify-center w-full">
              <span class="line-height-[2]">{{ isSaveData ? '继续学习' : '开始学习' }}</span>
              <IconFluentArrowCircleRight16Regular class="text-xl" />
            </div>
          </BaseButton>
        </div>
      </div>
    </div>

    <div class="card flex flex-col">
      <div class="flex justify-between">
        <div class="title">我的书籍</div>
        <div class="flex gap-4 items-center">
          <PopConfirm
            title="确认删除所有选中书籍？"
            @confirm="handleBatchDel"
            v-if="selectIds.length"
          >
            <BaseIcon class="del" title="删除">
              <DeleteIcon />
            </BaseIcon>
          </PopConfirm>

          <div
            class="color-link cursor-pointer"
            v-if="base.article.bookList.length > 1"
            @click="
             () => {
              isMultiple = !isMultiple
              selectIds = []
             }
            "
          >
            {{ isMultiple ? '取消' : '管理书籍' }}
          </div>
          <div class="color-link cursor-pointer" @click="nav('/app/book-detail', { isAdd: true })">
            创建个人书籍
          </div>
        </div>
      </div>
      <div class="flex gap-4 flex-wrap mt-4">
        <Book
          :is-add="false"
          :is-user="true"
          quantifier="篇"
          :item="item"
          :checked="selectIds.includes(item.id)"
          @check="() => toggleSelect(item)"
          :show-checkbox="isMultiple && j >= 1"
          v-for="(item, j) in base.article.bookList"
          @click="goBookDetail(item)"
        />
        <Book :is-add="true" @click="router.push('/app/book-list')" />
      </div>
    </div>

    <div class="card flex flex-col min-h-50" v-loading="isFetching">
      <div class="flex justify-between">
        <div class="title">推荐</div>
        <div class="flex gap-4 items-center">
          <div class="color-link cursor-pointer" @click="router.push('/app/book-list')">更多</div>
        </div>
      </div>

      <div class="flex gap-4 flex-wrap mt-4">
        <Book
          :is-add="false"
          quantifier="篇"
          :item="item as any"
          v-for="(item, j) in recommendBookList"
          @click="goBookDetail(item as any)"
        />
      </div>
    </div>
  </BasePage>
</template>

<style scoped lang="scss">
.stat {
  @apply rounded-xl p-4 box-border relative flex-1 bg-[var(--bg-history)];
  border: 1px solid gainsboro;

  .num {
    @apply color-[#409eff] text-xl font-bold;
  }

  .txt {
    @apply color-gray-500;
  }
}
</style>
