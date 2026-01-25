<script lang="ts" setup>

import {GITHUB} from "@/config/env.ts";
import BaseIcon from "@/components/BaseIcon.vue";
</script>

<template>
  <a :href="GITHUB" aria-label="GITHUB 项目地址" class="color-[--color-reverse-black] github" rel="noreferrer"
     target="_blank">
    <BaseIcon title="Github">
      <IconSimpleIconsGithub/>
    </BaseIcon>
  </a>
</template>

<style lang="scss" scoped>

</style>