<script setup lang="ts">
import {nextTick, onMounted, ref, watch} from 'vue';

const props = defineProps<{
  modelValue: number;
  min?: number;
  max?: number;
  step?: number;
  disabled?: boolean;
  showText?: boolean;
  showValue?: boolean;  // 是否显示当前值
  unit?: string
}>();

const emit = defineEmits(['update:modelValue']);

const min = props.min ?? 0;
const max = props.max ?? 100;
const step = props.step ?? 1;

const sliderRef = ref<HTMLElement | null>(null);
const isDragging = ref(false);
const sliderLeft = ref(0);
const sliderWidth = ref(0);

const currentValue = ref(props.modelValue);

watch(() => props.modelValue, (val) => {
  currentValue.value = val;
});

const valueToPercent = (value: number) => ((value - min) / (max - min)) * 100;

// 计算一个数字的小数位数
function countDecimals(value: number) {
  if (Math.floor(value) === value) return 0;
  const str = value.toString();
  if (str.indexOf('e-') >= 0) {
    // 科学计数法处理
    const [, trail] = str.split('e-');
    return parseInt(trail, 10);
  }
  return str.split('.')[1]?.length || 0;
}

// 对数值按步长对齐，并控制精度，避免浮点误差
function alignToStep(value: number, step: number) {
  const decimals = countDecimals(step);
  return Number((Math.round(value / step) * step).toFixed(decimals));
}

const percentToValue = (percent: number) => {
  let val = min + ((max - min) * percent) / 100;
  val = alignToStep(val, step);

  if (val < min) val = min;
  if (val > max) val = max;

  return val;
};


const updateSliderRect = () => {
  if (!sliderRef.value) return;
  const rect = sliderRef.value.getBoundingClientRect();
  sliderLeft.value = rect.left;
  sliderWidth.value = rect.width;
};

const setValueFromPosition = (pageX: number) => {
  let percent = ((pageX - sliderLeft.value) / sliderWidth.value) * 100;
  if (percent < 0) percent = 0;
  if (percent > 100) percent = 100;
  currentValue.value = percentToValue(percent);
  emit('update:modelValue', currentValue.value);
};

const onMouseDown = (e: MouseEvent) => {
  if (props.disabled) return;
  e.preventDefault();
  updateSliderRect();
  isDragging.value = true;
  setValueFromPosition(e.pageX);
  window.addEventListener('mousemove', onMouseMove);
  window.addEventListener('mouseup', onMouseUp);
};

const onTouchStart = (e: TouchEvent) => {
  if (props.disabled) return;
  updateSliderRect();
  isDragging.value = true;
  setValueFromPosition(e.touches[0].pageX);
  window.addEventListener('touchmove', onTouchMove);
  window.addEventListener('touchend', onTouchEnd);
};

const onMouseMove = (e: MouseEvent) => {
  if (!isDragging.value) return;
  e.preventDefault();
  setValueFromPosition(e.pageX);
};

const onTouchMove = (e: TouchEvent) => {
  if (!isDragging.value) return;
  setValueFromPosition(e.touches[0].pageX);
};

const onMouseUp = () => {
  if (!isDragging.value) return;
  isDragging.value = false;
  window.removeEventListener('mousemove', onMouseMove);
  window.removeEventListener('mouseup', onMouseUp);
};

const onTouchEnd = () => {
  if (!isDragging.value) return;
  isDragging.value = false;
  window.removeEventListener('touchmove', onTouchMove);
  window.removeEventListener('touchend', onTouchEnd);
};

const onClickTrack = (e: MouseEvent) => {
  if (props.disabled) return;
  updateSliderRect();
  setValueFromPosition(e.pageX);
};

onMounted(() => {
  nextTick(() => {
    updateSliderRect();
    window.addEventListener('resize', updateSliderRect);
  });
});

</script>

<template>
  <div class="w-full flex">
    <div class="flex-1">
      <div
        ref="sliderRef"
        class="custom-slider"
        :class="{ 'is-disabled': disabled }"
        @mousedown="onClickTrack"
        @touchstart.prevent="onClickTrack"
      >
        <div class="custom-slider__track"></div>
        <div
          class="custom-slider__fill"
          :style="{ width: valueToPercent(currentValue) + '%' }"
        ></div>
        <div
          class="custom-slider__thumb"
          :style="{ left: valueToPercent(currentValue) + '%' }"
          @mousedown.stop.prevent="onMouseDown"
          @touchstart.stop.prevent="onTouchStart"
          tabindex="0"
          role="slider"
          :aria-valuemin="min"
          :aria-valuemax="max"
          :aria-valuenow="currentValue"
          :aria-disabled="disabled"
        ></div>
      </div>
      <div class="text flex justify-between text-sm color-gray" v-if="showText">
        <span>{{ min }}</span>
        <span>{{ max }}</span>
      </div>
    </div>
    <div v-if="showValue" class="w-10 pl-5 ">{{ currentValue }}{{ unit}}</div>
  </div>
</template>

<style scoped lang="scss">
.custom-slider {
  position: relative;
  width: 100%;
  height: 24px;
  user-select: none;
  touch-action: none;
  cursor: pointer;

  &.is-disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  &__track {
    position: absolute;
    top: 50%;
    left: 0;
    right: 0;
    height: 6px;
    background-color: #ddd;
    border-radius: 2px;
    transform: translateY(-50%);
  }

  &__fill {
    position: absolute;
    top: 50%;
    left: 0;
    height: 6px;
    background-color: #409eff;
    border-radius: 2px 0 0 2px;
    transform: translateY(-50%);
    pointer-events: none;
  }

  &__thumb {
    position: absolute;
    top: 50%;
    width: 16px;
    height: 16px;
    background-color: #fff;
    border: 2px solid #409eff;
    border-radius: 50%;
    transform: translate(-50%, -50%);
    cursor: grab;
    transition: box-shadow 0.2s;
  }

  &__thumb:focus {
    outline: none;
    box-shadow: 0 0 5px #409eff;
    cursor: grabbing;
  }
}
</style>
