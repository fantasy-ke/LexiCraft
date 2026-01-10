<script setup lang="ts">
import BaseButton from "./BaseButton.vue";

interface Resource {
  name: string;
  description?: string;
  difficulty?: string;
  author?: string;
  features?: string;
  suitable?: string;
  link: string;
}

const props = defineProps<{
  resource: Resource;
}>();

const emit = defineEmits(['openLink']);

// 根据难度获取对应的样式类
const getDifficultyClass = (difficulty: string) => {
  switch (difficulty) {
    case '入门':
      return 'bg-green-500';
    case '基础':
      return 'bg-blue-500';
    case '中级':
      return 'bg-purple-500';
    case '进阶':
      return 'bg-amber-500';
    case '高级':
      return 'bg-red-500';
    case '全级别':
      return 'bg-gray-500';
    default:
      return 'bg-blue-500';
  }
};
</script>

<template>
  <div class="card-white min-h-45 mb-0 flex flex-col justify-between">
    <div>
      <div class="text-xl font-semibold mb-3 text-gray-800 dark:text-gray-100">
        {{ resource.name }}
      </div>
      <div class="space-y-2 mb-4">
        <div v-if="resource.author" class="text-sm text-gray-600 dark:text-gray-300">
          <span class="font-medium">作者：</span>{{ resource.author }}
        </div>
        <div v-if="resource.features" class="text-sm text-gray-600 dark:text-gray-300">
          <span class="font-medium">🌟 特点：</span>{{ resource.features }}
        </div>
        <div v-if="resource.suitable" class="text-sm text-gray-600 dark:text-gray-300">
          <span class="font-medium">📌 适合：</span>{{ resource.suitable }}
        </div>
        <div v-if="resource.description" class="text-sm text-gray-600 dark:text-gray-300">
          {{ resource.description }}
        </div>
        <span
          v-if="resource.difficulty"
          class="inline-block px-3 py-1 rounded-full text-xs font-medium text-white"
          :class="getDifficultyClass(resource.difficulty)"
        >
          {{ resource.difficulty }}
        </span>
      </div>
    </div>
    <div class="flex flex-col gap-3">
      <BaseButton type="primary" @click="emit('openLink', resource.link)">
        打开链接
      </BaseButton>
    </div>
  </div>
</template>
