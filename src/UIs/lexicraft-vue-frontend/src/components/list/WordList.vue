<script setup lang="ts">

import BaseList from "@/components/list/BaseList.vue";
import type { Word } from "@/types/types.ts";
import WordItem from "../WordItem.vue";

withDefaults(defineProps<{
  list: Word[],
  showTranslate?: boolean
  showWord?: boolean
}>(), {
  list: [],
  showTranslate: true,
  showWord: true
})

const emit = defineEmits<{
  click: [val: { item: Word, index: number }],
}>()

const listRef: any = $ref(null as any)

function scrollToBottom() {
  listRef?.scrollToBottom()
}

function scrollToItem(index: number) {
  listRef?.scrollToItem(index)
}

defineExpose({scrollToBottom, scrollToItem})

</script>

<template>
  <BaseList
      ref="listRef"
      @click="(e:any) => emit('click',e)"
      :list="list"
      v-bind="$attrs">
      <template v-slot="{ item, index, active }">
        <WordItem
          :show-translate="showTranslate"
          :show-word="showWord"
          :item="item" :index="index" :active="active" />
      </template>
  </BaseList>
</template>
