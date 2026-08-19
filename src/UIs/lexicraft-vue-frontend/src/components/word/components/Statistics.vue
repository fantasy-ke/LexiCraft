<script lang="ts" setup>
import {useBaseStore} from '@/stores/base.ts'
import BaseButton from '@/components/BaseButton.vue'
import type {Statistics, TaskWords} from '@/types/types.ts'
import {emitter, EventKey, useEvents} from '@/utils/eventBus.ts'
import {useSettingStore} from '@/stores/setting.ts'
import {usePracticeStore} from '@/stores/practice.ts'
import dayjs from 'dayjs'
import isBetween from 'dayjs/plugin/isBetween'
import {defineAsyncComponent, inject, watch} from 'vue'
import isoWeek from 'dayjs/plugin/isoWeek'
import {msToHourMinute} from '@/utils'
import Progress from '@/components/base/Progress.vue'
import ChannelIcons from '@/components/ChannelIcons/ChannelIcons.vue'
import {AppEnv} from '@/config/env.ts'
import {addStat} from '@/apis'
import Toast from '@/components/base/toast/Toast.ts'
import {ShortcutKey, WordPracticeMode} from '@/types/enum.ts'

dayjs.extend(isoWeek)
dayjs.extend(isBetween)
const Dialog = defineAsyncComponent(() => import('@/components/dialog/Dialog.vue'))

const store = useBaseStore()
const settingStore = useSettingStore()
const statStore = usePracticeStore()
const model = defineModel({default: false})
let list = $ref([])
let dictIsEnd = $ref(false)
let practiceTaskWords = inject<TaskWords>('practiceTaskWords')

function calcWeekList() {
  // 获取本周的起止时间
  const startOfWeek = dayjs().startOf('isoWeek') // 周一
  const endOfWeek = dayjs().endOf('isoWeek') // 周日
  // 初始化 7 天的数组，默认 false
  const weekList = Array(7).fill(false)

  store.sdict.statistics.forEach(item => {
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
      weekList[idx] = true
    }
  })
  list = weekList
}

// 监听 model 弹窗打开时重新计算
watch(model, async newVal => {
  if (newVal) {
    dictIsEnd = false
    let data: Statistics = {
      spend: statStore.spend,
      startDate: statStore.startDate,
      total: statStore.total,
      wrong: statStore.wrong,
      new: statStore.newWordNumber,
      review: statStore.reviewWordNumber + statStore.writeWordNumber,
      title: store.sdict.name,
    }
    window.umami?.track('endStudyWord', {
      name: store.sdict.name,
      spend: Number(statStore.spend / 1000 / 60).toFixed(1),
      index: store.sdict.lastLearnIndex,
      perDayStudyNumber: store.sdict.perDayStudyNumber,
      custom: store.sdict.custom,
      complete: store.sdict.complete,
      str: `name:${store.sdict.name},per:${store.sdict.perDayStudyNumber},spend:${Number(statStore.spend / 1000 / 60).toFixed(1)},index:${store.sdict.lastLearnIndex}`,
    })

    //如果 shuffle 数组不为空，就说明是复习，不用修改 lastLearnIndex
    if (settingStore.wordPracticeMode !== WordPracticeMode.Shuffle) {
      store.sdict.lastLearnIndex = store.sdict.lastLearnIndex + statStore.newWordNumber
      // 检查已忽略的单词数量，是否全部完成
      let ignoreList = [store.allIgnoreWords, store.knownWords][settingStore.ignoreSimpleWord ? 0 : 1]
      // 忽略单词数
      const ignoreCount = ignoreList.filter(word => store.sdict.words.some(w => w.word.toLowerCase() === word)).length
      // 如果lastLearnIndex已经超过可学单词数，则判定完成
      if (store.sdict.lastLearnIndex + ignoreCount >= store.sdict.length) {
        dictIsEnd = true
        store.sdict.complete = true
        store.sdict.lastLearnIndex = store.sdict.length
      }
    }

    if (AppEnv.CAN_REQUEST) {
      let res = await addStat({
        ...data,
        type: 'word',
        perDayStudyNumber: store.sdict.perDayStudyNumber,
        lastLearnIndex: store.sdict.lastLearnIndex,
        complete: store.sdict.complete,
      })
      if (!res.success) {
        Toast.error(res.msg)
      }
    }

    store.sdict.statistics.push(data as any)
    calcWeekList() // 新增：计算本周学习记录
  }
})

