<script lang="ts" setup>
import type {Dict} from "@/types/types.ts";
import Book from "@/components/Book.vue";

defineProps<{
  list?: Partial<Dict>[],
  selectId?: string
  quantifier?: string
}>()

const emit = defineEmits<{
  selectDict: [val: { dict: any, index: number }]
  del: [val: { dict: any, index: number }]
  detail: [],
  add: []
}>()

</script>

<template>
  <div class="dict-list">
    <Book v-for="(dict,index) in list"
          :is-add="false"
          :item="dict"
          :quantifier="quantifier"
          @click="emit('selectDict',{dict,index})"/>
  </div>
</template>

<style lang="scss" scoped>
.dict-list { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: clamp(.8rem, 2vw, 1.25rem); align-items: stretch; }
@media (max-width: 560px) { .dict-list { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .7rem; } }
</style>
