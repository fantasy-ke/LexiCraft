<script setup lang="ts">
import { SoundFileOptions } from '@/config/env.ts'
import { getAudioFileUrl, usePlayAudio } from '@/hooks/sound.ts'
import Switch from '@/components/base/Switch.vue'
import { Option, Select } from '@/components/base/select'
import Textarea from '@/components/base/Textarea.vue'
import VolumeIcon from '@/components/icon/VolumeIcon.vue'
import Slider from '@/components/base/Slider.vue'
import SettingItem from '@/components/setting/SettingItem.vue'
import { useSettingStore } from '@/stores/setting.ts'
import { useBaseStore } from '@/stores/base.ts'
import {ShortcutKey} from "@/types/enum.ts";

const settingStore = useSettingStore()
const store = useBaseStore()

const simpleWords = $computed({
  get: () => store.simpleWords.join(','),
  set: v => {
    try {
      store.simpleWords = v.split(',')
    } catch (e) {}
  },
})
</script>

<template>
  <div>
    <SettingItem
      title="忽略大小写"
      desc="开启后，输入时不区分大小写，如输入“hello”和“Hello”都会被认为是正确的"
    >
      <Switch v-model="settingStore.ignoreCase" />
    </SettingItem>

    <SettingItem
      title="允许默写模式下显示提示"
      :desc="`开启后，可以通过将鼠标移动到单词上或者按快捷键 ${settingStore.shortcutKeyMap[ShortcutKey.ShowWord]} 显示正确答案`"
    >
      <Switch v-model="settingStore.allowWordTip" />
    </SettingItem>

    <div class="line"></div>
    <SettingItem
      title="简单词过滤"
      desc="开启后，练习的单词中不会包含简单词；文章统计的总词数中不会包含简单词"
    >
      <Switch v-model="settingStore.ignoreSimpleWord" />
    </SettingItem>

    <SettingItem title="简单词列表" class="items-start!" v-if="settingStore.ignoreSimpleWord">
      <Textarea
        placeholder="多个单词用英文逗号隔号"
        v-model="simpleWords"
        :autosize="{ minRows: 6, maxRows: 10 }"
      />
    </SettingItem>

    <!--          音效-->
    <!--          音效-->
    <!--          音效-->
    <div class="line"></div>
    <SettingItem main-title="音效" />
    <SettingItem title="单词/句子发音口音" desc="仅单词生效，文章固定美音">
      <Select v-model="settingStore.soundType" placeholder="请选择" class="w-50!">
        <Option label="美音" value="us" />
        <Option label="英音" value="uk" />
      </Select>
    </SettingItem>

    <div class="line"></div>
    <SettingItem title="按键音">
      <Switch v-model="settingStore.keyboardSound" />
    </SettingItem>
    <SettingItem title="按键音效">
      <Select v-model="settingStore.keyboardSoundFile" placeholder="请选择" class="w-50!">
        <Option
          v-for="item in SoundFileOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        >
          <div class="flex justify-between items-center w-full">
            <span>{{ item.label }}</span>
            <VolumeIcon :time="100" @click="usePlayAudio(getAudioFileUrl(item.value)[0])" />
          </div>
        </Option>
      </Select>
    </SettingItem>
    <SettingItem title="音量">
      <Slider v-model="settingStore.keyboardSoundVolume" showText showValue unit="%" />
    </SettingItem>
  </div>
</template>

<style scoped lang="scss"></style>
