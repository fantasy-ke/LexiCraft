<script lang="ts" setup>

</script>

<template>
  <div class="base-page">
    <div class="page">
      <slot></slot>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.base-page { display: flex; width: 100%; justify-content: center; }
.page { width: min(100%, 1240px); min-width: 0; margin: 0 auto; padding: 0 2px 20px; box-sizing: border-box; }

@media (max-width: 768px) {
  .page { width: 100%; padding: 0 2px 14px; }
}
</style>
