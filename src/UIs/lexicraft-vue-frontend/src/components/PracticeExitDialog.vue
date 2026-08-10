<script lang="ts" setup>
interface Props {
  visible: boolean
  listLabel: string
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible', 'exit-home', 'exit-list', 'continue'])


const handleClose = () => {
  emit('update:visible', false)
}

const exitHome = () => {
  emit('exit-home')
  handleClose()
}

const exitList = () => {
  emit('exit-list')
  handleClose()
}
</script>

<template>
  <div v-if="visible" class="exit-dialog-overlay" @click.self="handleClose">
    <div class="exit-dialog">
      <div class="dialog-header">
        <h3 class="title">退出练习</h3>
        <button class="close-icon-btn" @click="handleClose">✕</button>
      </div>

      <div class="dialog-body">
        <!-- 提示卡片 -->
        <div class="notice-card">
          短暂的休息能让学习更有效率，期待你的回归！
        </div>

        <!-- 选项列表 -->
        <div class="options-list">
          <div class="option-item" @click="exitHome">
            <div class="option-left">
              <IconFluentHome20Regular class="icon"/>
              <span class="label">返回首页</span>
            </div>
            <IconFluentArrowRight16Regular class="arrow"/>
          </div>

          <div class="option-item" @click="exitList">
            <div class="option-left">
              <IconFluentList20Regular v-if="listLabel.includes('列表')" class="icon"/>
              <IconFluentBook20Regular v-else class="icon"/>
              <span class="label">{{ listLabel }}</span>
            </div>
            <IconFluentArrowRight16Regular class="arrow"/>
          </div>
        </div>

        <!-- 底部按钮 -->
        <button class="continue-btn" @click="$emit('continue')">
          <IconFluentPlay20Regular class="play-icon"/>
          继续学习
        </button>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.exit-dialog-overlay { position: fixed; inset: 0; z-index: 10000; display: flex; align-items: center; justify-content: center; padding: 1.5rem; background: color-mix(in srgb, #000 58%, transparent); backdrop-filter: blur(6px); }
.exit-dialog { width: min(100%, 420px); overflow: hidden; border: 1px solid var(--border-strong); border-radius: var(--radius-card); color: var(--text-primary); background: var(--surface-card); box-shadow: var(--card-shadow); animation: slide-up .26s ease-out; }
.dialog-header { display: flex; align-items: center; justify-content: space-between; padding: 1.2rem 1.3rem; border-bottom: 1px solid var(--border-color); }
.dialog-header .title { margin: 0; color: var(--text-primary); font-family: var(--font-heading); font-size: 1.2rem; font-weight: 500; }
.close-icon-btn { display: grid; width: 30px; height: 30px; place-items: center; border: 0; color: var(--text-secondary); background: transparent; cursor: pointer; }
.close-icon-btn:hover { color: var(--danger); }
.dialog-body { display: flex; flex-direction: column; gap: 1rem; padding: 1.25rem; }
.notice-card { padding: .9rem 1rem; border-left: 3px solid var(--accent); color: var(--text-secondary); background: var(--accent-soft); font-size: .85rem; line-height: 1.5; }
.options-list { display: grid; gap: .6rem; }
.option-item { display: flex; align-items: center; justify-content: space-between; padding: .8rem 1rem; border: 1px solid var(--border-color); border-radius: var(--radius-control); cursor: pointer; transition: border-color .2s ease, background .2s ease; }
.option-item:hover { border-color: var(--accent); background: var(--hover-bg); }
.option-left { display: flex; align-items: center; gap: .8rem; }
.option-left .icon { color: var(--accent); }
.option-left .label { color: var(--text-primary); font-size: .9rem; font-weight: 600; }
.arrow { color: var(--text-tertiary); transition: transform .2s ease; }
.option-item:hover .arrow { color: var(--accent); transform: translateX(3px); }
.continue-btn { display: flex; align-items: center; justify-content: center; gap: .5rem; margin-top: .25rem; padding: .85rem; border: 1px solid var(--accent); border-radius: var(--radius-control); color: var(--accent-contrast); background: var(--accent); cursor: pointer; font: inherit; font-weight: 700; }
.continue-btn:hover { background: var(--accent-hover); }
:global(html[data-theme-style='editorial'] .exit-dialog) { border-top-width: 5px; }
:global(html[data-theme-style='zen'] .exit-dialog) { border-inline: 0; box-shadow: none; }
:global(html[data-theme-style='zen'] .option-item), :global(html[data-theme-style='zen'] .continue-btn) { border-radius: 0; font-family: var(--font-mono); }
:global(html[data-theme-style='ink'] .exit-dialog) { border-width: 2px; transform: rotate(-.35deg); }
:global(html[data-theme-style='ink'] .notice-card), :global(html[data-theme-style='ink'] .option-item), :global(html[data-theme-style='ink'] .continue-btn) { border-width: 2px; font-family: var(--font-hand); }
@keyframes slide-up { from { opacity: 0; transform: translateY(16px); } }
</style>
