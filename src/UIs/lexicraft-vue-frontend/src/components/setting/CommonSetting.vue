<script lang="ts" setup>
import {SoundFileOptions} from '@/config/env.ts'
import {getAudioFileUrl, usePlayAudio} from '@/hooks/sound.ts'
import Switch from '@/components/base/Switch.vue'
import {Option, Select} from '@/components/base/select'
import Textarea from '@/components/base/Textarea.vue'
import VolumeIcon from '@/components/icon/VolumeIcon.vue'
import Slider from '@/components/base/Slider.vue'
import SettingItem from '@/components/setting/SettingItem.vue'
import {useSettingStore} from '@/stores/setting.ts'
import {useBaseStore} from '@/stores/base.ts'
import {ShortcutKey} from '@/types/enum.ts'
import useTheme, {type ThemeMode, type ThemeStyle} from '@/hooks/theme'

const settingStore = useSettingStore()
const store = useBaseStore()
const {setTheme, setThemeStyle} = useTheme()

const themeStyles: Array<{value: ThemeStyle; label: string; caption: string; mark: string}> = [
  {value: 'editorial', label: '暖调书卷', caption: '像一本值得翻阅的书', mark: 'Aa'},
  {value: 'zen', label: '极简专注', caption: '安静、克制的写作空间', mark: '_'},
  {value: 'ink', label: '趣味手绘', caption: '像翻开一本涂鸦笔记', mark: '✎'}
]

const colorModes: Array<{value: ThemeMode; label: string}> = [
  {value: 'light', label: '明亮'},
  {value: 'dark', label: '暗色'},
  {value: 'auto', label: '跟随系统'}
]

const simpleWords = $computed({
  get: () => store.simpleWords.join(','),
  set: v => {
    try {
      store.simpleWords = v.split(',')
    } catch (e) {
    }
  },
})
</script>

<template>
  <div>
    <section class="appearance-setting" aria-labelledby="appearance-title">
      <div class="appearance-heading">
        <div>
          <span class="appearance-kicker">VISUAL READING ROOM</span>
          <h3 id="appearance-title">学习空间的气质</h3>
        </div>
        <p>选择内部学习空间的视觉风格。入口首页和登录页始终保持暖调书卷气。</p>
      </div>

      <div class="theme-style-list">
        <button
            v-for="item in themeStyles"
            :key="item.value"
            :aria-pressed="settingStore.themeStyle === item.value"
            :class="['theme-style-card', `theme-style-card--${item.value}`, {active: settingStore.themeStyle === item.value}]"
            type="button"
            @click="setThemeStyle(item.value)"
        >
          <span class="style-mark">{{ item.mark }}</span>
          <strong>{{ item.label }}</strong>
          <small>{{ item.caption }}</small>
        </button>
      </div>

      <div class="color-mode-row">
        <span>明暗模式</span>
        <div class="segmented-control">
          <button
              v-for="mode in colorModes"
              :key="mode.value"
              :aria-pressed="settingStore.theme === mode.value"
              :class="{active: settingStore.theme === mode.value}"
              type="button"
              @click="setTheme(mode.value)"
          >
            {{ mode.label }}
          </button>
        </div>
      </div>
    </section>

    <div class="line"></div>
    <SettingItem
        desc="开启后，输入时不区分大小写，如输入“hello”和“Hello”都会被认为是正确的"
        title="忽略大小写"
    >
      <Switch v-model="settingStore.ignoreCase"/>
    </SettingItem>

    <SettingItem
        :desc="`开启后，可以通过将鼠标移动到单词上或者按快捷键 ${settingStore.shortcutKeyMap[ShortcutKey.ShowWord]} 显示正确答案`"
        title="允许默写模式下显示提示"
    >
      <Switch v-model="settingStore.allowWordTip"/>
    </SettingItem>

    <div class="line"></div>
    <SettingItem
        desc="开启后，练习的单词中不会包含简单词；文章统计的总词数中不会包含简单词"
        title="简单词过滤"
    >
      <Switch v-model="settingStore.ignoreSimpleWord"/>
    </SettingItem>

    <SettingItem v-if="settingStore.ignoreSimpleWord" class="items-start!" title="简单词列表">
      <Textarea
          v-model="simpleWords"
          :autosize="{ minRows: 6, maxRows: 10 }"
          placeholder="多个单词用英文逗号隔号"
      />
    </SettingItem>

    <!--          音效-->
    <!--          音效-->
    <!--          音效-->
    <div class="line"></div>
    <SettingItem main-title="音效"/>
    <SettingItem desc="仅单词生效，文章固定美音" title="单词/句子发音口音">
      <Select v-model="settingStore.soundType" class="w-50!" placeholder="请选择">
        <Option label="美音" value="us"/>
        <Option label="英音" value="uk"/>
      </Select>
    </SettingItem>

    <div class="line"></div>
    <SettingItem title="按键音">
      <Switch v-model="settingStore.keyboardSound"/>
    </SettingItem>
    <SettingItem title="按键音效">
      <Select v-model="settingStore.keyboardSoundFile" class="w-50!" placeholder="请选择">
        <Option
            v-for="item in SoundFileOptions"
            :key="item.value"
            :label="item.label"
            :value="item.value"
        >
          <div class="flex justify-between items-center w-full">
            <span>{{ item.label }}</span>
            <VolumeIcon :time="100" @click="usePlayAudio(getAudioFileUrl(item.value)[0])"/>
          </div>
        </Option>
      </Select>
    </SettingItem>
    <SettingItem title="音量">
      <Slider v-model="settingStore.keyboardSoundVolume" showText showValue unit="%"/>
    </SettingItem>
  </div>
