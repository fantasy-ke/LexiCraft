<script setup lang="ts">
import type { Dict } from "@/types/types";
import Checkbox from "@/components/base/checkbox/Checkbox.vue";

interface IProps {
  item?: Partial<Dict>;
  quantifier?: string
  isAdd: boolean
  showCheckbox?: boolean
  checked?: boolean
  showProgress?: boolean
  isUser?: boolean
}

const props = withDefaults(defineProps<IProps>(), {
  showProgress: true,
  isUser: false
})

defineEmits<{
  check: []
}>()
</script>

<template>
  <div :id="item?.id" v-if="!isAdd" class="book-card">
    <div class="book-cover">
      <!-- Cover Image or Placeholder -->
      <img v-if="item?.cover" :src="item.cover" :alt="item.name" class="img-cover"/>
      <div v-else class="cover-placeholder">
        <span>{{ item?.name?.charAt(0) || 'D' }}</span>
      </div>

      <!-- Status Tags -->
      <div class="status-tag learning" v-if="item?.lastLearnIndex > 0 && item?.lastLearnIndex < item?.length">学习中</div>
      <div class="status-tag complete" v-else-if="item?.lastLearnIndex >= item?.length">已完成</div>
      
      <!-- Custom Tag -->
      <div class="status-tag custom" v-if="item.custom">自定义</div>

      <!-- Checkbox for batch operations -->
      <div class="checkbox-wrapper" v-if="showCheckbox" @click.stop>
        <Checkbox :model-value="checked" @change="$emit('check')"/>
      </div>
    </div>

    <div class="book-info">
      <h4 class="book-title" :title="item?.name">{{ item?.name }}</h4>
      <div class="book-meta">
        <span class="progress" v-if="showProgress">
          {{ item?.lastLearnIndex }}/{{ item?.length }} {{ quantifier }}
        </span>
        <span class="progress" v-else>
          {{ item?.length }} {{ quantifier }}
        </span>
      </div>
    </div>
  </div>

  <!-- Add Button Style -->
  <div v-else class="book-card add-card" id="no-book">
    <div class="add-content">
      <IconFluentAdd16Regular class="add-icon"/>
      <span class="add-text">添加词典</span>
    </div>
  </div>
</template>

<style scoped lang="scss">
.book-card {
  width: 160px; /* Smaller than dashboard's 280px */
  background: var(--header-bg);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  flex-direction: column;

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
    border-color: var(--text-tertiary);
  }
}

.book-cover {
  height: 90px; /* Scaled down height */
  position: relative;
  background: #e2e8f0;

  .img-cover {
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
    font-size: 2rem;
    font-weight: 700;
  }

  .status-tag {
    position: absolute;
    top: 0.25rem;
    color: white;
    font-size: 0.6rem;
    padding: 0.1rem 0.4rem;
    border-radius: 100px;
    z-index: 2;

    &.learning {
      right: 0.25rem;
      background: rgba(59, 130, 246, 0.9);
    }
    &.complete {
      right: 0.25rem;
      background: rgba(16, 185, 129, 0.9);
    }
    &.custom {
      left: 0.25rem;
      background: rgba(168, 85, 247, 0.9);
    }
  }
  
  .checkbox-wrapper {
    position: absolute;
    bottom: 0.25rem;
    left: 0.25rem;
    z-index: 3;
    background: rgba(255,255,255,0.8);
    border-radius: 4px;
    padding: 2px;
  }
}

.book-info {
  padding: 0.6rem;
  
  .book-title {
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-primary);
    margin: 0 0 0.3rem 0;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .book-meta {
    display: flex;
    justify-content: space-between;
    font-size: 0.7rem;
    color: #94a3b8;
  }
}

/* Add Card Style */
.add-card {
  border: 1px dashed var(--border-color);
  background: transparent;
  
  &:hover {
    background: var(--hover-bg);
  }
  
  .add-content {
    height: 100%;
    min-height: 130px; 
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    color: var(--text-tertiary);
    
    .add-icon {
      font-size: 1.5rem;
      margin-bottom: 0.4rem;
    }
    
    .add-text {
      font-size: 0.8rem;
    }
  }
}

/* Mobile Adaptation */
@media (max-width: 768px) {
  .book-card {
    width: 31%; /* 3 items per row on mobile typically, or adjust to fit flex gap */
    flex-grow: 1;
    min-width: 100px;
  }
}

@media (max-width: 480px) {
  .book-card {
    width: 47%; /* 2 items per row */
  }
}
</style>
