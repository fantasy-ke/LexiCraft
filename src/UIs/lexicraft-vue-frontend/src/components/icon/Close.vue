<script lang="ts" setup>
import Tooltip from "@/components/base/Tooltip.vue";

defineEmits(['click'])
defineProps<{
  title?: string
}>()
</script>

<template>
  <div class="close"
       @click="$emit('click')"
  >
    <Tooltip :title="title">
      <IconFluentDismissCircle16Regular/>
    </Tooltip>
  </div>
</template>

<style lang="scss" scoped>
.close {
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.1rem;
}
</style>
