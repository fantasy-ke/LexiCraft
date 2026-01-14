<template>
  <div class="card stats-card">
    <div class="card-header">
      <h3 class="title">学习统计</h3>
      <div class="filter-tabs">
        <span 
          class="tab" 
          v-for="tab in tabs" 
          :key="tab"
          :class="{ active: currentTab === tab }"
          @click="$emit('update:currentTab', tab)"
        >
          {{ tab }}
        </span>
      </div>
    </div>
    
    <div class="stats-content">
      <div class="stat-block">
        <div class="label">学习时长</div>
        <div class="value">{{ stats.duration }}</div>
      </div>
      <div class="divider"></div>
      <div class="stat-block">
        <div class="label">完成课程</div>
        <div class="value">{{ stats.completed }} <span class="unit">节</span></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  currentTab: string
  stats: {
    duration: string
    completed: number
  }
}>()

defineEmits<{
  (e: 'update:currentTab', value: string): void
}>()

const tabs = ['总计', '本周', '本月', '今年']
</script>

<style scoped lang="scss">
@import './styles/stats-card.scss';
</style>