const close = () => (model.value = false)

useEvents([
  //特意注释掉，因为在练习界面用快捷键下一组时，需要判断是否在结算界面
  // [ShortcutKey.NextChapter, close],
  [ShortcutKey.RepeatChapter, close],
  [ShortcutKey.DictationChapter, close],
])

function options(emitType: string) {
  emitter.emit(EventKey[emitType])
  close()
}

// 计算学习进度百分比
const studyProgress = $computed(() => {
  if (!store.sdict.length) return 0
  return Math.round((store.sdict.lastLearnIndex / store.sdict.length) * 100)
})

// 计算正确率
const accuracyRate = $computed(() => {
  if (statStore.total === 0) return 100
  return Math.round(((statStore.total - statStore.wrong) / statStore.total) * 100)
})

// 获取鼓励文案
const encouragementText = $computed(() => {
  const rate = accuracyRate
  if (rate >= 95) return '🎉 太棒了！继续保持！'
  if (rate >= 85) return '👍 表现很好，再接再厉！'
  if (rate >= 70) return '💪 不错的成绩，继续加油！'
  return '🌟 每次练习都是进步，坚持下去！'
})

// 格式化学习时间
const formattedStudyTime = $computed(() => {
  const time = msToHourMinute(statStore.spend)
  return time.replace('小时', 'h ').replace('分钟', 'm')
})

calcWeekList() // 新增：计算本周学习记录
</script>

