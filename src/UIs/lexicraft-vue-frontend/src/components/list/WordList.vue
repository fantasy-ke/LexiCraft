<script lang="ts" setup>

import BaseList from "@/components/list/BaseList.vue";
import type {Word} from "@/types/types.ts";
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
      :list="list"
      v-bind="$attrs"
      @click="(e:any) => emit('click',e)">
    <template v-slot="{ item, index, active }">
      <WordItem
          :active="active"
          :index="index"
          :item="item" :show-translate="showTranslate" :show-word="showWord"/>
    </template>
  </BaseList>
</template>
