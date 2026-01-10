<script setup lang="ts">

import { GITHUB } from "@/config/env.ts";
import BaseIcon from "@/components/BaseIcon.vue";
</script>

<template>
  <a :href="GITHUB" target="_blank" rel="noreferrer" aria-label="GITHUB 项目地址"
     class="color-[--color-reverse-black] github">
    <BaseIcon title="Github">
      <IconSimpleIconsGithub/>
    </BaseIcon>
  </a>
</template>

<style scoped lang="scss">

</style>