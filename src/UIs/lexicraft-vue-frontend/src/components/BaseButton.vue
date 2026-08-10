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



<style lang="scss" scoped>
.base-button {
  position: relative;
  display: inline-flex;
  height: 2.25rem;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  padding: 0 .95rem;
  border: 1px solid transparent;
  border-radius: var(--radius-control);
  outline: none;
  color: var(--accent-contrast);
  cursor: pointer;
  font-family: var(--font-family);
  font-size: .88rem;
  text-align: center;
  transition: color .2s ease, background .2s ease, border-color .2s ease, box-shadow .2s ease, transform .2s ease;
  user-select: none;
  white-space: nowrap;

  & + .base-button { margin-left: 1rem; }
  &.small { height: 1.8rem; padding-inline: .65rem; font-size: .76rem; }
  &.large { height: 2.75rem; padding-inline: 1.35rem; font-size: .92rem; }
  &.disabled { opacity: .55; cursor: not-allowed; }
  &:focus-visible { outline: 3px solid var(--focus-ring); outline-offset: 2px; }
  .loading { position: absolute; }
  & > span { line-height: 1; }
  & > span :deep(a) { color: inherit; }

  &.primary { background: var(--btn-primary); box-shadow: var(--control-shadow); }
  &.primary:hover:not(.disabled) { background: var(--btn-primary-hover); transform: translateY(-1px); }
  &.primary.disabled { background: var(--btn-primary-disabled); }
  &.info { border-color: var(--border-color); color: var(--text-primary); background: var(--btn-info); }
  &.info:hover:not(.disabled) { border-color: var(--accent); background: var(--btn-info-hover); }
  &.orange { color: #241d12; background: var(--btn-orange); }
  &.orange:hover:not(.disabled) { background: var(--btn-orange-hover); }
  &.text { padding-inline: .5rem; color: var(--text-primary); background: transparent; }
  &.text:hover:not(.disabled) { color: var(--accent); text-decoration: underline; text-underline-offset: 3px; }
  &.active { box-shadow: inset 0 0 0 2px var(--accent-contrast); }
}

:global(html[data-theme-style='editorial'] .base-button) { font-family: var(--font-sans); letter-spacing: .02em; }
:global(html[data-theme-style='zen'] .base-button) { border-radius: 0; box-shadow: none; font-family: var(--font-mono); text-transform: lowercase; }
:global(html[data-theme-style='zen'] .base-button:hover:not(.disabled)) { transform: none; }
:global(html[data-theme-style='ink'] .base-button) { border-width: 2px; border-color: var(--border-strong); box-shadow: var(--control-shadow); font-weight: 800; transform: rotate(-.3deg); }
:global(html[data-theme-style='ink'] .base-button:hover:not(.disabled)) { transform: translateY(-2px) rotate(.5deg); }
</style>
