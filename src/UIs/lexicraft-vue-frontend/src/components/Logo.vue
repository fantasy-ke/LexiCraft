<script setup lang="ts">
import { useSettingStore } from '@/stores/setting.ts'
import { useRouter } from 'vue-router'
import { IS_DEV } from '@/config/env'

const settingStore = useSettingStore()
const router = useRouter()

function goHome() {
  if (IS_DEV) {
    router.push('/')
  } else {
    // LexionCraft
    location.href = window.atob('aHR0cHM6Ly9sZXhpb25jcmFmdC5jYw==')
  }
}
</script>

<template>
  <div class="center-logo-box" @click="goHome">
    <div class="logo-icon">
      <span class="text-white font-bold">LC</span>
    </div>
    <span class="logo-text">LexionCraft</span>
  </div>
</template>

<style scoped lang="scss">
.center-logo-box {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  cursor: pointer;
  padding: 0.5rem;
  transition: opacity 0.2s;

  &:hover {
    opacity: 0.8;
  }

  .logo-icon {
    width: 32px;
    height: 32px;
    background: linear-gradient(135deg, #3b82f6, #1d4ed8);
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 2px 8px rgba(59, 130, 246, 0.2);
  }

  .logo-text {
    font-size: 1.2rem;
    font-weight: 700;
    color: var(--text-primary);
    letter-spacing: -0.01em;
  }
}
</style>
