<template>
  <div class="dashboard">
    <!-- 第一行：打卡、统计、热力图 -->
    <div class="top-section-grid">
      <!-- 每日打卡 -->
      <div class="card check-in-card">
        <div class="card-header">
          <h3 class="title">每日打卡</h3>
          <button class="icon-btn">
            <IconFluentSettings20Regular />
          </button>
        </div>
        
        <div class="check-in-content">
          <div class="stats-row">
            <div class="stat-box">
              <div class="label">连签</div>
              <div class="value purple">{{ checkInStats.consecutiveDays }} <span class="unit">天</span></div>
            </div>
            <div class="stat-box right">
              <div class="label">累计打卡</div>
              <div class="value">{{ checkInStats.totalDays }} <span class="unit">天</span></div>
            </div>
          </div>
          
          <div class="week-circles">
            <div class="week-day" v-for="(day, index) in weekDays" :key="index" :class="{ 'active': day.checked, 'today': day.isToday }">
              <div class="day-label">{{ day.label }}</div>
              <div class="day-circle">
                <IconFluentCheckmark12Regular v-if="day.checked" />
              </div>
            </div>
          </div>
          
          <div class="action-buttons">
            <button class="calendar-btn">
              <IconFluentCalendarDate20Regular />
              打卡日历
            </button>
            <button class="check-in-btn">
              <IconFluentHandWave20Regular />
              长按签到
            </button>
          </div>
        </div>
      </div>

      <!-- 学习统计 -->
      <div class="card stats-card">
        <div class="card-header">
          <h3 class="title">学习统计</h3>
          <div class="filter-tabs">
            <span 
              class="tab" 
              v-for="tab in ['总计', '本周', '本月', '今年']" 
              :key="tab"
              :class="{ active: currentStatTab === tab }"
              @click="currentStatTab = tab"
            >
              {{ tab }}
            </span>
          </div>
        </div>
        
        <div class="stats-content">
          <div class="stat-block">
            <div class="label">学习时长</div>
            <div class="value">{{ currentStats.duration }}</div>
          </div>
          <div class="divider"></div>
          <div class="stat-block">
            <div class="label">完成课程</div>
            <div class="value">{{ currentStats.completed }} <span class="unit">节</span></div>
          </div>
        </div>
      </div>

      <!-- 学习热力图 -->
      <div class="card heatmap-card">
        <div class="card-header">
          <h3 class="title">学习热力图</h3>
          <div class="date-selector">
            <span class="date-text">2026年1月</span>
            <IconFluentChevronRight20Regular />
          </div>
        </div>
        
        <div class="heatmap-grid">
          <div class="heatmap-header">
            <span>一</span><span>二</span><span>三</span><span>四</span><span>五</span><span>六</span><span>日</span>
          </div>
          <div class="heatmap-body">
            <div 
              class="heatmap-cell" 
              v-for="(day, i) in heatmapData" 
              :key="i" 
              :class="{ 
                'active': day.isToday, 
                'level-1': day.level === 1, 
                'level-2': day.level === 2,
                'level-3': day.level === 3 
              }"
              :title="day.date + ' 学习' + day.count + '次'"
            ></div>
          </div>
        </div>
      </div>
    </div>

    <!-- 第二行：课程和最近学习 -->
    <div class="bottom-section-grid">
      <!-- 我的课程 -->
      <div class="card courses-card">
        <div class="card-header">
          <h3 class="title">我的课程</h3>
          <button class="shop-btn" @click="router.push('/app/dict-list')">
            <IconFluentBuildingShop20Regular />
            课程商城
          </button>
        </div>
        
        <div class="courses-list">
          <!-- 课程卡片 -->
          <div 
            class="course-item" 
            v-for="course in myCourses" 
            :key="course.id"
            @click="navigateToCourse(course)"
          >
            <div class="course-cover">
              <!-- 使用默认封面或实际封面 -->
              <div class="cover-placeholder" v-if="!course.cover">
                <span>{{ course.name.charAt(0) }}</span>
              </div>
              <img v-else :src="course.cover" :alt="course.name">
              <div class="status-tag learning" v-if="!course.complete && course.lastLearnIndex > 0">学习中</div>
              <div class="status-tag complete" v-else-if="course.complete">已完成</div>
            </div>
            <div class="course-info">
              <h4 class="course-title" :title="course.name">{{ course.name }}</h4>
              <div class="course-meta">
                <span class="progress">{{ course.lastLearnIndex }}/{{ course.length }} 词</span>
                <!-- <span class="time">{{ course.time }}</span> -->
              </div>
            </div>
          </div>
          
          <!-- 添加课程占位 -->
          <div class="add-course-item" @click="router.push('/app/dict-list')">
            <div class="add-icon">+</div>
            <div class="add-text">添加课程包</div>
          </div>
        </div>
      </div>

      <!-- 最近学习 -->
      <div class="card recent-card">
        <div class="card-header">
          <h3 class="title">最近学习</h3>
        </div>
        
        <div class="recent-list">
          <div class="recent-item" v-for="item in recentItems" :key="item.id">
            <div class="item-cover">
              <img :src="item.cover" alt="cover">
            </div>
            <div class="item-info">
              <h4 class="item-title">{{ item.title }}</h4>
              <p class="item-subtitle">{{ item.subtitle }}</p>
              <div class="item-meta">{{ item.time }}</div>
            </div>
          </div>
          
          <button class="view-more-btn">查看更多</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useBaseStore } from '@/stores/base'
