<script setup lang="ts">

import BaseIcon from "@/components/BaseIcon.vue";
let showWechatDialog = $ref(false)

</script>

<template>
  <BaseIcon title="微信群" @click="showWechatDialog = true">
    <IconSimpleIconsWechat class="color-green-500"/>
  </BaseIcon>
  <Dialog v-model="showWechatDialog" title="微信群">
    <div class="w-120 p-6 pt-0">
      <div class="mb-4">
        加入我们的用户社群后，您可以与我们的开发团队进行沟通，分享您的使用体验和建议，帮助我们改进产品，同时也能够及时了解我们的最新动态和更新内容。
      </div>
      <div class="text-center">
        <img src="/wechat.png" alt="微信群二维码" class="w-70 rounded-xl shadow-lg">
      </div>
    </div>
  </Dialog>
</template>

<style scoped lang="scss">

</style>