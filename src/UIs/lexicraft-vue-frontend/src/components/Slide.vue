<script lang="ts" setup>
const props = defineProps<{
  width?: string,
  height?: string,
  slideCount: number,
  step: number
}>()

const style = $computed(() => {
  return {
    width: props.slideCount * 100 + '%',
    transform: `translate3d(-${100 / props.slideCount * props.step}%, 0, 0)`
  }
})

</script>

<template>
  <div class="slide">
    <div :style="style"
         class="slide-list">
      <slot></slot>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.slide {
  width: 100%;
  height: 100%;
  overflow: hidden;

  .slide-list {
    width: 100%;
    height: 100%;
    display: flex;
    transition: all .3s;
  }
}

</style>
