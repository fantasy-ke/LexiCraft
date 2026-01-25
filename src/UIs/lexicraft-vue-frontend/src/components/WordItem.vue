<script lang="ts" setup>
import type {Word} from '@/types/types.ts'
import VolumeIcon from '@/components/icon/VolumeIcon.vue'
import {usePlayWordAudio} from '@/hooks/sound.ts'
import Tooltip from '@/components/base/Tooltip.vue'
import BaseIcon from '@/components/BaseIcon.vue'
import {useWordOptions} from '@/hooks/dict.ts'

withDefaults(
    defineProps<{
      item: Word
      showTranslate?: boolean
      showWord?: boolean
      showTransPop?: boolean
      showOption?: boolean
      showCollectIcon?: boolean
      showMarkIcon?: boolean
      index?: number
      active?: boolean
    }>(),
    {
      showTranslate: true,
      showWord: true,
      showTransPop: true,
      showOption: true,
      showCollectIcon: true,
      showMarkIcon: true,
      active: false,
    }
)

const playWordAudio = usePlayWordAudio()

const {isWordCollect, toggleWordCollect, isWordSimple, toggleWordSimple} = useWordOptions()
</script>

<template>
  <div :class="{ active }" class="common-list-item">
    <div class="left">
      <slot :item="item" name="prefix"></slot>
      <div class="title-wrapper">
        <div class="item-title">
          <span v-if="index != undefined" class="text-sm translate-y-0.5 text-gray-500">{{ index }}.</span>
          <span :class="!showWord && 'word-shadow'" class="word">{{ item.word }}</span>
          <span :class="!showWord && 'word-shadow'" class="phonetic text-gray">{{ item.phonetic0 }}</span>
          <VolumeIcon class="volume" @click="playWordAudio(item.word)"></VolumeIcon>
        </div>
        <div v-if="item.trans.length && showTranslate" class="item-sub-title flex flex-col gap-2">
          <div v-for="v in item.trans">
            <Tooltip v-if="v.cn.length > 30 && showTransPop" :title="v.pos + '  ' + v.cn">
              <span>{{ v.pos + '  ' + v.cn.slice(0, 30) + '...' }}</span>
            </Tooltip>
            <span v-else>{{ v.pos + '  ' + v.cn }}</span>
          </div>
        </div>
      </div>
    </div>
    <div v-if="showOption" class="right">
      <slot :item="item" name="suffix"></slot>
      <BaseIcon
          v-if="showCollectIcon"
          :class="!isWordCollect(item) ? 'collect' : 'fill'"
          :title="!isWordCollect(item) ? '收藏' : '取消收藏'"
          @click.stop="toggleWordCollect(item)"
      >
        <IconFluentStar16Regular v-if="!isWordCollect(item)"/>
        <IconFluentStar16Filled v-else/>
      </BaseIcon>

      <BaseIcon
          v-if="showMarkIcon"
          :class="!isWordSimple(item) ? 'collect' : 'fill'"
          :title="!isWordSimple(item) ? '标记为已掌握' : '取消标记已掌握'"
          @click.stop="toggleWordSimple(item)"
      >
        <IconFluentCheckmarkCircle16Regular v-if="!isWordSimple(item)"/>
        <IconFluentCheckmarkCircle16Filled v-else/>
      </BaseIcon>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
