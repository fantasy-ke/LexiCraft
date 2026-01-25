<script lang="ts" setup>
import {computed, ref, watch} from 'vue';

interface IProps {
  modelValue: boolean;
  disabled?: boolean;
  width?: number;       // 开关宽度，默认 40px
  activeText?: string;  // 开启状态显示文字
  inactiveText?: string;// 关闭状态显示文字
}

const props = withDefaults(defineProps<IProps>(), {
  activeText: '开',
  inactiveText: '关',
})

const emit = defineEmits(['update:modelValue', 'change']);

const isChecked = ref(props.modelValue);

watch(() => props.modelValue, (val) => {
  isChecked.value = val;
});

const toggle = () => {
  if (props.disabled) return;
  isChecked.value = !isChecked.value;
  emit('update:modelValue', isChecked.value);
  emit('change', isChecked.value);
};

const onKeydown = (e: KeyboardEvent) => {
  if (e.code === 'Space' || e.key === ' ') {
    e.preventDefault();
    toggle();
  }
};

const switchWidth = computed(() => props.width ?? 40);
const switchHeight = computed(() => (switchWidth.value / 2) | 0);
const ballSize = computed(() => switchHeight.value - 4);
</script>

<template>
  <div
      :aria-checked="isChecked"
      :class="{ 'checked': isChecked, 'disabled': disabled }"
      :style="{ width: switchWidth + 'px', height: switchHeight + 'px' ,borderRadius: switchHeight + 'px'}"
      :tabindex="disabled ? -1 : 0"
      class="switch"
      role="switch"
      @click="toggle"
      @keydown="onKeydown"
  >
    <transition name="fade">
      <span v-if="isChecked && activeText" class="text left">{{ activeText }}</span>
    </transition>
    <div
        :style="{
          width: ballSize + 'px',
          height: ballSize + 'px',
          transform: isChecked ? 'translateX(' + (switchWidth - ballSize - 2) + 'px)' : 'translateX(2px)'
        }"
        class="ball"
    ></div>
    <transition name="fade">
      <span v-if="!isChecked && inactiveText" class="text right">{{ inactiveText }}</span>
    </transition>
  </div>
</template>

<style lang="scss" scoped>
.switch {
  @apply inline-flex items-center cursor-pointer user-select-none outline-none bg-gray-200 position-relative transition-all duration-300;


  &.disabled {
    @apply cursor-not-allowed opacity-60;
  }

  &.checked {
    @apply bg-blue-500;
  }

  .ball {
    @apply bg-white rounded-full transition-transform duration-300 box-shadow-sm absolute;
  }

  .text {
    @apply absolute text-xs text-white user-select-none;
    font-size: 0.75rem;

    &.left {
      @apply ml-1.5;
    }

    &.right {
      @apply right-1.5;
    }
  }
}
</style>
