<script lang="ts" setup>
import {_getAccomplishDays} from '@/utils'
import BaseButton from '@/components/BaseButton.vue'
import Checkbox from '@/components/base/checkbox/Checkbox.vue'
import Slider from '@/components/base/Slider.vue'
import {defineAsyncComponent, watch} from 'vue'
import {useSettingStore} from '@/stores/setting'
import Toast from '@/components/base/toast/Toast'
import ChangeLastPracticeIndexDialog from '@/components/word/components/ChangeLastPracticeIndexDialog.vue'
import Tooltip from '@/components/base/Tooltip.vue'
import {useRuntimeStore} from '@/stores/runtime'
import BaseInput from '@/components/base/BaseInput.vue'
import InputNumber from '@/components/base/InputNumber.vue'

const Dialog = defineAsyncComponent(() => import('@/components/dialog/Dialog.vue'))

const settings = useSettingStore()
const runtimeStore = useRuntimeStore()

const model = defineModel<boolean>()

defineProps<{
  showLeftOption: boolean
}>()

const emit = defineEmits<{
  ok: []
}>()

let show = $ref(false)
let tempPerDayStudyNumber = $ref(0)
let tempWordReviewRatio = $ref(0)
let tempLastLearnIndex = $ref(0)
let tempDisableShowPracticeSettingDialog = $ref(false)

function changePerDayStudyNumber() {
  runtimeStore.editDict.perDayStudyNumber = tempPerDayStudyNumber
  runtimeStore.editDict.lastLearnIndex = tempLastLearnIndex
  settings.wordReviewRatio = tempWordReviewRatio
  settings.disableShowPracticeSettingDialog = tempDisableShowPracticeSettingDialog
  emit('ok')
}

watch(
    () => model.value,
    n => {
      if (n) {
        if (runtimeStore.editDict.id) {
          tempPerDayStudyNumber = runtimeStore.editDict.perDayStudyNumber
          tempLastLearnIndex = runtimeStore.editDict.lastLearnIndex
          tempWordReviewRatio = settings.wordReviewRatio
          tempDisableShowPracticeSettingDialog = settings.disableShowPracticeSettingDialog
        } else {
          Toast.warning('请先选择一本词典')
        }
      }
    }
)
</script>

