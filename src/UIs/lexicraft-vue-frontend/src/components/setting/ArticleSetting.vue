<script setup lang="ts">
import Switch from '@/components/base/Switch.vue'
import Slider from '@/components/base/Slider.vue'
import SettingItem from '@/components/setting/SettingItem.vue'
import { useSettingStore } from '@/stores/setting.ts'

const settingStore = useSettingStore()
</script>

<template>
  <div>
    <SettingItem mainTitle="音效" />
    <SettingItem title="自动播放句子">
      <Switch v-model="settingStore.articleSound" />
    </SettingItem>
    <SettingItem title="结束后播放下一篇">
      <Switch v-model="settingStore.articleAutoPlayNext" />
    </SettingItem>
    <SettingItem title="音量">
      <Slider v-model="settingStore.articleSoundVolume" showText showValue unit="%" />
    </SettingItem>
    <SettingItem title="倍速">
      <Slider v-model="settingStore.articleSoundSpeed" :step="0.1" :min="0.5" :max="3" showText showValue />
    </SettingItem>

    <div class="line"></div>
    <SettingItem title="输入时忽略符号/数字/人名">
      <Switch v-model="settingStore.ignoreSymbol" />
    </SettingItem>
  </div>
</template>

<style scoped lang="scss"></style>
