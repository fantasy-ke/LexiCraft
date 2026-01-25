<script lang="ts" setup>
import {useSettingStore} from '@/stores/setting'

interface Hint {
  key: string
  label: string
}

defineProps<{
  hints: Hint[]
}>()

const settingStore = useSettingStore()

function getShortcutKey(keyName: string) {
  return settingStore.shortcutKeyMap[keyName] || keyName
}
</script>

<template>
  <div class="shortcut-hints-container">
    <div v-for="hint in hints" :key="hint.key" class="hint-item">
      <div class="key-wrapper">
        <template v-if="getShortcutKey(hint.key).includes('+')">
          <template v-for="(k, index) in getShortcutKey(hint.key).split('+')" :key="index">
            <kbd>{{ k.trim() }}</kbd>
            <span v-if="index < getShortcutKey(hint.key).split('+').length - 1" class="plus">+</span>
          </template>
        </template>
        <kbd v-else>{{ getShortcutKey(hint.key) }}</kbd>
      </div>
      <span class="label">{{ hint.label }}</span>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.shortcut-hints-container {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  padding: 0.8rem;
  background: rgba(var(--bg-second-rgb), 0.6);
  backdrop-filter: blur(10px);
  border: 1px solid var(--color-item-border);
  border-radius: 10px;
  width: fit-content;
  user-select: none;
  pointer-events: none;
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.05);

  .hint-item {
    display: flex;
    flex-direction: row; // 改为水平排列
    align-items: center;
    gap: 0.6rem;
    width: 100%;

    .key-wrapper {
      display: flex;
      align-items: center;
      justify-content: flex-start;
      min-width: 2.2rem; // 保证按键对齐
    }

    kbd {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 1.6rem; // 缩小尺寸
      height: 1.6rem; // 缩小尺寸
      padding: 0 0.4rem;
      font-family: inherit;
      font-size: 0.75rem; // 缩小字号
      font-weight: 600;
      color: var(--text-primary);
      background: var(--bg-primary);
      border: 1px solid var(--color-item-border);
      border-radius: 4px;
      box-shadow: 0 1.5px 0 var(--color-item-border);
    }

    .plus {
      margin: 0 0.15rem;
      font-size: 0.7rem;
      color: var(--text-tertiary);
    }

    .label {
      font-size: 0.75rem; // 缩小字号
      color: var(--text-secondary);
      white-space: nowrap;
      font-weight: 500;
    }
  }
}

@media (max-width: 768px) {
  .shortcut-hints-container {
    display: none;
  }
}
</style>
