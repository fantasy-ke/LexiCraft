<script setup lang="ts">

import BaseIcon from "@/components/BaseIcon.vue";
import { useAttrs } from "vue";
import { useNav } from "@/utils";

const attrs = useAttrs()
const router = useNav()

function onClick() {
  if (!attrs.onClick) {
    router.back()
  }
}
</script>

<template>
  <BaseIcon
      title="返回"
      @click="onClick"
  >
    <IconFluentChevronLeft28Filled/>
  </BaseIcon>
</template>

<style scoped lang="scss">

</style>
