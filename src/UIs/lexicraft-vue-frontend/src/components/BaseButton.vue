<script lang="ts" setup>
import Tooltip from '@/components/base/Tooltip.vue'

interface IProps {
  keyboard?: string
  active?: boolean
  disabled?: boolean
  loading?: boolean
  size?: 'small' | 'normal' | 'large'
  type?: 'primary' | 'info' | 'orange' | 'text'
}

withDefaults(defineProps<IProps>(), {
  type: 'primary',
  size: 'normal',
})

defineEmits(['click'])
</script>

<template>
  <Tooltip :disabled="!keyboard" :title="`${keyboard}`">
    <div
        :class="[active && 'active', size, type, (disabled || loading) && 'disabled']"
        class="base-button"
        v-bind="$attrs"
        @click="e => !disabled && !loading && $emit('click', e)"
    >
      <span :style="{ opacity: loading ? 0 : 1 }"><slot></slot></span>
      <IconEosIconsLoading v-if="loading" :color="type === 'info' || type === 'text' ? 'var(--color-main-text)' : '#ffffff'" class="loading"
                           width="18"/>
    </div>
  </Tooltip>
</template>

<style>
:root {
  --btn-primary: rgb(75, 85, 99);
  --btn-primary-disabled: #90969e;
  --btn-primary-hover: rgb(105, 121, 143);
  --btn-info: white;
  --btn-info-hover: #eaeaea;
  --btn-orange: #facc15;
  --btn-orange-hover: #bfac61;
}

html.dark {
  --btn-info: #1b1b1b;
  --btn-info-hover: #3a3a3a;
}
</style>

<style lang="scss" scoped>
.base-button {
  cursor: pointer;
  box-sizing: border-box;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  outline: none;
  text-align: center;
  transition: all 0.3s;
  user-select: none;
  vertical-align: middle;
  white-space: nowrap;
  border-radius: 0.3rem;
  padding: 0 0.9rem;
  font-size: 0.9rem;
  height: 2rem;
  color: white;

  & + .base-button {
    margin-left: 1rem;
  }

  &.disabled {
    opacity: 0.6;
    cursor: not-allowed;
    user-select: none;
    color: rgba(#fff, 0.4);
  }

  .loading {
    position: absolute;
  }

  &.small {
    border-radius: 0.3rem;
    padding: 0 0.6rem;
    height: 1.6rem;
    font-size: 0.8rem;
  }

  &.large {
    padding: 0 1.3rem;
    height: 2.4rem;
    font-size: 0.9rem;
    border-radius: 0.5rem;
  }

  & > span {
    line-height: 1;
    transform: translateY(-5%);

    :deep(a) {
      color: white;
    }
  }

  &.primary {
    background: var(--btn-primary);

    &.disabled {
      opacity: 1;
      background: var(--btn-primary-disabled);
    }

    &:hover:not(.disabled) {
      background: var(--btn-primary-hover);
    }
  }

  &.info {
    background: var(--btn-info);
    border: 1px solid var(--color-main-text);
    color: var(--color-main-text);

    &:hover:not(.disabled) {
      background: var(--btn-info-hover);
    }
  }

  &.orange {
    background: var(--btn-orange);
    color: black;

    &:hover:not(.disabled) {
      background: var(--btn-orange-hover);
      color: rgba(0, 0, 0, 0.6);
    }
  }

  &.text {
    background: transparent;
    color: var(--color-main-text);
    padding: 0 0.5rem;

    &:hover:not(.disabled) {
      opacity: 0.8;
      text-decoration: underline;
    }

    & > span :deep(a) {
      color: var(--color-main-text);
    }
  }

  &.active {
    opacity: 0.4;
  }
}
</style>
