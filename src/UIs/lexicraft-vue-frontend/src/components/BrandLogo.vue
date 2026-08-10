<template>
  <span class="brand-logo" :class="{compact, 'brand-logo--light': light}" aria-hidden="true">
    <svg class="brand-logo__mark" viewBox="0 0 48 48" role="img">
      <path class="brand-logo__paper" d="M8 11.5c6.5-2.7 11.9-1.8 16 2.3v23c-4.1-4.1-9.5-5-16-2.3z"/>
      <path class="brand-logo__paper" d="M40 11.5c-6.5-2.7-11.9-1.8-16 2.3v23c4.1-4.1 9.5-5 16-2.3z"/>
      <path class="brand-logo__line" d="M24 14v23M10 13.5c4.7-1.5 9-.7 14 2.5M38 13.5c-4.7-1.5-9-.7-14 2.5"/>
      <path class="brand-logo__spark" d="M36.5 5.5v5M34 8h5M37.5 6.5l2.5-2.5"/>
    </svg>
    <span v-if="!compact" class="brand-logo__copy">
      <strong>LexiCraft</strong>
      <small>{{ tagline }}</small>
    </span>
  </span>
</template>

<script lang="ts" setup>
withDefaults(defineProps<{
  compact?: boolean
  light?: boolean
  tagline?: string
}>(), {
  compact: false,
  light: false,
  tagline: 'language journal'
})
</script>

<style lang="scss" scoped>
.brand-logo {
  display: inline-flex;
  align-items: center;
  gap: .62rem;
  color: var(--text-primary);
}
.brand-logo__mark {
  width: 42px;
  height: 42px;
  overflow: visible;
  flex: 0 0 auto;
}
.brand-logo__paper {
  fill: var(--surface-card);
  stroke: var(--border-strong);
  stroke-width: 1.6;
  stroke-linejoin: round;
}
.brand-logo__line {
  fill: none;
  stroke: var(--accent);
  stroke-linecap: round;
  stroke-linejoin: round;
  stroke-width: 1.7;
}
.brand-logo__spark {
  fill: none;
  stroke: var(--accent);
  stroke-linecap: round;
  stroke-width: 1.8;
}
.brand-logo__copy { display: flex; flex-direction: column; align-items: flex-start; line-height: 1; }
.brand-logo__copy strong { font-family: var(--font-heading); font-size: 1.05rem; letter-spacing: -.025em; }
.brand-logo__copy small { margin-top: .25rem; color: var(--text-tertiary); font-family: var(--font-mono); font-size: .54rem; letter-spacing: .14em; text-transform: uppercase; }
.brand-logo.compact .brand-logo__mark { width: 38px; height: 38px; }
.brand-logo--light { --text-primary: #fffaf0; --surface-card: rgba(255, 250, 240, .14); --border-strong: rgba(255, 250, 240, .9); --accent: #f1b49b; --text-tertiary: rgba(255, 250, 240, .7); }
</style>
