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
  <BasePage>
    <!-- Study Record Header Card -->
    <div class="card-white p-8 relative group mb-6">
      <div class="absolute inset-0 overflow-hidden rounded-xl pointer-events-none">
        <div class="absolute -right-20 -top-20 w-64 h-64 bg-indigo-500/10 rounded-full blur-3xl group-hover:bg-indigo-500/20 transition-colors duration-700"></div>
      </div>
      
      <div class="flex flex-col lg:flex-row gap-10 relative z-10">
        <!-- Current Book Focus -->
        <div class="shrink-0">
          <Book
            v-if="base.sbook.id"
            :is-add="false"
            :item="base.sbook"
            :show-progress="false"
            quantifier="篇"
            class="scale-110 lg:scale-100 origin-top shadow-2xl"
            @click="goBookDetail(base.sbook)"
          />
          <Book v-else :is-add="true" class="scale-110 lg:scale-100 origin-top shadow-2xl" @click="router.push('/app/book-list')"/>
        </div>

        <!-- Weekly & Stats -->
        <div class="flex-1 space-y-8">
          <div class="flex justify-between items-start">
            <div class="space-y-4">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-xl bg-blue-500/10 center text-blue-600">
                  <IconFluentCalendarWeekNumbers24Filled class="text-2xl"/>
                </div>
                <div>
                   <h2 class="text-2xl font-black m-0 tracking-tight">本周学习记录</h2>
                   <p class="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">WEEKLY ACTIVITY</p>
                </div>
              </div>
              
              <div class="flex gap-2">
                <div
                  v-for="(item, i) in weekList"
                  :key="i"
                  class="flex flex-col items-center gap-2"
                >
                   <div 
                     :class="item ? 'bg-gradient-to-br from-blue-500 to-indigo-600 shadow-lg shadow-blue-500/30' : 'bg-slate-100 dark:bg-slate-800'"
                     class="w-8 h-8 md:w-10 md:h-10 rounded-xl center transition-all duration-500 hover:scale-110"
                   >
                     <IconFluentCheckmark12Filled v-if="item" class="text-white text-lg"/>
                     <span v-else class="text-xs font-black text-slate-300">{{ i + 1 }}</span>
                   </div>
                </div>
              </div>
            </div>
            
            <BaseButton 
              v-opacity="base.sbook.id" 
              class="!h-10 !px-5 !rounded-2xl !bg-white dark:!bg-slate-800 !border-slate-100 dark:!border-slate-700 !text-slate-600 dark:!text-slate-300 shadow-sm hover:!bg-blue-50 dark:hover:!bg-blue-950/30 hover:!border-blue-200 dark:hover:!border-blue-800 hover:!text-blue-600 transition-all duration-300 group" 
              @click="router.push('/app/book-list')"
            >
               <div class="flex items-center gap-2">
                 <IconFluentArrowSwap24Regular class="text-lg group-hover:rotate-180 transition-transform duration-500"/>
                 <span class="font-black text-xs uppercase tracking-wider">Change Book</span>
               </div>
            </BaseButton>
          </div>

          <!-- Quick Stats -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div class="stat-card-premium">
              <div class="icon bg-blue-100 text-blue-600"><IconFluentTimer24Filled/></div>
              <div>
                 <div class="value">{{ todayTotalSpend }}</div>
                 <div class="label">今日时长</div>
              </div>
            </div>
            <div class="stat-card-premium">
              <div class="icon bg-indigo-100 text-indigo-600"><IconFluentFlash24Filled/></div>
              <div>
                 <div class="value">{{ totalDay }}</div>
                 <div class="label">累计天数</div>
              </div>
            </div>
            <div class="stat-card-premium">
              <div class="icon bg-emerald-100 text-emerald-600"><IconFluentHistory24Filled/></div>
              <div>
                 <div class="value text-emerald-600">{{ totalSpend }}</div>
                 <div class="label">总计时长</div>
              </div>
            </div>
          </div>

          <!-- Progress and Action -->
          <div class="flex flex-col sm:flex-row gap-6 items-center bg-slate-50 dark:bg-slate-800/40 p-6 rounded-3xl border border-slate-100 dark:border-slate-800">
            <div class="flex-1 w-full space-y-2">
               <div class="flex justify-between items-end">
                  <span class="text-sm font-black text-indigo-600 dark:text-indigo-400 uppercase tracking-widest">{{ base.currentBookProgress }}% 完成</span>
                  <span class="text-xs font-bold text-slate-400">{{ base.sbook?.lastLearnIndex || 0 }} / {{ base.sbook?.length || 0 }} 篇</span>
               </div>
               <Progress
                  :percentage="base.currentBookProgress"
                  :show-text="false"
                  class="h-3 rounded-full"
               ></Progress>
            </div>

            <BaseButton
              :disabled="!base.sbook.name"
              class="w-full sm:w-60 h-16 rounded-[1.25rem] bg-gradient-to-r from-slate-900 via-indigo-950 to-slate-900 border-none shadow-2xl shadow-slate-900/40 text-xl font-black flex items-center justify-center transition-all duration-500 hover:scale-[1.05] active:scale-[0.95] group relative overflow-hidden"
              @click="startStudy"
            >
               <div class="absolute inset-0 bg-gradient-to-r from-white/0 via-white/5 to-white/0 translate-x-[-100%] group-hover:translate-x-[100%] transition-transform duration-1000"></div>
               <div class="center gap-4 w-full relative z-10">
                 <div class="w-10 h-10 rounded-full bg-white/10 center group-hover:bg-white/20 transition-colors">
                   <IconFluentPlay24Filled class="text-xl group-hover:scale-125 transition-transform"/>
                 </div>
                 <span class="tracking-widest uppercase text-sm">{{ isSaveData ? 'Continue' : 'Start Path' }}</span>
               </div>
            </BaseButton>
          </div>
        </div>
      </div>
    </div>

    <!-- My Books Section -->
    <div class="card-white p-8 mb-6">
      <div class="flex justify-between items-center mb-10">
        <div>
          <h2 class="text-2xl font-black m-0 grad-text">我的书读</h2>
          <p class="text-sm font-bold text-slate-400 mt-1 uppercase tracking-widest">PERSONAL LIBRARY</p>
        </div>
        <div class="flex gap-4 items-center">
          <PopConfirm v-if="selectIds.length" title="确认要移出选中的书籍吗？" @confirm="handleBatchDel">
            <div class="w-10 h-10 rounded-xl bg-rose-50 text-rose-500 center cursor-pointer hover:bg-rose-100 transition-colors">
              <DeleteIcon class="text-xl"/>
            </div>
          </PopConfirm>

          <BaseButton type="info" size="small" class="rounded-xl px-4" @click="isMultiple = !isMultiple; selectIds = []">
            {{ isMultiple ? '完成管理' : '批量管理' }}
          </BaseButton>
          
          <BaseButton type="primary" size="small" class="rounded-xl px-6 bg-gradient-to-r from-blue-600 to-indigo-600 border-none font-bold" @click="nav('/app/book-detail', { isAdd: true })">
            添加书籍
          </BaseButton>
        </div>
      </div>
      
      <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-6">
        <Book
          v-for="(item, j) in base.article.bookList"
          :checked="selectIds.includes(item.id)"
          :is-add="false"
          :is-user="true"
          :item="item"
          :show-checkbox="isMultiple && j >= 1"
          quantifier="篇"
          class="hover:-translate-y-2 transition-transform duration-300"
          @check="() => toggleSelect(item)"
          @click="goBookDetail(item)"
        />
        <Book :is-add="true" class="hover:-translate-y-2 transition-transform duration-300" @click="router.push('/app/book-list')"/>
      </div>
    </div>

    <!-- Recommendations Section -->
    <div v-loading="isFetching" class="card-white p-8">
      <div class="flex justify-between items-center mb-10 border-b border-slate-50 dark:border-slate-800 pb-6">
        <div>
          <h2 class="text-2xl font-black m-0">精选书籍</h2>
          <p class="text-sm font-bold text-slate-400 mt-1 uppercase tracking-widest">HAND-PICKED FOR YOU</p>
        </div>
        <BaseButton type="info" size="small" class="rounded-xl px-6" @click="router.push('/app/book-list')">
          发现更多
        </BaseButton>
      </div>

      <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-6">
        <Book
          v-for="(item, j) in recommendBookList"
          :is-add="false"
          :item="item as any"
          quantifier="篇"
          class="hover:-translate-y-2 transition-transform duration-300"
          @click="goBookDetail(item as any)"
        />
      </div>
    </div>
  </BasePage>
</template>

<style lang="scss" scoped>
.stat-card-premium {
  @apply flex items-center gap-4 p-5 rounded-3xl bg-slate-50 dark:bg-slate-800/40 border border-slate-100 dark:border-slate-800 transition-all duration-300;
  
  .icon {
    @apply w-12 h-12 rounded-2xl center text-2xl shrink-0;
  }
  
  .value {
    @apply text-xl font-black text-slate-900 dark:text-white leading-tight;
  }
  
  .label {
    @apply text-[10px] font-black text-slate-400 uppercase tracking-widest mt-1;
  }
  
  &:hover {
    @apply -translate-y-1 shadow-lg shadow-slate-200/50 dark:shadow-none bg-white dark:bg-slate-800;
  }
}
</style>
