<script setup lang="ts">

import { defineAsyncComponent, onMounted, watch } from "vue";
import { useSettingStore } from "@/stores/setting.ts";
import { jump2Feedback } from "@/utils";
import { useDisableEventListener } from "@/hooks/event.ts";
import ConflictNoticeText from "@/components/ConflictNoticeText.vue";

const Dialog = defineAsyncComponent(() => import('@/components/dialog/Dialog.vue'))

let settingStore = useSettingStore()
let show = $ref(false)

watch(() => settingStore.load, (n) => {
  if (n && settingStore.conflictNotice) {
    setTimeout(() => {
      show = true
    }, 300)
  }
}, { immediate: true })

useDisableEventListener(() => show)

</script>

<template>
  <Dialog
      v-model="show"
      title="重要提示"
      footer
      padding
      :closeOnClickBg="false"
      cancel-button-text="不再提醒"
      confirm-button-text="关闭"
      @cancel="settingStore.conflictNotice = false"
  >
    <div class="w-150 center flex-col color-main">
      <ConflictNoticeText/>
    </div>
  </Dialog>
</template>
