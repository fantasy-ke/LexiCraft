<script lang="ts" setup>

import type {Article} from "@/types/types.ts";
import {useDisableEventListener} from "@/hooks/event.ts";
import EditArticle from "@/components/article/components/EditArticle.vue";
import {getDefaultArticle} from "@/types/func.ts";
import {defineAsyncComponent} from "vue";

const Dialog = defineAsyncComponent(() => import('@/components/dialog/Dialog.vue'))

interface IProps {
  article?: Article
  modelValue: boolean
}

const props = withDefaults(defineProps<IProps>(), {
  article: () => getDefaultArticle(),
  modelValue: false
})
const emit = defineEmits<{
  save: [val: Article]
  'update:modelValue': [val: boolean]
}>()

useDisableEventListener(() => props.modelValue)

</script>

<template>
  <Dialog
      :full-screen="true"
      :header="false"
      :model-value="props.modelValue"
      @close="emit('update:modelValue',false)"
  >
    <div class="wrapper">
      <EditArticle
          :article="article"
          @save="val => emit('save',val)"
      />
    </div>
  </Dialog>
</template>

<style lang="scss" scoped>

.wrapper {
  width: 100%;
  height: 100%;
  display: flex;
  background: var(--color-primary);
}
</style>
