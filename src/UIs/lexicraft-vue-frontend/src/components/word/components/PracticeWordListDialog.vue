<script lang="ts" setup>

import BaseTable from "@/components/BaseTable.vue";
import WordItem from "@/components/WordItem.vue";
import {defineAsyncComponent} from "vue";
import type {TaskWords} from "@/types/types.ts";
import Checkbox from "@/components/base/checkbox/Checkbox.vue";

const Dialog = defineAsyncComponent(() => import('@/components/dialog/Dialog.vue'))

const model = defineModel()
defineProps<{
  data: TaskWords
}>()

let showTranslate = $ref(false)

</script>

<template>
  <Dialog v-model="model" padding title="任务">
    <div class="pb-4 h-80vh flex gap-4">
      <div class="h-full flex flex-col gap-2">
        <div class="flex justify-between items-center">
          <span class="title">新词 {{data.new.length}} 个</span>
          <Checkbox v-model="showTranslate">翻译</Checkbox>
        </div>
        <BaseTable
            :list='data.new'
            :loading='false'
            :show-toolbar="false"
            :showPagination="false"
            class="overflow-auto flex-1 w-85"
        >
          <template v-slot="item">
            <WordItem
                :index="item.index"
                :item="item.item"
                :show-option="false"
                :show-translate="showTranslate"
            />
          </template>
        </BaseTable>
      </div>
      <div v-if="data.review.length" class="h-full flex flex-col gap-2">
        <div class="flex justify-between items-center">
          <span class="title">复习上次 {{data.review.length}} 个</span>
        </div>
        <BaseTable
            :list='data.review'
            :loading='false'
            :show-toolbar="false"
            :showPagination="false"
            class="overflow-auto flex-1 w-85"
        >
          <template v-slot="item">
            <WordItem
                :index="item.index"
                :item="item.item"
                :show-option="false"
                :show-translate="showTranslate"
            />
          </template>
        </BaseTable>
      </div>
      <div v-if="data.write.length" class="h-full flex flex-col gap-2">
        <div class="flex justify-between items-center">
          <span class="title">复习之前 {{data.write.length}} 个</span>
        </div>
        <BaseTable
            :list='data.write'
            :loading='false'
            :show-toolbar="false"
            :showPagination="false"
            class="overflow-auto flex-1 w-85"
        >
          <template v-slot="item">
            <WordItem
                :index="item.index"
                :item="item.item"
                :show-option="false"
                :show-translate="showTranslate"
            />
          </template>
        </BaseTable>
      </div>
    </div>
  </Dialog>
</template>

<style lang="scss" scoped>

</style>
