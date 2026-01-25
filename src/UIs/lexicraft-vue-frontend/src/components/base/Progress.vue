<script lang="ts" setup>
import {computed} from 'vue';

interface IProps {
  percentage: number;
  showText?: boolean;
  textInside?: boolean;
  strokeWidth?: number;
  color?: string;
  active?: boolean;
  format?: (percentage: number) => string;
  size?: 'normal' | 'large';
}

const props = withDefaults(defineProps<IProps>(), {
  showText: true,
  textInside: false,
  strokeWidth: 6,
  color: '#409eff',
  active: true,
  format: (percentage) => `${percentage}%`,
  size: 'normal',
});

const barStyle = computed(() => {
  return {
    width: `${props.percentage}%`,
    backgroundColor: props.color,
  };
});

const trackStyle = computed(() => {
  const height = props.size === 'large' ? props.strokeWidth * 2.5 : props.strokeWidth;
  return {
    height: `${height}px`,
    opacity: props.active ? 1 : 0.4,
  };
});

const progressTextSize = 18

const content = computed(() => {
  if (typeof props.format === 'function') {
    return props.format(props.percentage) || '';
  } else {
    return `${props.percentage}%`;
  }
});
</script>

<template>
  <div :aria-valuenow="percentage" aria-valuemax="100" aria-valuemin="0" class="progress" role="progressbar">
    <div :style="trackStyle" class="progress-bar">
      <div :style="barStyle" class="progress-bar-inner">
        <div v-if="showText && textInside" :style="{ fontSize: progressTextSize + 'px' }" class="progress-bar-text">
          {{ content }}
        </div>
      </div>
    </div>
    <div v-if="showText && !textInside" :style="{ fontSize: progressTextSize + 'px' }" class="progress-bar-text">
      {{ content }}
    </div>
  </div>
</template>

<style lang="scss" scoped>
.progress {
  position: relative;
  width: 100%;
  display: flex;
  align-items: center;

  .progress-bar {
    width: 100%;
    border-radius: 100px;
    background-color: var(--color-progress-bar);
    overflow: hidden;
    position: relative;
    vertical-align: middle;

    .progress-bar-inner {
      position: relative;
      height: 100%;
      border-radius: 100px;
      transition: width 0.6s ease;
      text-align: right;

      .progress-bar-text {
        display: inline-block;
        vertical-align: middle;
        color: #fff;
        font-size: 12px;
        margin: 0 5px;
        white-space: nowrap;
      }
    }
  }

  .progress-bar-text {
    margin-left: 5px;
    min-width: 50px;
    color: var(--el-text-color-regular);
    font-size: 14px;
    text-align: center;
    flex-shrink: 0;
  }
}
</style>
