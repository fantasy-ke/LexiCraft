<script setup lang="ts">
let logList = [
  {
    date: '2026/01/06',
    content: '优化书籍详情页面',
  },
  {
    date: '2025/12/30',
    content: '移除“继续默写”选项',
  },
  {
    date: '2025/12/29',
    content: '单词练习界面，底部工具栏新增音频设置按钮',
  },
  {
    date: '2025/12/27',
    content: '优化进度条展示，现可展示当前阶段、所有阶段',
  },
  {
    date: '2025/12/23',
    content: '新增复习、自测、默写、听写模式',
  },
  {
    date: '2025/12/20',
    content: '新增资源分享页面',
  },
  {
    date: '2025/12/17',
    content: '新增帮助页面',
  },
  {
    date: '2025/12/16',
    content: '修复弹框内边距太小；单词、文章、通用设置在设置页面、练习界面均可进行设置',
  },
  {
    date: '2025/12/15',
    content: '修复在黑暗模式下，翻译颜色不正确；支持中文符号输入',
  },
  {
    date: '2025/12/11',
    content: '修复音标显示错误问题，优化反馈页面',
  },
  {
    date: '2025/12/10',
    content: '新增选项：复习比(单词练习时，复习词与新词的比例)',
  },
  {
    date: '2025/12/5',
    content: '解决练习界面无法复制、全选的问题',
  },
  {
    date: '2025/12/3',
    content: '单词、文章设置修改为弹框，更方便',
  },
  {
    date: '2025/12/3',
    content: '录入新概念（三、四）部分音频，优化文章相关功能',
  },
  {
    date: '2025/12/2',
    content: '完成新概念（一）音频，优化文章管理页面',
  },
  {
    date: '2025/11/30',
    content: '文章里的单词可点击播放',
  },
  {
    date: '2025/11/29',
    content: '修改 Slider 组件显示bug，新增 IE 浏览器检测提示',
  },
  {
    date: '2025/11/28',
    content: '新增引导框、 新增词典测试模式（由大佬hebeihang 开发）',
  },
  {
    date: '2025/11/25',
    content: '文章练习新增人名忽略功能（新概念一已全部适配），上传了新概念（一）1-18 音频',
  },
  {
    date: '2025/11/23',
    content: '优化练习完成结算界面，新增分享功能',
  },
  {
    date: '2025/11/22',
    content: '适配移动端',
  },
  {
    date: '2025/11/16',
    content: '自测单词时，不认识单词可以直接输入，自动标识为错误单词，无需按2',
  },
  {
    date: '2025/11/15',
    content: '练习单词时，底部工具栏新增“跳到下一阶段”按钮',
  },
  {
    date: '2025/11/14',
    content:
      '新增文章练习时可跳过空格：如果在单词的最后一位上，不按空格直接输入下一个字母的话，自动跳下一个单词，按空格也自动跳下一个单词',
  },
  {
    date: '2025/11/13',
    content: '新增文章练习时“输入时忽略符号/数字”选项',
  },
  {
    date: '2025/11/6',
    content: '新增随机复习功能',
  },
  {
    date: '2025/10/30',
    content: '集成PWA基础配置，支持用户以类App形式打开项目',
  },
  {
    date: '2025/10/26',
    content: '进一步完善单词练习，解决复习数量太多的问题',
  },
  {
    date: '2025/10/8',
    content: '文章支持自动播放下一篇',
  },
  {
    date: '2025/9/14',
    content: '完善文章编辑、导入、导出等功能',
  },
  {
    date: '2025/8/10',
    content: '2.0版本发布，全新UI，全新逻辑，新增短语、例句、近义词等功能',
  },
  {
    date: '2025/7/19',
    content: '1.0版本发布',
  },
]
</script>

<template>
  <div>
    <div class="log-item" v-for="item in logList" :key="item.date">
      <div class="mb-2">
        <div>
          <div>日期：{{ item.date }}</div>
          <div>内容：{{ item.content }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.log-item {
  border-bottom: 1px solid var(--color-input-border);
  margin-bottom: 1rem;
}
</style>
