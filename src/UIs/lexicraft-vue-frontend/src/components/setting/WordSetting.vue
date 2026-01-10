<script setup lang="ts">

import Switch from "@/components/base/Switch.vue";
import RadioGroup from "@/components/base/radio/RadioGroup.vue";
import InputNumber from "@/components/base/InputNumber.vue";
import Slider from "@/components/base/Slider.vue";
import SettingItem from "@/components/setting/SettingItem.vue";
import Radio from "@/components/base/radio/Radio.vue";
import { useSettingStore } from "@/stores/setting.ts";
const settingStore = useSettingStore()

</script>

<template>
  <div>
    <SettingItem title="显示上一个/下一个单词"
                 desc="开启后，练习中会在上方显示上一个/下一个单词"
    >
      <Switch v-model="settingStore.showNearWord"/>
    </SettingItem>

    <SettingItem title="不默认显示练习设置弹框"
                 desc="在词典详情页面，点击学习按钮后，是否显示练习设置弹框"
    >
      <Switch v-model="settingStore.disableShowPracticeSettingDialog"/>
    </SettingItem>

    <SettingItem title="输入错误时，清空已输入内容"
    >
      <Switch v-model="settingStore.inputWrongClear"/>
    </SettingItem>


    <SettingItem title="单词循环设置" class="gap-0!">
      <RadioGroup v-model="settingStore.repeatCount">
        <Radio :value="1" size="default">1</Radio>
        <Radio :value="2" size="default">2</Radio>
        <Radio :value="3" size="default">3</Radio>
        <Radio :value="5" size="default">5</Radio>
        <Radio :value="100" size="default">自定义</Radio>
      </RadioGroup>
      <div class="ml-2 center gap-space" v-if="settingStore.repeatCount === 100">
        <span>循环次数</span>
        <InputNumber v-model="settingStore.repeatCustomCount"
                     :min="6"
                     :max="15"
                     type="number"
        />
      </div>
    </SettingItem>

    <SettingItem title="复习比"
                 desc="复习词与新词的比例，修改后下次学习生效"
    >
      <InputNumber :min="0" :max="10" v-model="settingStore.wordReviewRatio"/>
    </SettingItem>

    <!--          发音-->
    <!--          发音-->
    <!--          发音-->
    <div class="line"></div>
    <SettingItem mainTitle="音效"/>
    <SettingItem title="单词自动发音">
      <Switch v-model="settingStore.wordSound"/>
    </SettingItem>
    <SettingItem title="音量">
      <Slider v-model="settingStore.wordSoundVolume" showText showValue unit="%"/>
    </SettingItem>
    <SettingItem title="倍速">
      <Slider v-model="settingStore.wordSoundSpeed" :step="0.1" :min="0.5" :max="3" showText showValue/>
    </SettingItem>
    <div class="line"></div>
    <SettingItem title="效果音（输入错误、完成时的音效）">
      <Switch v-model="settingStore.effectSound"/>
    </SettingItem>
    <SettingItem title="音量">
      <Slider v-model="settingStore.effectSoundVolume" showText showValue unit="%"/>
    </SettingItem>

    <!--          自动切换-->
    <!--          自动切换-->
    <!--          自动切换-->
    <div class="line"></div>
    <SettingItem mainTitle="自动切换"/>
    <SettingItem title="自动切换下一个单词"
                 desc="仅在 **跟写** 时生效，听写、自测、默写均不会自动切换，需要手动按 **空格键** 切换"
    >
      <Switch v-model="settingStore.autoNextWord"/>
    </SettingItem>

    <SettingItem title="自动切换下一个单词时间"
                 desc="正确输入单词后，自动跳转下一个单词的时间"
    >
      <InputNumber v-model="settingStore.waitTimeForChangeWord"
                   :disabled="!settingStore.autoNextWord"
                   :min="0"
                   :max="10000"
                   :step="100"
                   type="number"
      />
      <span class="ml-4">毫秒</span>
    </SettingItem>


    <!--          字体设置-->
    <!--          字体设置-->
    <!--          字体设置-->
    <div class="line"></div>
    <SettingItem mainTitle="字体设置"/>
    <SettingItem title="外语字体">
      <Slider
          :min="10"
          :max="100"
          v-model="settingStore.fontSize.wordForeignFontSize" showText showValue unit="px"/>
    </SettingItem>
    <SettingItem title="中文字体">
      <Slider
          :min="10"
          :max="100"
          v-model="settingStore.fontSize.wordTranslateFontSize" showText showValue unit="px"/>
    </SettingItem>
  </div>
</template>

<style scoped lang="scss">

</style>