import { Dict } from '@/types/types'

const router = useRouter()
const baseStore = useBaseStore()

// --- 1. 我的课程数据 (从Store获取) ---
const myCourses = computed(() => {
  // 过滤掉前3个系统默认词典 (收藏、错词、已掌握)，只显示用户添加的课程
  // 也可以根据需求决定是否显示错词本等，但通常"课程"指用户添加的词书
  return baseStore.word.bookList.slice(3) || []
})

const navigateToCourse = (course: Dict) => {
  // 这里的路由跳转需要根据实际情况调整，通常是跳转到练习页
  // 现有的路由结构中，单词练习通常在 /practice/words/:id 或 /app/words (选中)
  // 既然要跳到"指定"页面，我们应该先选中该词典，然后跳转
  baseStore.changeDict(course)
  router.push('/app/study-word') 
  // 或者如果支持直接ID跳转: router.push(`/practice/words/${course.id}`)
}


// --- 2. 模拟数据配置 (JSON Config) ---
const FAKE_DATA = {
  checkIn: {
    consecutiveDays: 5,
    totalDays: 48,
    weekHistory: [false, true, true, true, true, true, false] // 周一到周日
  },
  stats: {
    '总计': { duration: '128h', completed: 42 },
    '本周': { duration: '5h 20m', completed: 2 },
    '本月': { duration: '24h', completed: 8 },
    '今年': { duration: '128h', completed: 42 }
  },
  heatmap: {
    // 模拟28天的数据，0=无，1=少，2=中，3=多
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

// --- 3. 响应式数据绑定 ---

// 打卡统计
const checkInStats = ref(FAKE_DATA.checkIn)
const weekDays = computed(() => {
  const labels = ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
  // 假设今天是周六(index 5)，简单模拟
  const todayIndex = 5 
  return labels.map((label, index) => ({
    label,
    checked: FAKE_DATA.checkIn.weekHistory[index],
    isToday: index === todayIndex
  }))
})

// 学习统计
const currentStatTab = ref('本周')
const currentStats = computed(() => {
  return FAKE_DATA.stats[currentStatTab.value] || { duration: '0m', completed: 0 }
})

// 热力图
const heatmapData = computed(() => {
  return FAKE_DATA.heatmap.levels.map((level, index) => ({
    level,
    count: level * 5, // 模拟次数
    date: `1月${index + 1}日`,
    isToday: index === 11 // 随便定一天是今天
  }))
})

// 最近学习
const recentItems = ref(FAKE_DATA.recent)

</script>

<style scoped lang="scss">
.dashboard {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.card {
  background: var(--header-bg);
  border-radius: 12px;
  padding: 1.25rem;
  box-shadow: 0 1px 3px var(--shadow-color);
  border: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.25rem;
  
  .title {
    font-size: 1.1rem;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0;
  }
}

/* Check-in Card */
.check-in-card {
  .stats-row {
    display: flex;
    justify-content: space-between;
    margin-bottom: 1.5rem;
    
    .stat-box {
      .label {
        font-size: 0.8rem;
        color: var(--text-tertiary);
        margin-bottom: 0.25rem;
      }
      .value {
        font-size: 1.75rem;
        font-weight: 700;
        color: var(--text-primary);
        line-height: 1;
        
        &.purple {
          color: #8b5cf6;
        }
        
        .unit {
          font-size: 0.9rem;
          color: var(--text-tertiary);
          font-weight: 400;
        }
      }
      
      &.right {
        text-align: right;
      }
    }
  }
  
  .week-circles {
    display: flex;
    justify-content: space-between;
    margin-bottom: 1.5rem;
    
    .week-day {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.5rem;
      
      .day-label {
        font-size: 0.75rem;
        color: var(--text-tertiary);
      }
      
      .day-circle {
        width: 32px;
        height: 32px;
        border-radius: 8px;
        border: 1px solid var(--border-color);
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--text-tertiary);
      }
      
      &.today .day-circle {
        border-color: #8b5cf6;
        color: #8b5cf6;
      }
      
      &.active .day-circle {
        background: #8b5cf6;
        border-color: #8b5cf6;
        color: white;
      }
    }
  }
  
  .action-buttons {
    display: flex;
    gap: 1rem;
    
    button {
      flex: 1;
      height: 40px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      font-size: 0.9rem;
      cursor: pointer;
      font-weight: 500;
      transition: all 0.2s;
    }
    
    .calendar-btn {
      background: var(--header-bg);
      border: 1px solid var(--border-color);
      color: var(--text-tertiary);
      
      &:hover {
        background: var(--hover-bg);
        border-color: var(--text-tertiary);
      }
    }
    
    .check-in-btn {
      background: #8b5cf6;
      border: none;
      color: white;
      
      &:hover {
        background: #7c3aed;
      }
    }
  }
}

/* Stats Card */
.stats-card {
  .filter-tabs {
    background: var(--hover-bg);
    padding: 2px;
    border-radius: 6px;
    display: flex;
    
    .tab {
      padding: 2px 8px;
      font-size: 0.75rem;
      border-radius: 4px;
      color: var(--text-tertiary);
      cursor: pointer;
      transition: all 0.2s;
      
      &.active {
        background: var(--header-bg);
        color: var(--text-active);
        box-shadow: 0 1px 2px var(--shadow-color);
      }
    }
  }
  
  .stats-content {
    display: flex;
    align-items: center;
    flex: 1;
    
    .stat-block {
      flex: 1;
      
      .label {
        font-size: 0.875rem;
        color: var(--text-tertiary);
        margin-bottom: 0.5rem;
      }
      
      .value {
        font-size: 1.5rem;
        font-weight: 700;
        color: var(--text-primary);
        
        .unit {
          font-size: 0.9rem;
          font-weight: 400;
          color: var(--text-tertiary);
        }
      }
    }
    
    .divider {
      width: 1px;
      height: 40px;
      background: var(--border-color);
      margin: 0 1rem;
    }
  }
}

/* Heatmap Card */
.heatmap-card {
  .heatmap-grid {
    .heatmap-header {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      text-align: center;
      margin-bottom: 0.5rem;
      
      span {
        font-size: 0.75rem;
        color: #94a3b8;
      }
    }
    
    .heatmap-body {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 6px;
      
        .heatmap-cell {
        aspect-ratio: 1;
        background: var(--hover-bg);
        border-radius: 4px;
        transition: all 0.2s;
        
        &.level-1 { background: #fee2e2; }
        &.level-2 { background: #fca5a5; }
        &.level-3 { background: #ef4444; }
        
        &.active { 
          border: 1px solid #ef4444; 
          transform: scale(1.1);
        }
        
        &:hover {
          transform: scale(1.1);
        }
      }
    }
  }
}

/* Courses Card */
.courses-card {
  
  .shop-btn {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    background: var(--hover-bg);
    border: none;
    padding: 0.25rem 0.75rem;
    border-radius: 100px;
    font-size: 0.8rem;
    color: var(--text-tertiary);
    cursor: pointer;
    
    &:hover {
      background: var(--active-bg);
    }
  }
  
  .courses-list {
    display: flex;
    gap: 1rem;
    overflow-x: auto;
    padding-bottom: 0.5rem;
    
    /* Scrollbar styling */
    &::-webkit-scrollbar {
      height: 6px;
    }
    &::-webkit-scrollbar-track {
      background: transparent;
    }
    &::-webkit-scrollbar-thumb {
      background-color: #cbd5e1;
      border-radius: 20px;
    }
    
    .course-item {
      flex: 0 0 280px;
      background: var(--header-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      overflow: hidden;
      cursor: pointer;
      transition: all 0.2s;
      
      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0,0,0,0.05);
      }
      
      .course-cover {
        height: 140px;
        position: relative;
        background: #e2e8f0;
        
        img {
          width: 100%;
          height: 100%;
          object-fit: cover;
        }
        
        .cover-placeholder {
          width: 100%;
          height: 100%;
          display: flex;
          align-items: center;
          justify-content: center;
          background: linear-gradient(135deg, #e0e7ff 0%, #c7d2fe 100%);
          color: #4f46e5;
          font-size: 3rem;
          font-weight: 700;
        }
        
        .status-tag {
          position: absolute;
          top: 0.5rem;
          right: 0.5rem;
          color: white;
          font-size: 0.75rem;
          padding: 0.15rem 0.5rem;
          border-radius: 100px;
          
          &.learning {
            background: rgba(59, 130, 246, 0.9);
          }
          
          &.complete {
            background: rgba(16, 185, 129, 0.9);
          }
        }
      }
      
      .course-info {
        padding: 0.75rem;
        
        .course-title {
          font-size: 0.95rem;
          font-weight: 600;
          color: var(--text-primary);
          margin-bottom: 0.5rem;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }
        
        .course-meta {
          display: flex;
          justify-content: space-between;
          font-size: 0.75rem;
          color: #94a3b8;
        }
      }
    }
    
      .add-course-item {
      flex: 0 0 200px;
      border: 1px dashed var(--border-color);
      border-radius: 12px;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      color: var(--text-tertiary);
      transition: all 0.2s;
      min-height: 200px;
      
      &:hover {
        border-color: var(--text-tertiary);
        background: var(--hover-bg);
      }
      
      .add-icon {
        font-size: 2rem;
        margin-bottom: 0.5rem;
      }
      
      .add-text {
        font-size: 0.875rem;
      }
    }
  }
}

/* Recent Card */
.recent-card {
  
  .recent-list {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    
    .recent-item {
      display: flex;
      gap: 0.75rem;
      padding-bottom: 1rem;
      border-bottom: 1px solid #f1f5f9;
      
      &:last-child {
        border-bottom: none;
      }
      
      .item-cover {
        width: 60px;
        height: 60px;
        border-radius: 8px;
        overflow: hidden;
        flex-shrink: 0;
        
        img {
          width: 100%;
          height: 100%;
          object-fit: cover;
        }
      }
      
      .item-info {
        flex: 1;
        overflow: hidden;
        
        .item-title {
          font-size: 0.9rem;
          font-weight: 600;
          color: var(--text-primary);
          margin-bottom: 0.25rem;
          line-height: 1.3;
        }
        
        .item-subtitle {
          font-size: 0.8rem;
          color: var(--text-tertiary);
          margin-bottom: 0.25rem;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }
        
        .item-meta {
          font-size: 0.75rem;
          color: var(--text-tertiary);
        }
      }
    }
    
    .view-more-btn {
      width: 100%;
      padding: 0.75rem;
      background: var(--hover-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      font-size: 0.875rem;
      color: var(--text-tertiary);
      cursor: pointer;
      margin-top: auto;
      
      &:hover {
        background: var(--active-bg);
      }
    }
  }
}

/* Grid Layouts */
.top-section-grid {
  display: grid;
  grid-template-columns: 1.2fr 1fr 0.8fr;
  gap: 1.5rem;
  
  @media (max-width: 1024px) {
    grid-template-columns: 1fr 1fr;
    
    .heatmap-card {
      grid-column: span 2;
    }
  }
  
  @media (max-width: 768px) {
    grid-template-columns: 1fr;
    
    .heatmap-card {
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
}

.icon-btn {
  background: none;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  padding: 4px;
  
  &:hover {
    color: #64748b;
  }
}

/* Mobile Optimizations */
@media (max-width: 768px) {
  .dashboard {
    gap: 1rem;
  }

  .top-section-grid,
  .bottom-section-grid {
    gap: 1rem;
  }

  .card {
    padding: 1rem;
  }

  /* Check-in Card tweaks */
  .check-in-card {
    .stats-row .stat-box .value {
      font-size: 1.5rem;
    }
    
    .week-circles .week-day {
      .day-circle {
        width: 28px;
        height: 28px;
        font-size: 0.75rem;
      }
      .day-label {
        font-size: 0.7rem;
      }
    }
  }

  /* Stats Card tweaks */
  .stats-card {
    .stats-content {
      .stat-block .value {
        font-size: 1.25rem;
      }
    }
  }
}

</style>