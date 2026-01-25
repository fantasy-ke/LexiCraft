<script lang="ts" setup>
import {watch} from "vue";
import type {DictResource} from "@/types/types.ts";
import DictList from "@/components/list/DictList.vue";

const props = defineProps<{
  category: string,
  groupByTag: any,
  selectId: string
}>()
const emit = defineEmits<{
  selectDict: [val: { dict: DictResource, index: number }]
  detail: [],
}>()
const tagList = $computed(() => Object.keys(props.groupByTag))
let currentTag = $ref(tagList[0])
let list = $computed(() => {
  return props.groupByTag[currentTag]
})

watch(() => props.groupByTag, () => {
  currentTag = tagList[0]
})

</script>

<template>
  <div>
    <div class="flex">
      <div class="category shrink-0">{{ category }}：</div>
      <div class="tags">
        <div v-for="i in Object.keys(groupByTag)" :class="i === currentTag &&'active'"
             class="tag"
             @click="currentTag = i">{{ i }}
        </div>
      </div>
    </div>

    <DictList
        :list="list"
        :select-id="selectId"
        @selectDict="e => emit('selectDict',e)"/>
  </div>
</template>

<style lang="scss" scoped>

.category {
  margin-top: 1.4rem;
  font-weight: bold;
  color: var(--color-font-1);
}

.tags {
  display: flex;
  flex-wrap: wrap;
  margin: 1rem 0;
  margin-top: calc(1rem - 0.4rem);

  .tag {
    color: var(--color-font-1);
    cursor: pointer;
    padding: 0.4rem 1rem;
    border-radius: 2rem;

    &.active {
      color: var(--color-font-active-1);
      background: gray;
    }
  }
}

// 移动端适配
@media (max-width: 768px) {
  .flex.items-center {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;

    .category {
      font-size: 1rem;
      font-weight: bold;
    }

    .tags {
      margin: 0.5rem 0;
      gap: 0.3rem;

      .tag {
        padding: 0.3rem 0.8rem;
        font-size: 0.9rem;
        min-height: 44px;
        min-width: 44px;
        display: flex;
        align-items: center;
        justify-content: center;
      }
    }
  }
}

// 超小屏幕适配
@media (max-width: 480px) {
  .flex.items-center {
    .category {
      font-size: 0.9rem;
    }

    .tags {
      .tag {
        padding: 0.2rem 0.6rem;
        font-size: 0.8rem;
      }
    }
  }
}

</style>
