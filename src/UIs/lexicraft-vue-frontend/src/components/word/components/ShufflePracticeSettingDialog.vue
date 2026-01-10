<script setup lang="ts">

import Slider from "@/components/base/Slider.vue";
import {defineAsyncComponent, watch} from "vue";
import {useBaseStore} from "@/stores/base.ts";

const Dialog = defineAsyncComponent(() => import('@/components/dialog/Dialog.vue'))

const store = useBaseStore()

const model = defineModel()

const emit = defineEmits<{
  ok: [val: number];
}>()

let num = $ref(0)
let min = $ref(0)

watch(() => model.value, (n) => {
  if (n) {
    num = Math.floor(store.sdict.lastLearnIndex / 3)
    num = num > 50 ? 50 : num
    min = num < 10 ? num : 10
  }
})
</script>

<template>
  <Dialog v-model="model" title="随机复习设置"
          :footer="true"
          :padding="true"
          @ok="emit('ok',num)">
    <div class="w-120 color-main">
      <div class="flex gap-4 items-end mb-2">
        <span>随机复习：<span class="font-bold">{{ store.sdict.name }}</span></span>
        <span class="target-number">{{ num }}</span>个单词
      </div>
      <div class="flex gap-space">
        <span class="shrink-0">随机数量：</span>
        <Slider :min="min"
                :step="10"
                show-text
                class="mt-1"
                :max="store.sdict.lastLearnIndex"
                v-model="num"/>
      </div>
      <div class="text-right">
        <span class="text-sm text-gray-500">只能复习已学习过的单词</span>
      </div>
    </div>
  </Dialog>
</template>

<style scoped lang="scss">

</style>
