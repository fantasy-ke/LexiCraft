<script lang="ts" setup>
const props = defineProps<{
  q: string,
  a?: string | string[],
}>()
let show = $ref(false)
let isArray = $computed(() => typeof props.a !== 'string')
</script>

<template>
  <div class="qa-item my-6">
    <header class="flex justify-between items-center cp font-bold text-lg" @click="show = !show">
      <span>{{ q }}</span>
      <IconFluentChevronLeft20Filled :class="show?'transform-rotate-270':'transform-rotate-180'" class="anim"/>
    </header>
    <div v-if="show" class="content mt-4 text-base">
      <template v-if="isArray">
        <p v-for="(v,i) in a">{{a.length>1?`${i+1}. `:''}}{{v}}</p>
      </template>
      <span v-else>{{a}}</span>
      <slot></slot>
    </div>
  </div>
</template>

<style lang="scss" scoped>

</style>