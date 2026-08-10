<script lang="ts" setup>
import {useRouter} from 'vue-router'
import {IS_DEV} from '@/config/env'
import BrandLogo from '@/components/BrandLogo.vue'

const router = useRouter()

function goHome() {
  if (IS_DEV) {
    router.push('/')
  } else {
    location.href = window.atob('aHR0cHM6Ly9sZXhpb25jcmFmdC5jYw==')
  }
}
</script>

<template>
  <button class="logo-button" type="button" @click="goHome">
    <BrandLogo tagline="language journal"/>
  </button>
</template>

<style lang="scss" scoped>
.logo-button {
  display: inline-flex;
  padding: .25rem;
  border: 0;
  color: inherit;
  background: transparent;
  cursor: pointer;
  transition: opacity .2s ease, transform .2s ease;
}
.logo-button:hover { opacity: .82; transform: translateY(-1px); }
.logo-button:focus-visible { outline: 3px solid var(--focus-ring); outline-offset: 3px; }
</style>
