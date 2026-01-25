<template>
  <div class="card recent-card">
    <div class="card-header">
      <h3 class="title">最近学习</h3>
    </div>

    <div class="recent-list">
      <div v-for="item in items" :key="item.id" class="recent-item">
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
</template>

<script lang="ts" setup>
defineProps<{
  items: Array<{
    id: number
    title: string
    subtitle: string
    time: string
    cover: string
  }>
}>()
</script>

<style lang="scss" scoped>
@use './styles/recent-card.scss';
</style>