<template>
  <Dialog v-model="model" :close-on-click-bg="false" :header="false" :keyboard="false" :show-close="false">
    <div class="p-8 pr-3 bg-[var(--bg-card-primary)] rounded-2xl space-y-6">
      <!-- Header Section -->
      <div class="text-center relative">
        <div
            class="text-3xl font-bold mb-2 bg-gradient-to-r from-purple-500 to-purple-700 bg-clip-text text-transparent"
        >
          <template v-if="practiceTaskWords.shuffle.length"> 🎯 复习完成</template>
          <template v-else> 🎉 今日任务完成</template>
        </div>
        <p class="font-medium text-lg">{{ encouragementText }}</p>
      </div>

      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div class="item">
          <IconFluentClock20Regular class="text-purple-500"/>
          <div class="text-sm mb-1 font-medium">学习时长</div>
          <div class="text-xl font-bold">{{ formattedStudyTime }}</div>
        </div>

        <div class="item">
          <IconFluentTarget20Regular class="text-purple-500"/>
          <div class="text-sm mb-1 font-medium">正确率</div>
          <div class="text-xl font-bold">{{ accuracyRate }}%</div>
        </div>

        <div class="item">
          <IconFluentSparkle20Regular class="text-purple-500"/>
          <div class="text-sm mb-1 font-medium">新词</div>
          <div class="text-xl font-bold">{{ statStore.newWordNumber }}</div>
        </div>

        <div class="item">
          <IconFluentBook20Regular class="text-purple-500"/>
          <div class="text-sm mb-1 font-medium">复习</div>
          <div class="text-xl font-bold">
            {{ statStore.reviewWordNumber + statStore.writeWordNumber }}
          </div>
        </div>
      </div>

      <div class="w-full gap-3 flex">
        <div class="space-y-6 flex-1">
          <!-- Weekly Progress -->
          <div class="bg-[--bg-card-secend] rounded-xl p-2">
            <div class="text-center mb-4">
              <div class="text-xl font-semibold mb-1">本周学习记录</div>
            </div>
            <div class="flex justify-between gap-4">
              <div
                  v-for="(item, i) in list"
                  :key="i"
                  :class="item ? 'bg-green-500 text-white shadow-lg' : 'bg-white text-gray-700'"
                  class="flex-1 text-center px-2 py-3 rounded-lg"
              >
                <div class="font-semibold mb-1">{{ i + 1 }}</div>
                <div
                    :class="item ? 'bg-white bg-opacity-30' : 'bg-gray-300'"
                    class="w-2 h-2 rounded-full mx-auto mb-1"
                ></div>
              </div>
            </div>
          </div>

          <!-- Progress Overview -->
          <div class="bg-[var(--bg-card-secend)] rounded-xl py-2 px-6">
            <div class="flex justify-between items-center mb-3">
              <div class="text-xl font-semibold">学习进度</div>
              <div class="text-2xl font-bold text-purple-600">{{ studyProgress }}%</div>
            </div>
            <Progress :percentage="studyProgress" :show-text="false" size="large"/>
            <div class="flex justify-between text-sm font-medium mt-4">
              <span>已学习: {{ store.sdict.lastLearnIndex }}</span>
              <span>总词数: {{ store.sdict.length }}</span>
            </div>
          </div>
        </div>
        <ChannelIcons/>
      </div>
      <!-- Action Buttons -->
      <div class="flex min-w-130 justify-center">
        <BaseButton
            :keyboard="settingStore.shortcutKeyMap[ShortcutKey.RepeatChapter]"
            @click="options(EventKey.repeatStudy)"
        >
          <div class="center gap-2">
            <IconFluentArrowClockwise20Regular/>
            重学一遍
          </div>
        </BaseButton>
        <BaseButton
            v-if="settingStore.wordPracticeMode !== WordPracticeMode.Review"
            :keyboard="settingStore.shortcutKeyMap[ShortcutKey.NextChapter]"
            @click="options(EventKey.continueStudy)"
        >
          <div class="center gap-2">
            <IconFluentPlay20Regular/>
            {{ dictIsEnd ? '从头开始练习' : '再来一组' }}
          </div>
        </BaseButton>
        <BaseButton @click="$router.back">
          <div class="center gap-2">
            <IconFluentHome20Regular/>
            返回主页
          </div>
        </BaseButton>
      </div>
    </div>
  </Dialog>
</template>
<style lang="scss" scoped>
// 移动端适配
@media (max-width: 768px) {
  // 弹窗容器优化
  .w-140 {
    width: 90vw !important;
    max-width: 500px;
    padding: 1.5rem !important;
  }

  // 标题优化
  .center.text-2xl {
    font-size: 1.3rem;
    margin-bottom: 1rem;
  }

  // 统计数据布局
  .flex .flex-1 {
    .text-sm {
      font-size: 0.8rem;
    }

    .text-4xl {
      font-size: 2rem;
    }
  }

  // 时间显示
  .text-xl {
    font-size: 1rem;

    .text-2xl {
      font-size: 1.5rem;
    }
  }

  // 错词/正确统计卡片
  .flex.justify-center.gap-10 {
    gap: 1rem;
    flex-wrap: wrap;

    > div {
      padding: 0.8rem 2rem;

      .text-3xl {
        font-size: 1.8rem;
      }
    }
  }

  // 本周学习记录
  .flex.gap-4 {
    gap: 0.5rem;

    .w-8.h-8 {
      width: 2rem;
      height: 2rem;
      font-size: 0.9rem;
    }
  }

  // 按钮组
  .flex.justify-center.gap-4 {
    flex-direction: column;
    gap: 0.5rem;

    .base-button {
      width: 100%;
      min-height: 48px;
    }
  }
}

@media (max-width: 480px) {
  .w-140 {
    width: 95vw !important;
    padding: 1rem !important;
  }

  .flex .flex-1 {
    .text-4xl {
      font-size: 1.5rem;
    }
  }
}
</style>

<style scoped>
.item {
  @apply bg-[var(--bg-card-secend)] rounded-xl p-2 text-center border border-gray-100;
}
</style>
