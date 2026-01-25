<template>
  <div class="card heatmap-card">
    <div class="card-header">
      <h3 class="title">学习热力图</h3>
      <div class="date-selector">
        <span class="date-text">2026年1月</span>
        <IconFluentChevronRight20Regular/>
      </div>
    </div>

    <div class="heatmap-grid">
      <div class="heatmap-header">
        <span>一</span><span>二</span><span>三</span><span>四</span><span>五</span><span>六</span><span>日</span>
      </div>
      <div class="heatmap-body">
        <div
            v-for="(day, i) in heatmapData"
            :key="i"
            :class="{ 
            'active': day.isToday, 
            'level-1': day.level === 1, 
            'level-2': day.level === 2,
            'level-3': day.level === 3 
          }"
            :title="day.date + ' 学习' + day.count + '次'"
            class="heatmap-cell"
        ></div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
defineProps<{
  heatmapData: Array<{
    level: number
    count: number
    date: string
    isToday: boolean
  }>
}>()
</script>

<style lang="scss" scoped>
@use './styles/heatmap-card.scss';
</style>