<template>
  <Dialog v-model="model" :footer="true" padding title="学习设置" @ok="changePerDayStudyNumber">
    <div id="mode" class="target-modal p-4">
      <!-- Header Stats -->
      <div class="bg-blue-50 dark:bg-blue-900/20 rounded-3xl p-6 mb-8 border border-blue-100 dark:border-blue-800/50">
        <div class="flex items-center justify-around text-center">
          <div class="space-y-1">
            <div class="text-[10px] font-black text-blue-500 uppercase tracking-[0.2em]">Total Words</div>
            <div class="text-3xl font-black text-slate-900 dark:text-white">{{ runtimeStore.editDict.length }}</div>
          </div>
          <div class="w-px h-10 bg-blue-200 dark:bg-blue-800"></div>
          <div class="space-y-1">
            <div class="text-[10px] font-black text-indigo-500 uppercase tracking-[0.2em]">Estimated Days</div>
            <div class="text-3xl font-black text-slate-900 dark:text-white">
              {{ _getAccomplishDays(runtimeStore.editDict.length - tempLastLearnIndex, tempPerDayStudyNumber) }}
            </div>
          </div>
        </div>
      </div>

      <!-- Quick Summary Sentence -->
      <div class="bg-slate-50 dark:bg-slate-800/40 rounded-2xl p-4 mb-8 flex flex-wrap items-center justify-center gap-2 text-slate-600 dark:text-slate-300 font-bold border border-slate-100 dark:border-slate-800">
        <span>从第</span>
        <div class="w-20">
          <BaseInput v-model="tempLastLearnIndex" class="!bg-white dark:!bg-slate-900 !rounded-xl text-center font-black text-blue-600"/>
        </div>
        <span>个开始</span>
        <span class="opacity-30">|</span>
        <span>每日新词</span>
        <div class="w-16">
          <BaseInput v-model="tempPerDayStudyNumber" class="!bg-white dark:!bg-slate-900 !rounded-xl text-center font-black text-blue-600"/>
        </div>
        <span class="opacity-30">|</span>
        <span>复习</span>
        <span class="text-indigo-600 font-black text-xl">{{ Math.floor(tempPerDayStudyNumber * tempWordReviewRatio) }}</span>
        <span>个</span>
      </div>

      <!-- Detailed Settings -->
      <div class="space-y-8">
        <!-- Review Ratio -->
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <div class="w-8 h-8 rounded-lg bg-indigo-100 dark:bg-indigo-900/30 center text-indigo-600">
                <IconFluentArrowSync24Regular class="text-lg"/>
              </div>
              <span class="font-black text-slate-700 dark:text-slate-200 uppercase tracking-widest text-xs">复习比例</span>
              <Tooltip title="复习词与新词的比例 (建议 1:1 或 1:2)">
                <IconFluentQuestionCircle20Regular class="text-slate-400 cursor-help"/>
              </Tooltip>
            </div>
            <div class="bg-indigo-50 dark:bg-indigo-900/20 px-3 py-1 rounded-lg text-indigo-600 font-black text-sm">
              {{ tempWordReviewRatio.toFixed(1) }}x
            </div>
          </div>
          <Slider v-model="tempWordReviewRatio" :max="5" :min="0" :step="0.1" class="px-2" />
          <p class="text-[10px] text-slate-500 font-bold italic pl-10">
            * {{ tempWordReviewRatio === 0 ? '当前模式不包含复习任务' : `每学习 1 个新词，将安排 ${tempWordReviewRatio} 个复习任务` }}
          </p>
        </div>

        <!-- Daily Count -->
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <div class="w-8 h-8 rounded-lg bg-blue-100 dark:bg-blue-900/30 center text-blue-600">
                <IconFluentAddSquare24Regular class="text-lg"/>
              </div>
              <span class="font-black text-slate-700 dark:text-slate-200 uppercase tracking-widest text-xs">每日学习量</span>
            </div>
            <div class="bg-blue-50 dark:bg-blue-900/20 px-3 py-1 rounded-lg text-blue-600 font-black text-sm">
              {{ tempPerDayStudyNumber }} words
            </div>
          </div>
          <Slider v-model="tempPerDayStudyNumber" :max="200" :min="10" :step="10" class="px-2" />
        </div>

        <!-- Start Position -->
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <div class="w-8 h-8 rounded-lg bg-emerald-100 dark:bg-emerald-900/30 center text-emerald-600">
                <IconFluentFlag24Regular class="text-lg"/>
              </div>
              <span class="font-black text-slate-700 dark:text-slate-200 uppercase tracking-widest text-xs">起始位置</span>
            </div>
            <BaseButton size="small" class="h-8 rounded-lg text-[10px] font-black uppercase tracking-widest px-3 border-emerald-100 text-emerald-600 hover:bg-emerald-50 transition-colors" @click="show = true">
              <IconFluentList24Regular class="mr-1"/> 词表选择
            </BaseButton>
          </div>
          <div class="flex items-center gap-4">
             <Slider v-model="tempLastLearnIndex" :max="runtimeStore.editDict.words.length" :min="0" :step="1" class="flex-1 px-2" />
             <div class="bg-emerald-50 dark:bg-emerald-900/20 px-3 py-1 rounded-lg text-emerald-600 font-black text-sm shrink-0">
               {{ tempLastLearnIndex }} / {{ runtimeStore.editDict.length }}
             </div>
          </div>
        </div>
      </div>
    </div>
    <template v-if="showLeftOption" v-slot:footer-left>
      <div class="flex items-center">
        <Checkbox v-model="tempDisableShowPracticeSettingDialog"/>
        <Tooltip title="可在设置页面更改">
          <span class="text-sm">保持默认，不再显示</span>
        </Tooltip>
      </div>
    </template>
  </Dialog>
  <ChangeLastPracticeIndexDialog
      v-model="show"
      @ok="
      e => {
        tempLastLearnIndex = e
        show = false
      }
    "
  />
</template>

<style lang="scss" scoped>
.target-modal {
  width: 40rem;
  max-width: 100%;
}

// 移动端适配
@media (max-width: 768px) {
  .target-modal {
    width: 90vw !important;
    max-width: 400px;
    padding: 0 1rem;

    // 模式选择
    .center .flex.gap-4 {
      width: 100%;
      flex-direction: column;
      height: auto;
      gap: 0.8rem;

      .mode-item {
        width: 100%;
        padding: 1rem;

        .title {
          font-size: 1rem;
        }

        .desc {
          font-size: 0.85rem;
          margin-top: 0.5rem;
        }
      }
    }

    // 统计显示
    .text-center {
      font-size: 0.9rem;

      .text-3xl {
        font-size: 1.5rem;
      }
    }

    // 滑块控件
    .flex.mb-4,
    .flex.mb-6 {
      flex-direction: column;
      align-items: flex-start;
      gap: 0.5rem;

      span {
        width: 100%;
      }

      .flex-1 {
        width: 100%;
      }
    }

    // 按钮
    .base-button {
      width: 100%;
      min-height: 44px;
    }
  }
}

@media (max-width: 480px) {
  .target-modal {
    width: 95vw !important;
    padding: 0 0.5rem;

    .text-center {
      font-size: 0.8rem;

      .text-3xl {
        font-size: 1.2rem;
      }
    }
  }
}
</style>
