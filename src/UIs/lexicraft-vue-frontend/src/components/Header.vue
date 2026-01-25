<script lang="ts" setup>

import BackIcon from "@/components/BackIcon.vue";
import {useAttrs} from "vue";

interface IProps {
  title: string;
  showBackIcon?: boolean;
}

withDefaults(defineProps<IProps>(), {
  title: '',
  showBackIcon: true,
})


const attrs = useAttrs()
</script>

<template>
  <div class="mb-3 text-xl font-bold relative min-h-8">
    <BackIcon v-if="showBackIcon" class="z-2 relative" v-bind="attrs"/>
    <span class="absolute text-center w-full left-0" @click.stop>{{ title }}</span>
  </div>
</template>

<style lang="scss" scoped>

</style>