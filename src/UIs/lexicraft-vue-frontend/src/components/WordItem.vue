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
  <div :class="{ active }" class="word-item-card common-list-item">
    <div class="left">
      <slot :item="item" name="prefix"></slot>
      <div class="title-wrapper">
        <div class="item-title">
          <span v-if="index != undefined" class="index-num">{{ index }}.</span>
          <span :class="!showWord && 'word-shadow'" class="word">{{ item.word }}</span>
          <span :class="!showWord && 'word-shadow'" class="phonetic">{{ item.phonetic0 }}</span>
          <VolumeIcon class="volume" @click="playWordAudio(item.word)"></VolumeIcon>
        </div>
        <div v-if="item.trans.length && showTranslate" class="item-sub-title">
          <div v-for="v in item.trans" class="trans-row">
            <span class="pos-tag">{{ v.pos }}</span>
            <Tooltip v-if="v.cn.length > 30 && showTransPop" :title="v.cn">
              <span class="trans-text">{{ v.cn.slice(0, 30) + '...' }}</span>
            </Tooltip>
            <span v-else class="trans-text">{{ v.cn }}</span>
          </div>
        </div>
      </div>
    </div>
    <div v-if="showOption" class="right">
      <slot :item="item" name="suffix"></slot>
      <div class="action-icons">
        <BaseIcon
            v-if="showCollectIcon"
            :class="['action-btn', !isWordCollect(item) ? 'collect' : 'fill']"
            :title="!isWordCollect(item) ? '收藏' : '取消收藏'"
            @click.stop="toggleWordCollect(item)"
        >
          <IconFluentStar16Regular v-if="!isWordCollect(item)"/>
          <IconFluentStar16Filled v-else class="text-amber-400"/>
        </BaseIcon>

        <BaseIcon
            v-if="showMarkIcon"
            :class="['action-btn', !isWordSimple(item) ? 'collect' : 'fill']"
            :title="!isWordSimple(item) ? '标记为已掌握' : '取消标记已掌握'"
            @click.stop="toggleWordSimple(item)"
        >
          <IconFluentCheckmarkCircle16Regular v-if="!isWordSimple(item)"/>
          <IconFluentCheckmarkCircle16Filled v-else class="text-emerald-500"/>
        </BaseIcon>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.word-item-card {
  position: relative;
  background: white;
  border: 1px solid transparent;
  border-radius: 12px;
  padding: 1rem 1.25rem;
  margin-bottom: 0.75rem;
  transition: all 0.2s ease;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.03);

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 8px 16px rgba(0, 0, 0, 0.06);
    background: white;
    z-index: 1;
  }

  &.active {
    border-color: var(--color-link);
    background: var(--active-bg);
  }
}

.dark .word-item-card {
  background: rgba(30, 41, 59, 0.6);
  border-color: rgba(255, 255, 255, 0.05);

  &:hover {
    background: rgba(30, 41, 59, 0.9);
    border-color: rgba(255, 255, 255, 0.1);
  }

  &.active {
    background: rgba(59, 130, 246, 0.15);
    border-color: rgba(59, 130, 246, 0.4);
  }
}

.left {
  flex: 1;
  display: flex;
  gap: 1rem;
  align-items: flex-start;
}

.title-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.item-title {
  display: flex;
  align-items: baseline;
  gap: 0.75rem;
  flex-wrap: wrap;

  .index-num {
    font-family: var(--word-font-family);
    color: var(--text-tertiary);
    font-size: 0.85rem;
    min-width: 1.5rem;
  }

  .word {
    font-family: var(--word-font-family);
    font-size: 1.25rem;
    font-weight: 700;
    color: var(--text-primary);
    line-height: 1.2;
  }

  .phonetic {
    font-family: var(--word-font-family);
    font-size: 0.9rem;
    color: var(--text-secondary);
    background: var(--hover-bg);
    padding: 0.1rem 0.4rem;
    border-radius: 4px;
  }

  .volume {
    opacity: 0.4;
    transition: opacity 0.2s;
    cursor: pointer;

    &:hover {
      opacity: 1;
      color: var(--color-link);
    }
  }
}

.item-sub-title {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  padding-left: 2.25rem; // Align with word start
}

.trans-row {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  font-size: 0.95rem;
  line-height: 1.4;
  color: var(--text-secondary);

  .pos-tag {
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--text-tertiary);
    font-style: italic;
    min-width: 1.2rem;
  }

  .trans-text {
    color: var(--text-secondary);
  }
}

.right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.5rem;
}

.action-icons {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  opacity: 0;
  transition: opacity 0.2s;
}

.word-item-card:hover .action-icons {
  opacity: 1;
}

.action-btn {
  padding: 0.4rem;
  border-radius: 6px;
  color: var(--text-tertiary);
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: var(--hover-bg);
    color: var(--text-primary);
  }

  &.fill {
    opacity: 1;
  }
}

/* Mobile adaptation */
@media (max-width: 768px) {
  .word-item-card {
    padding: 0.75rem;
  }

  .item-title {
    .word {
      font-size: 1.1rem;
    }
  }

  .item-sub-title {
    padding-left: 0;
  }

  .action-icons {
    opacity: 1; // Always show on mobile
  }
}
</style>
