<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue'
import Tooltip from '@/components/base/Tooltip.vue'
import { useEventListener } from '@/hooks/event'

import BaseButton from '@/components/BaseButton.vue'
import { useRuntimeStore } from '@/stores/runtime'

export interface ModalProps {
  modelValue?: boolean
  showClose?: boolean
  title?: string
  content?: string
  fullScreen?: boolean
  padding?: boolean
  footer?: boolean
  header?: boolean
  confirmButtonText?: string
  cancelButtonText?: string
  keyboard?: boolean
  closeOnClickBg?: boolean
  confirm?: any
  beforeClose?: any
}

const props = withDefaults(defineProps<ModalProps>(), {
  modelValue: undefined,
  showClose: true,
  closeOnClickBg: true,
  fullScreen: false,
  footer: false,
  header: true,
  confirmButtonText: '确认',
  cancelButtonText: '取消',
  keyboard: true,
})

const emit = defineEmits(['update:modelValue', 'close', 'ok', 'cancel'])

let confirmButtonLoading = $ref(false)
let zIndex = $ref(999)
let visible = $ref(false)
let openTime = $ref(Date.now())
let maskRef = $ref<HTMLDivElement>(null)
let modalRef = $ref<HTMLDivElement>(null)
const runtimeStore = useRuntimeStore()
let id = Date.now()

async function close() {
  if (!visible) {
    return
  }
  if (props.beforeClose) {
    if (!(await props.beforeClose())) {
      return
    }
  }
  //记录停留时间，避免时间太短，弹框闪烁
  let stayTime = Date.now() - openTime
  let closeTime = 300
  if (stayTime < 500) {
    closeTime += 500 - stayTime
  }
  return new Promise(resolve => {
    setTimeout(() => {
      maskRef?.classList.toggle('bounce-out')
      modalRef?.classList.toggle('bounce-out')
    }, 500 - stayTime)

    setTimeout(() => {
      emit('update:modelValue', false)
      emit('close')
      visible = false
      resolve(true)
      let rIndex = runtimeStore.modalList.findIndex(item => item.id === id)
      if (rIndex > -1) {
        runtimeStore.modalList.splice(rIndex, 1)
      }
    }, closeTime)
  })
}

watch(
  () => props.modelValue,
  n => {
    if (n) {
      id = Date.now()
      runtimeStore.modalList.push({ id, close })
      zIndex = 999 + runtimeStore.modalList.length
      visible = true
    } else {
      close()
    }
  }
)

onMounted(() => {
  if (props.modelValue === undefined) {
    visible = true
    id = Date.now()
    runtimeStore.modalList.push({ id, close })
    zIndex = 999 + runtimeStore.modalList.length
  }
})

onUnmounted(() => {
  if (props.modelValue === undefined) {
    visible = false
    let rIndex = runtimeStore.modalList.findIndex(item => item.id === id)
    if (rIndex > -1) {
      runtimeStore.modalList.splice(rIndex, 1)
    }
  }
})

useEventListener('keyup', async (e: KeyboardEvent) => {
  if (e.key === 'Escape' && props.keyboard) {
    let lastItem = runtimeStore.modalList[runtimeStore.modalList.length - 1]
    if (lastItem?.id === id) {
      await cancel()
    }
  }
})

async function ok() {
  if (props.confirm) {
    confirmButtonLoading = true
    await props.confirm()
    confirmButtonLoading = false
  }
  emit('ok')
  await close()
}

async function cancel() {
  emit('cancel')
  await close()
}
</script>

<template>
  <Teleport to="body">
    <div class="modal-root" :style="{ 'z-index': zIndex }" v-if="visible">
      <div
        class="modal-mask"
        ref="maskRef"
        v-if="!fullScreen"
        @click.stop="closeOnClickBg && close()"
      ></div>
      <div class="modal" ref="modalRef" :class="[fullScreen ? 'full' : 'window']">
        <Tooltip title="关闭">
          <IconFluentDismiss20Regular
            @click="close"
            v-if="showClose"
            class="close cursor-pointer"
            width="24"
          />
        </Tooltip>
        <div class="modal-header" v-if="header">
          <div class="title">{{ props.title }}</div>
        </div>
        <div class="modal-body" :class="{ padding }">
          <slot></slot>
          <div v-if="content" class="content max-h-60vh">{{ content }}</div>
        </div>
        <div class="modal-footer" v-if="footer">
          <div class="left flex items-end">
            <slot name="footer-left"></slot>
          </div>
          <div class="right">
            <BaseButton type="info" @click="cancel">{{ cancelButtonText }}</BaseButton>
            <BaseButton id="dialog-ok" :loading="confirmButtonLoading" @click="ok"
              >{{ confirmButtonText }}
            </BaseButton>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
$time: 0.3s;

@keyframes bounce-in {
  0% {
    opacity: 0;
  }
  100% {
    opacity: 1;
  }
}

@keyframes bounce-in-full {
  0% {
    transform: scale(1.5);
    opacity: 0;
  }
  100% {
    transform: scale(1);
    opacity: 1;
  }
}

@keyframes fade-in {
  0% {
    opacity: 0;
  }
  100% {
    opacity: 1;
  }
}

.modal-root {
  @apply fixed top-0 left-0 z-999 flex items-center justify-center w-full h-full overflow-hidden;

  .modal-mask {
    @apply fixed top-0 left-0 w-full h-full transition-all duration-300;
    background: rgba(#000, 0.6);
    animation: fade-in $time;

    &.bounce-out {
      @apply bg-transparent;
    }
  }

  .window {
    animation: bounce-in $time ease-out;
    @apply shadow-lg rounded-lg;

    &.bounce-out {
      opacity: 0;
    }
  }

  .full {
    @apply w-full h-full;
    animation: bounce-in-full $time ease-out;

    &.bounce-out {
      transform: scale(1.5);
      opacity: 0;
    }
  }

  .modal {
    @apply relative overflow-hidden flex flex-col transition-all duration-300;
    background: var(--color-card-bg);

    .close {
      @apply absolute right-1.2rem top-1.2rem z-999;
    }

    .modal-header {
      @apply flex justify-between items-center p-5 pb-0 rounded-t-lg;

      .title {
        @apply font-bold text-xl;
      }
    }

    .modal-body {
      @apply box-border text-main-text font-normal text-base leading-6 w-full flex-1 overflow-hidden flex;

      &.padding {
        @apply p-1 px-5;
      }

      .content {
        @apply w-64 p-2 px-4 pb-4;
      }
    }

    .modal-footer {
      @apply flex justify-between p-5;
    }
  }
}
</style>
