<script lang="ts" setup>
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
  <div class="resource-card card-white min-h-45 mb-0 flex flex-col justify-between">
    <div>
      <div class="resource-title text-xl font-semibold mb-3">
        {{ resource.name }}
      </div>
      <div class="space-y-2 mb-4">
        <div v-if="resource.author" class="resource-meta text-sm">
          <span class="font-medium">作者：</span>{{ resource.author }}
        </div>
        <div v-if="resource.features" class="resource-meta text-sm">
          <span class="font-medium">🌟 特点：</span>{{ resource.features }}
        </div>
        <div v-if="resource.suitable" class="resource-meta text-sm">
          <span class="font-medium">📌 适合：</span>{{ resource.suitable }}
        </div>
        <div v-if="resource.description" class="resource-meta text-sm">
          {{ resource.description }}
        </div>
        <span
            v-if="resource.difficulty"
            :class="getDifficultyClass(resource.difficulty)"
            class="inline-block px-3 py-1 rounded-full text-xs font-medium text-white"
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


<style lang="scss" scoped>
.resource-card { padding: 1.25rem; color: var(--text-primary); background: var(--surface-card); }
.resource-title { color: var(--text-primary); font-family: var(--font-heading); }
.resource-meta { color: var(--text-secondary); line-height: 1.6; }
:global(html[data-theme-style='zen'] .resource-card) { border-inline: 0; box-shadow: none; }
:global(html[data-theme-style='ink'] .resource-card) { border-width: 2px; transform: rotate(-.2deg); }
</style>
