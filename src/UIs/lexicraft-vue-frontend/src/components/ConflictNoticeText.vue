<script setup lang="ts">

import { jump2Feedback } from "@/utils";
import WeChat from "@/components/ChannelIcons/WeChat.vue";
</script>

<template>
  <div class="text">
    如果您安装了 <span class="font-bold text-red">“调速” “Vim” “音视频增强”</span> 等插件/脚本，它们会拦截键盘按下事件，<span
    class="font-bold text-red">导致在本网站练习时按 'A'、 'S' 、'D' 等键无反应</span>，您可以根据以下步骤解决冲突：
  </div>
  <ul class="m-0">
    <li>用浏览器无痕模式打开本网站，确认能否正常输入？</li>
    <li>
      无痕模式下无法输入，<span class="color-link mx-1 cp" @click="jump2Feedback">点此</span>反馈，或者加微信群反馈：<WeChat/>
    </li>
    <li>无痕模式下可以输入，则是插件/脚本导致的冲突</li>
    <li>临时禁用对应插件/脚本，或在对应插件/脚本的设置里面排除本网站</li>
    <li>可安装此
      <a href="https://chromewebstore.google.com/detail/one-click-extensions-mana/pbgjpgbpljobkekbhnnmlikbbfhbhmem"
         target="_blank">插件(Chrome版本，需翻墙)</a>,
      <a href="https://microsoftedge.microsoft.com/addons/detail/%E5%BF%AB%E6%8D%B7%E6%89%A9%E5%B1%95%E7%AE%A1%E7%90%86/jdodenbllldnoogfmbmmgpieafbnaogm"
         target="_blank">插件(Edge版本，无需翻墙)</a>,
      来快速激活、禁用其他插件
    </li>
  </ul>
</template>

<style scoped lang="scss">

</style>