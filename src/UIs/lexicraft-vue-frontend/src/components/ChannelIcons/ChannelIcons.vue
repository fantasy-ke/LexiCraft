<script setup lang="ts">
import { GITHUB } from "@/config/env";
import BaseIcon from "@/components/BaseIcon.vue";
import { defineAsyncComponent } from "vue";
import ShareIcon from "@/components/ChannelIcons/ShareIcon.vue";
import WeChat from "@/components/ChannelIcons/WeChat.vue";
import Github from "@/components/ChannelIcons/Github.vue";

withDefaults(defineProps<{
  type?: 'vertical' | 'horizontal',
  share?: boolean,
  wechat?: boolean,
  github?: boolean,
}>(), {
  type: "vertical",
  share: true,
  github: true,
  wechat: true
})
const Dialog = defineAsyncComponent(() => import('@/components/dialog/Dialog.vue'))

let showXhsDialog = $ref(false)
let showQQDialog = $ref(false)

</script>

<template>
  <div class="center" :class="type === 'vertical' ? 'flex-col gap-1' : 'gap-4'">

    <Github v-if="github"/>

    <WeChat v-if="wechat"/>

    <BaseIcon title="QQ群" @click="showQQDialog = true">
      <IconUiwQq class="color-red"/>
    </BaseIcon>
    <BaseIcon title="小红书" @click="showXhsDialog = true">
      <IconSimpleIconsXiaohongshu class="color-red-500"/>
    </BaseIcon>

    <a href="https://x.com/typewords2" target="_blank" rel="noreferrer" aria-label="关注我的 X 账户 typewords2">
      <BaseIcon title="推特">
        <IconRiTwitterFill class="color-blue"/>
      </BaseIcon>
    </a>

    <a href="mailto:zyronon@163.com" target="_blank" rel="noreferrer" aria-label="发送邮件到 zyronon@163.com">
      <BaseIcon title="邮箱">
        <IconMaterialSymbolsMail class="color-blue"/>
      </BaseIcon>
    </a>

    <ShareIcon v-if="share"/>
  </div>

  <Dialog v-model="showXhsDialog" title="小红书">
    <div class="w-120 p-6 pt-0">
      <div class="mb-4">
        关注小红书后，您可以获得开发团队的最新动态和更新内容，反馈您的使用体验和建议，帮助我们改进产品，同时也能够及时了解我们的最新动态和更新内容。
      </div>
      <div class="text-center">
        <img src="/xhs.png" alt="小红书二维码" class="w-70 rounded-xl shadow-lg">
      </div>
    </div>
  </Dialog>

  <Dialog v-model="showQQDialog" title="QQ 交流群">
    <div class="w-120 p-6 pt-0">
      <div class="mb-4">
        <span>加入我们的用户社群后，您可以与我们的开发团队进行沟通，分享您的使用体验和建议，帮助我们改进产品，同时也能够及时了解我们的最新动态和更新内容。</span>
      </div>
      <div class="text-center">
        <img src="/qq.jpg" alt="QQ群二维码" class="w-70 rounded-xl shadow-lg">
      </div>
    </div>
  </Dialog>

</template>
<style scoped lang="scss">
</style>
