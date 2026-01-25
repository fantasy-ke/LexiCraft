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

const model = defineModel()

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
    <div id="mode" class="target-modal color-main">
      <div class="text-center mt-4">
        <span
        >共<span class="target-number mx-2">{{ runtimeStore.editDict.length }}</span
        >个单词，</span
        >
        <span
        >预计<span class="target-number mx-2">{{
            _getAccomplishDays(
              runtimeStore.editDict.length - tempLastLearnIndex,
              tempPerDayStudyNumber
            )
          }}</span
        >天完成</span
        >
      </div>

      <div class="text-center mt-4 mb-8 flex gap-1 items-end justify-center">
        <span>从第</span>
        <div class="w-20">
          <BaseInput v-model="tempLastLearnIndex" class="target-number"/>
        </div>
        <span>个开始，每日</span>
        <div class="w-16">
          <BaseInput v-model="tempPerDayStudyNumber" class="target-number"/>
        </div>
        <span>个新词</span>
        <span>，复习</span>
        <div class="target-number mx-2">
          {{ tempPerDayStudyNumber * tempWordReviewRatio }}
        </div>
        <span>个</span>
      </div>

      <div class="mb-4 space-y-2">
        <div class="flex items-center gap-space">
          <Tooltip title="复习词与新词的比例">
            <div class="flex items-center gap-1 w-20 break-keep">
              <span>复习比</span>
              <IconFluentQuestionCircle20Regular/>
            </div>
          </Tooltip>
          <InputNumber v-model="tempWordReviewRatio" :max="10" :min="0"/>
        </div>
        <div v-if="!tempWordReviewRatio" class="flex">
          <div class="w-23 flex-shrink-0"></div>
          <div class="text-sm text-gray-500">
            <div>未完成学习时，复习数量按照设置的复习比生成，为0则不复习</div>
            <div>完成学习后，新词数量固定为0，复习数量按照比例生成（若复习比小于1，以 1 计算）</div>
          </div>
        </div>
      </div>

      <div class="flex mb-4 gap-space">
        <span class="shrink-0 w-20">每日学习</span>
        <Slider
            v-model="tempPerDayStudyNumber"
            :max="200"
            :min="10"
            :step="10"
            class="mt-1"
            show-text
        />
      </div>
      <div class="flex gap-space">
        <span class="shrink-0 w-20">学习进度</span>
        <div class="flex-1">
          <Slider
              v-model="tempLastLearnIndex"
              :max="runtimeStore.editDict.words.length"
              :min="0"
              :step="10"
              class="my-1"
              show-text
          />
          <BaseButton @click="show = true">从词典选起始位置</BaseButton>
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
  width: 35rem;

  .mode-item {
    @apply w-50% border border-blue border-solid p-2 rounded-lg cursor-pointer;
  }

  .active {
    @apply bg-blue color-white;
  }
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
