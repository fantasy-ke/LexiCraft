<template>
  <div class="card courses-card">
    <div class="card-header">
      <h3 class="title">我的课程</h3>
      <button class="shop-btn" @click="$emit('goToShop')">
        <IconFluentBuildingShop20Regular />
        课程商城
      </button>
    </div>
    
    <div class="courses-list">
      <!-- 课程卡片 -->
      <div 
        class="course-item" 
        v-for="course in courses" 
        :key="course.id"
        @click="$emit('selectCourse', course)"
      >
        <div class="course-cover">
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
          </div>
        </div>
      </div>
      
      <!-- 添加课程占位 -->
      <div class="add-course-item" @click="$emit('goToShop')">
        <div class="add-icon">+</div>
        <div class="add-text">添加课程包</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Dict } from '@/types/types'

defineProps<{
  courses: Dict[]
}>()

defineEmits<{
  (e: 'selectCourse', course: Dict): void
  (e: 'goToShop'): void
}>()
</script>

<style scoped lang="scss">
@import './styles/courses-card.scss';
</style>
