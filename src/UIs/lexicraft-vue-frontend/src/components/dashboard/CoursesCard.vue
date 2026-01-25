<template>
  <div class="card courses-card">
    <div class="card-header">
      <h3 class="title">我的课程</h3>
      <button class="shop-btn" @click="$emit('goToShop')">
        <IconFluentBuildingShop20Regular/>
        课程商城
      </button>
    </div>

    <div class="courses-list">
      <!-- 课程卡片 -->
      <div
          v-for="course in courses"
          :key="course.id"
          class="course-item"
          @click="$emit('selectCourse', course)"
      >
        <div class="course-cover">
          <div v-if="!course.cover" class="cover-placeholder">
            <span>{{ course.name.charAt(0) }}</span>
          </div>
          <img v-else :alt="course.name" :src="course.cover">
          <div v-if="!course.complete && course.lastLearnIndex > 0" class="status-tag learning">学习中</div>
          <div v-else-if="course.complete" class="status-tag complete">已完成</div>
        </div>
        <div class="course-info">
          <h4 :title="course.name" class="course-title">{{ course.name }}</h4>
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

<script lang="ts" setup>
import {Dict} from '@/types/types'

defineProps<{
  courses: Dict[]
}>()

defineEmits<{
  (e: 'selectCourse', course: Dict): void
  (e: 'goToShop'): void
}>()
</script>

<style lang="scss" scoped>
@use './styles/courses-card.scss';
</style>
