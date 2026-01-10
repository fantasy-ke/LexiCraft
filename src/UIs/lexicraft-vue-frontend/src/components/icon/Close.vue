<script setup lang="ts">
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

<style scoped lang="scss">
.close {
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.1rem;
}
</style>
