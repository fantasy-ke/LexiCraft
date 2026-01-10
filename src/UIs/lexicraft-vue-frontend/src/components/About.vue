<script setup lang="ts">

import { GITHUB } from "@/config/env.ts";
import ChannelIcons from "@/components/ChannelIcons/ChannelIcons.vue";
import WeChat from "@/components/ChannelIcons/WeChat.vue";
</script>

<template>
  <h1>Type Words</h1>
  <div class="w-120">
    <p class="text-xl">
      感谢使用本项目！本项目是开源项目，如果觉得有帮助，请在 GitHub 点个 Star，您的支持是我持续改进的动力！
    </p>
    <p>
      GitHub地址：<a :href="GITHUB" target="_blank">{{ GITHUB }}</a>
    </p>
    <div class="flex flex-col gap-2 mt-20">
      <div class="flex items-center">
        微信反馈：<WeChat/>
      </div>
      <div class="">
        工单反馈：<a :href="`https://v.wjx.cn/vm/ev0W7fv.aspx#`"
                    target="_blank">https://v.wjx.cn/vm/ev0W7fv.aspx#</a>
      </div>
      <div class="flex items-center">
        其他渠道：
        <ChannelIcons type="horizontal"
                      :share="false"
                      :wechat="false"
                      :github="false"
        />
      </div>
    </div>

  </div>
</template>

<style scoped lang="scss">

</style>