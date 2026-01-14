<template>
  <div class="dashboard">
    <!-- 第一行：打卡、统计、热力图 -->
    <div class="top-section-grid">
      <CheckInCard :stats="checkInStats" />
      <StatsCard v-model:currentTab="currentStatTab" :stats="currentStats" />
      <HeatmapCard :heatmapData="heatmapData" />
    </div>

    <!-- 第二行：课程和最近学习 -->
    <div class="bottom-section-grid">
      <CoursesCard 
        :courses="myCourses" 
        @selectCourse="navigateToCourse"
        @goToShop="goToShop"
      />
      <RecentCard :items="recentItems" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useBaseStore } from '@/stores/base'
import { Dict } from '@/types/types'
import CheckInCard from '@/components/dashboard/CheckInCard.vue'
import StatsCard from '@/components/dashboard/StatsCard.vue'
import HeatmapCard from '@/components/dashboard/HeatmapCard.vue'
import CoursesCard from '@/components/dashboard/CoursesCard.vue'
import RecentCard from '@/components/dashboard/RecentCard.vue'

const router = useRouter()
const baseStore = useBaseStore()

// 我的课程数据
const myCourses = computed(() => {
  return baseStore.word.bookList.slice(3) || []
})

const navigateToCourse = (course: Dict) => {
  baseStore.changeDict(course)
  router.push('/app/study-word')
}

const goToShop = () => {
  router.push('/app/dict-list')
}

// 模拟数据
const FAKE_DATA = {
  checkIn: {
    consecutiveDays: 5,
    totalDays: 48,
    weekHistory: [false, true, true, true, true, true, false]
  },
  stats: {
    '总计': { duration: '128h', completed: 42 },
    '本周': { duration: '5h 20m', completed: 2 },
    '本月': { duration: '24h', completed: 8 },
    '今年': { duration: '128h', completed: 42 }
  },
  heatmap: {
    levels: [
      0,0,1,2, 1,3,2,1, 
      0,2,3,3, 1,0,0,1, 
      2,2,1,0, 3,3,2,1, 
      1,0,1,0
    ]
  },
  recent: [
    {
      id: 1,
      title: '【小初】 2025词...',
      subtitle: 'Unit 4 D字母开头的...',
      time: '大约 4小时前',
      cover: 'https://images.unsplash.com/photo-1546410531-bb4caa6b424d?ixlib=rb-1.2.1&auto=format&fit=crop&w=100&q=60'
    },
    {
      id: 2,
      title: '10篇短文搞定考...',
      subtitle: '27.A Russian Poem',
      time: '大约 4小时前',
      cover: 'https://images.unsplash.com/photo-1516979187457-637abb4f9353?ixlib=rb-1.2.1&auto=format&fit=crop&w=100&q=60'
    },
    {
      id: 3,
      title: '【小初】 2025词...',
      subtitle: 'Unit 1 A字母开头的...',
      time: '大约 22小时前',
      cover: 'https://images.unsplash.com/photo-1546410531-bb4caa6b424d?ixlib=rb-1.2.1&auto=format&fit=crop&w=100&q=60'
    }
  ]
}

// 打卡统计
const checkInStats = ref(FAKE_DATA.checkIn)

// 学习统计
const currentStatTab = ref('本周')
const currentStats = computed(() => {
  return FAKE_DATA.stats[currentStatTab.value] || { duration: '0m', completed: 0 }
})

// 热力图
const heatmapData = computed(() => {
  return FAKE_DATA.heatmap.levels.map((level, index) => ({
    level,
    count: level * 5,
    date: `1月${index + 1}日`,
    isToday: index === 11
  }))
})

// 最近学习
const recentItems = ref(FAKE_DATA.recent)
</script>

<style scoped lang="scss">
@import '@/components/dashboard/styles/common.scss';

.dashboard {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  max-width: 100%;
  overflow-x: hidden;
}

.top-section-grid {
  display: grid;
  grid-template-columns: 1.2fr 1fr 0.8fr;
  gap: 1.5rem;
  
  @media (max-width: 1024px) {
    grid-template-columns: 1fr 1fr;
    
    :deep(.heatmap-card) {
      grid-column: span 2;
    }
  }
  
  @media (max-width: 768px) {
    grid-template-columns: 1fr;
    gap: 1rem;
    
    :deep(.heatmap-card) {
      grid-column: span 1;
    }
  }
}

.bottom-section-grid {
  display: grid;
  grid-template-columns: 2.2fr 1fr;
  gap: 1.5rem;
  
  @media (max-width: 1024px) {
    grid-template-columns: 1fr;
  }
  
  @media (max-width: 768px) {
    gap: 1rem;
  }
}
</style>
