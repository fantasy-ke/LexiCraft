<script lang="ts" setup>
import {computed, ref, useAttrs, watch} from 'vue';
import Close from "@/components/icon/Close.vue";
import {useDisableEventListener} from "@/hooks/event.ts";

defineOptions({
  name: "BaseInput",
})

const props = defineProps({
  modelValue: [String, Number],
  placeholder: String,
  disabled: Boolean,
  autofocus: Boolean,
  error: Boolean,
  type: {
    type: String,
    default: 'text',
  },
  clearable: {
    type: Boolean,
    default: false,
  },
  required: {
    type: Boolean,
    default: false,
  },
  maxLength: Number,
  size: {
    type: String,
    default: 'normal',
    validator: (value: string) => ['normal', 'large'].includes(value)
  },
});

const emit = defineEmits(['update:modelValue', 'input', 'change', 'focus', 'blur', 'validation', 'enter']);
const attrs = useAttrs();

const inputValue = ref(props.modelValue);
let focus = $ref(false)
let inputEl = $ref<HTMLDivElement>()
const passwordVisible = ref(false)

const inputType = computed(() => {
  if (props.type === 'password') {
    return passwordVisible.value ? 'text' : 'password'
  }
  return props.type
})

const togglePasswordVisibility = () => {
  passwordVisible.value = !passwordVisible.value
}

watch(() => props.modelValue, (val) => {
  inputValue.value = val;
});

const onInput = (e: Event) => {
  const target = e.target as HTMLInputElement;
  inputValue.value = target.value;
  emit('update:modelValue', target.value);
  emit('input', e);
  emit('change', e);
};

const onChange = (e: Event) => {
  emit('change', e);
};

const onFocus = (e: FocusEvent) => {
  focus = true
  emit('focus', e);
};

const onBlur = (e: FocusEvent) => {
  focus = false
  emit('blur', e);
};

const onEnter = (e: KeyboardEvent) => {
  emit('enter', e);
};

const clearInput = () => {
  inputValue.value = '';
  emit('update:modelValue', '');
};

//当聚焦时，禁用输入监听
useDisableEventListener(() => focus)

const vFocus = {
  mounted: (el, bind) => {
    if (bind.value) {
      el.focus()
      setTimeout(() => focus = true)
    }
  }
}

</script>

<template>
  <div ref="inputEl"
       :class="{ 'is-disabled': disabled, 'error': props.error, focus, [`base-input--${size}`]: true }"
       class="base-input">
    <slot name="subfix"></slot>
    <!-- PreIcon slot -->
    <div v-if="$slots.preIcon" class="pre-icon">
      <slot name="preIcon"></slot>
    </div>
    <IconFluentLockClosed20Regular v-if="type === 'password'" class="pre-icon"/>
    <IconFluentMail20Regular v-if="type === 'email'" class="pre-icon"/>
    <IconFluentPhone20Regular v-if="type === 'tel'" class="pre-icon"/>
    <IconFluentNumberSymbol20Regular v-if="type === 'code'" class="pre-icon"/>

    <input
        v-focus="autofocus"
        :disabled="disabled"
        :maxlength="maxLength"
        :placeholder="placeholder"
        :type="inputType"
        :value="inputValue"
        class="inner"
        v-bind="attrs"
        @blur="onBlur"
        @change="onChange"
        @focus="onFocus"
        @input="onInput"
        @keydown.enter="onEnter"
    />
    <slot name="prefix"></slot>
    <Close
        v-if="clearable && inputValue && !disabled"
        @click="clearInput"/>
    <!-- Password visibility toggle -->
    <div
        v-if="type === 'password' && !disabled"
        :title="passwordVisible ? '隐藏密码' : '显示密码'"
        class="password-toggle"
        @click="togglePasswordVisibility">
      <IconFluentEye16Regular v-if="!passwordVisible"/>
      <IconFluentEyeOff16Regular v-else/>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.base-input {
  position: relative;
  display: inline-flex;
  width: 100%;
  align-items: center;
  box-sizing: border-box;
  padding: .25rem .4rem;
  overflow: hidden;
  border: 1px solid var(--color-input-border);
  border-radius: var(--radius-control);
  color: var(--color-input-color);
  background: var(--color-input-bg);
  transition: border-color .2s ease, box-shadow .2s ease, background .2s ease;

  ::placeholder { color: var(--text-tertiary); font-size: .88rem; opacity: .82; }
  &--normal .inner { height: 1.6rem; font-size: 1rem; }
  &--large { padding: .45rem .65rem; }
  &--large .inner { height: 2rem; font-size: 1.08rem; }
  &.is-disabled { opacity: .55; cursor: not-allowed; }
  &.error { border-color: var(--danger); background: color-mix(in srgb, var(--danger) 7%, var(--color-input-bg)); }
  &.focus { border-color: var(--accent); box-shadow: 0 0 0 3px var(--focus-ring); }
  &.has-preicon .inner { padding-left: 2rem; }

  .pre-icon { display: flex; align-items: center; justify-content: center; z-index: 1; margin-right: .25rem; color: var(--color-input-icon); pointer-events: none; }
  .inner { flex: 1; width: 100%; height: 1.6rem; border: 0; outline: 0; color: var(--color-input-color); background: transparent; font-family: inherit; }
  .inner:disabled { cursor: not-allowed; }
  .password-toggle { display: grid; width: 24px; height: 24px; margin-left: 4px; place-items: center; color: var(--color-input-icon); cursor: pointer; transition: color .2s ease; }
  .password-toggle:hover { color: var(--accent); }
}

:global(html[data-theme-style='editorial'] .base-input) { font-family: var(--font-sans); }
:global(html[data-theme-style='zen'] .base-input) { border-width: 0 0 1px; border-radius: 0; padding-inline: 0; background: transparent; }
:global(html[data-theme-style='zen'] .base-input.focus) { border-bottom-color: var(--text-primary); box-shadow: none; }
:global(html[data-theme-style='zen'] .inner) { font-family: var(--font-mono); }
:global(html[data-theme-style='ink'] .base-input) { border-width: 2px; transform: rotate(-.15deg); }
:global(html[data-theme-style='ink'] .base-input.focus) { box-shadow: 3px 4px 0 var(--focus-ring); transform: rotate(.15deg); }
</style>