</template>

<style lang="scss" scoped>
.appearance-setting {
  margin-bottom: 1.5rem;
  padding: clamp(1rem, 2.5vw, 1.6rem);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-card);
  color: var(--text-primary);
  background: var(--surface-card);
  box-shadow: var(--card-shadow);
}

.appearance-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1.5rem;
  margin-bottom: 1.25rem;

  h3 { margin: .2rem 0 0; font-family: var(--font-heading); font-size: 1.35rem; }
  p { max-width: 33rem; margin: 0; color: var(--text-secondary); font-size: .83rem; line-height: 1.6; }
}

.appearance-kicker {
  color: var(--accent);
  font-family: var(--font-mono);
  font-size: .68rem;
  font-weight: 800;
  letter-spacing: .14em;
}

.theme-style-list {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: .75rem;
}

.theme-style-card {
  display: grid;
  min-height: 8.5rem;
  align-content: end;
  justify-items: start;
  padding: 1rem;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-control);
  color: var(--text-primary);
  background: var(--surface-raised);
  cursor: pointer;
  text-align: left;
  transition: border-color .2s ease, box-shadow .2s ease, transform .2s ease;

  &:hover { transform: translateY(-2px); }
  &.active { border-color: var(--accent); box-shadow: 0 0 0 3px var(--focus-ring); }
  strong { margin-top: .75rem; font-size: .95rem; }
  small { margin-top: .25rem; color: var(--text-secondary); }
}

.style-mark { font-size: 1.8rem; line-height: 1; }
.theme-style-card--editorial { font-family: var(--font-editorial); background: #f4ead8; color: #2b241f; }
.theme-style-card--zen { border-radius: 0; font-family: var(--font-mono); background: #f8f8f6; color: #111; }
.theme-style-card--ink { border: 2px solid #242b27; border-radius: 15px 10px 17px 12px; font-family: var(--font-hand); background: #f4efde; color: #242b27; transform: rotate(.5deg); }
.theme-style-card--ink:hover { transform: translateY(-2px) rotate(-.5deg); }

.color-mode-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-top: 1rem;
  color: var(--text-secondary);
  font-size: .82rem;
}

.segmented-control {
  display: inline-flex;
  padding: 3px;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-control);
  background: var(--surface-muted);

  button {
    padding: .48rem .75rem;
    border: 0;
    border-radius: max(0px, calc(var(--radius-control) - 2px));
    color: var(--text-secondary);
    background: transparent;
    cursor: pointer;
    font: inherit;
  }

  button.active { color: var(--accent-contrast); background: var(--accent); }
}

@media (max-width: 760px) {
  .appearance-heading { align-items: flex-start; flex-direction: column; gap: .5rem; }
  .theme-style-list { grid-template-columns: 1fr; }
  .theme-style-card { min-height: 6.5rem; }
  .color-mode-row { align-items: stretch; flex-direction: column; }
  .segmented-control { display: grid; grid-template-columns: repeat(3, 1fr); }
}
</style>
