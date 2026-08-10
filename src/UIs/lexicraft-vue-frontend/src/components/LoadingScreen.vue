<template>
  <div class="loading-screen">
    <div class="loading-center">
      <div class="loading-mark" aria-hidden="true"><span>L</span><i></i></div>
      <div class="loading-title"><strong>LexiCraft</strong><small>YOUR LANGUAGE JOURNAL</small></div>
    </div>

    <div class="loading-bottom-section">
      <p v-if="loadingText" class="loading-tip">{{ loadingText }}</p>
      <div class="progress-area">
        <span class="loading-label">LOADING</span>
        <div class="progress-track" role="progressbar" :aria-valuenow="Math.round(progress)" aria-valuemin="0" aria-valuemax="100">
          <div :style="{width: `${progress}%`}" class="progress-fill"></div>
        </div>
        <span class="percentage-label">{{ Math.round(progress) }}%</span>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
interface Props {
  progress: number
  loadingText?: string
}

withDefaults(defineProps<Props>(), {
  loadingText: '正在整理学习书页...'
})
</script>

<style lang="scss" scoped>
.loading-screen { position: fixed; inset: 0; z-index: 9999; display: flex; align-items: center; justify-content: center; overflow: hidden; color: var(--text-primary); background-color: var(--surface-page); background-image: var(--texture-image); background-size: var(--texture-size); transition: color .3s ease, background .3s ease; }
.loading-center { display: flex; align-items: center; gap: 17px; animation: breathe 2.8s ease-in-out infinite; }
.loading-mark { position: relative; display: grid; width: 58px; height: 58px; place-items: center; border: 1px solid var(--border-strong); border-radius: var(--radius-control); color: var(--accent); background: var(--surface-card); box-shadow: var(--control-shadow); font-family: var(--font-heading); font-size: 32px; font-style: italic; }
.loading-mark i { position: absolute; right: -8px; bottom: -7px; width: 22px; height: 22px; border: 1px solid var(--accent); border-radius: 50%; }
.loading-title strong, .loading-title small { display: block; }
.loading-title strong { font-family: var(--font-heading); font-size: 2rem; font-weight: 500; }
.loading-title small { margin-top: 4px; color: var(--text-tertiary); font-family: var(--font-mono); font-size: .58rem; letter-spacing: .18em; }
.loading-bottom-section { position: absolute; right: 50%; bottom: 11%; display: flex; width: min(620px, calc(100% - 42px)); flex-direction: column; align-items: center; gap: 1.2rem; transform: translateX(50%); }
.loading-tip { margin: 0; color: var(--text-secondary); font-size: .85rem; text-align: center; }
.progress-area { display: grid; width: 100%; grid-template-columns: 68px 1fr 48px; align-items: center; gap: 1rem; }
.loading-label, .percentage-label { color: var(--text-tertiary); font-family: var(--font-mono); font-size: .65rem; font-weight: 800; letter-spacing: .13em; }
.percentage-label { text-align: right; }
.progress-track { position: relative; height: 4px; overflow: hidden; background: var(--surface-muted); }
.progress-fill { position: relative; height: 100%; background: var(--accent); transition: width .35s ease; }
.progress-fill::after { content: ''; position: absolute; inset: 0; background: linear-gradient(90deg, transparent, color-mix(in srgb, var(--accent-contrast) 42%, transparent), transparent); animation: shimmer 1.8s infinite; }

:global(html[data-theme-style='editorial'] .loading-screen) { background-image: repeating-linear-gradient(0deg, transparent 0 6px, color-mix(in srgb, var(--text-primary) 2%, transparent) 7px); }
:global(html[data-theme-style='editorial'] .loading-mark) { border-radius: 50%; }
:global(html[data-theme-style='editorial'] .progress-track::after) { content: ''; position: absolute; inset: -5px 50% -5px auto; width: 1px; background: var(--surface-page); box-shadow: 1px 0 0 var(--border-color); }
:global(html[data-theme-style='zen'] .loading-center) { align-items: baseline; gap: 10px; animation: none; }
:global(html[data-theme-style='zen'] .loading-mark) { width: auto; height: auto; border: 0; border-radius: 0; background: transparent; box-shadow: none; font-family: var(--font-mono); font-size: 1rem; font-style: normal; }
:global(html[data-theme-style='zen'] .loading-mark i), :global(html[data-theme-style='zen'] .loading-title small) { display: none; }
:global(html[data-theme-style='zen'] .loading-title strong) { font-family: var(--font-mono); font-size: 1rem; font-weight: 400; }
:global(html[data-theme-style='zen'] .loading-tip) { font-family: var(--font-mono); font-size: .72rem; }
:global(html[data-theme-style='zen'] .progress-track) { height: 1px; }
:global(html[data-theme-style='ink'] .loading-mark) { border-width: 2px; border-radius: 50% 43% 48% 45%; box-shadow: 5px 6px 0 var(--shadow-color); font-family: var(--font-hand); transform: rotate(-3deg); }
:global(html[data-theme-style='ink'] .loading-title strong) { font-family: var(--font-hand); font-weight: 800; transform: rotate(1deg); }
:global(html[data-theme-style='ink'] .loading-tip) { color: var(--accent); font-family: var(--font-hand); font-size: 1rem; font-weight: 800; transform: rotate(-1deg); }
:global(html[data-theme-style='ink'] .progress-track) { height: 8px; border: 2px solid var(--border-strong); border-radius: 8px 5px 9px 6px; background: transparent; }

@keyframes breathe { 50% { opacity: .82; transform: translateY(-3px); } }
@keyframes shimmer { from { transform: translateX(-100%); } to { transform: translateX(100%); } }
@media (max-width: 620px) { .loading-center { transform: scale(.9); } .progress-area { grid-template-columns: 50px 1fr 38px; gap: .6rem; } .loading-label, .percentage-label { font-size: .55rem; } }
@media (prefers-reduced-motion: reduce) { .loading-center, .progress-fill::after { animation: none; } }
</style>
