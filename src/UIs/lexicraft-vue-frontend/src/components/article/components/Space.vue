<script lang="ts" setup>
import {useSettingStore} from "@/stores/setting.ts";

const props = withDefaults(defineProps<{
  isWrong: boolean,
  isWait?: boolean,
  isShake?: boolean,
}>(), {
  isWrong: false,
  isShake: false,
})
const settingStore = useSettingStore()
const isMoveBottom = $computed(() => {
  return settingStore.dictation && !props.isWrong
})
</script>

<template>
  <span v-if="isWrong" class="word-space wrong"></span>
  <span v-else v-bind="$attrs">
    <span :class="[
           isWait ? 'opacity-100':' opacity-0',
           isShake ? isMoveBottom ? 'shakeBottom' : 'shake' : '',
           isMoveBottom && 'to-bottom'
       ]"
          class="word-space wait"
    ></span>
  </span>
</template>

<style lang="scss" scoped>
.word-space {
  position: relative;
  display: inline-block;
  width: 0.6rem;
  height: 1.5rem;
  box-sizing: border-box;
  margin: 0 1px;
  border-bottom: 2px solid transparent;

  &.wrong {
    border-bottom: 2px solid red;
  }

  &.to-bottom {
    transform: translateY(0.3rem);
  }

  &.wait {
    border-bottom: 2px solid var(--color-article);
    margin-left: 0.125rem;
    margin-right: 0.125rem;

    &::after {
      content: ' ';
      position: absolute;
      width: 2px;
      height: .25rem;
      background: var(--color-article);
      bottom: 0;
      right: 0;
    }

    &::before {
      content: ' ';
      position: absolute;
      width: 2px;
      height: .26rem;
      background: var(--color-article);
      bottom: 0;
      left: 0;
    }
  }
}

.shake {
  border-bottom: 2px solid red !important;
  animation: shake 0.82s cubic-bezier(0.36, 0.07, 0.19, 0.97) both;

  &::after {
    background: red !important;
  }

  &::before {
    background: red !important;
  }
}

.shakeBottom {
  @extend .shake;
  animation: shakeBottom 0.82s cubic-bezier(0.36, 0.07, 0.19, 0.97) both;
}

</style>
