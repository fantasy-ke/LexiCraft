<template>
  <div class="card check-in-card">
    <div class="card-header">
      <h3 class="title">每日打卡</h3>
      <button class="icon-btn">
        <IconFluentSettings20Regular/>
      </button>
    </div>

    <div class="check-in-content">
      <div class="stats-row">
        <div class="stat-box">
          <div class="label">连签</div>
          <div class="value purple">{{ stats.consecutiveDays }} <span class="unit">天</span></div>
        </div>
        <div class="stat-box right">
          <div class="label">累计打卡</div>
          <div class="value">{{ stats.totalDays }} <span class="unit">天</span></div>
        </div>
      </div>

      <div class="week-circles">
        <div v-for="(day, index) in weekDays" :key="index" :class="{ 'active': day.checked, 'today': day.isToday }"
             class="week-day">
          <div class="day-label">{{ day.label }}</div>
          <div class="day-circle">
            <IconFluentCheckmark12Regular v-if="day.checked"/>
          </div>
        </div>
      </div>

      <div class="action-buttons">
        <button class="calendar-btn">
          <IconFluentCalendarDate20Regular/>
          打卡日历
        </button>
        <button class="check-in-btn">
          <IconFluentHandWave20Regular/>
          长按签到
        </button>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import {computed} from 'vue'

const props = defineProps<{
  stats: {
    consecutiveDays: number
    totalDays: number
    weekHistory: boolean[]
  }
}>()

const weekDays = computed(() => {
  const labels = ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
  const todayIndex = 5 // 假设今天是周六
  return labels.map((label, index) => ({
    label,
    checked: props.stats.weekHistory[index],
    isToday: index === todayIndex
  }))
})
</script>

<style lang="scss" scoped>
@use './styles/check-in-card.scss';
</style>
