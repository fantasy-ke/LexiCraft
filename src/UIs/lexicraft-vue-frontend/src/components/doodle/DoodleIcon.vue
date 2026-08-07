<script lang="ts" setup>
withDefaults(defineProps<{
  name: 'home' | 'words' | 'book' | 'settings' | 'feedback' | 'help' | 'note' | 'arrow' | 'sun' | 'moon' | 'user' | 'spark' | 'check' | 'calendar' | 'target' | 'logout'
  size?: number | string
  title?: string
}>(), {
  size: 24,
  title: ''
})
</script>

<template>
  <svg
      :aria-hidden="title ? undefined : 'true'"
      :aria-label="title || undefined"
      :height="size"
      :width="size"
      class="doodle-icon"
      fill="none"
      role="img"
      viewBox="0 0 24 24"
  >
    <title v-if="title">{{ title }}</title>
    <g v-if="name === 'home'">
      <path d="M3.5 11.2 11.7 3l8.8 8.1"/>
      <path d="M5.8 9.5v10.3h12.6V9.6M9.2 19.5v-5.8h5.5v5.8"/>
      <path d="M4.2 12c4.9-.7 10.1-.5 15.2.1" class="doodle-light"/>
    </g>
    <g v-else-if="name === 'words'">
      <path d="M4.2 5.1c3.2-1.1 6.2-.5 7.7 1.2v13c-1.6-1.7-4.5-2.2-7.7-1.1z"/>
      <path d="M19.8 5.1c-3.2-1.1-6.2-.5-7.7 1.2v13c1.6-1.7 4.5-2.2 7.7-1.1z"/>
      <path d="M6.6 8.5c1.1-.2 2.1 0 3 .4M14.5 8.8c1-.4 2-.5 3-.2M6.7 12c1-.2 2 0 2.9.4M14.5 12.3c1-.4 2-.5 3-.2" class="doodle-light"/>
    </g>
    <g v-else-if="name === 'book'">
      <path d="M5 3.8h11.7A2.3 2.3 0 0 1 19 6.1v13.7H6.1A2.6 2.6 0 0 1 3.5 17V6.2A2.5 2.5 0 0 1 6 3.8"/>
      <path d="M6.3 16.1h12.5M7.6 7.5h7.9M7.4 10.6h6" class="doodle-light"/>
    </g>
    <g v-else-if="name === 'settings'">
      <path d="M9.8 3.2h4l.7 2.2 2 .8 2.1-1 2 3.4-1.7 1.5.2 2.2 1.7 1.5-2 3.5-2.2-.9-2 .9-.6 2.3h-4l-.7-2.2-2-.8-2.1 1-2-3.4 1.7-1.5-.2-2.2-1.7-1.5 2-3.5 2.2.9 2-.9z"/>
      <path d="M15.3 11.7a3.2 3.2 0 1 1-6.4.1 3.2 3.2 0 0 1 6.4-.1Z"/>
    </g>
    <g v-else-if="name === 'feedback'">
      <path d="M4.2 4.8h15.4v11H9.3L5 19.3l.7-3.5H4.2z"/>
      <path d="M8 9.5c2.5-1.2 5.3-1.2 8 0M8.7 12.3h4.8" class="doodle-light"/>
    </g>
    <g v-else-if="name === 'help'">
      <path d="M12.3 3.2c5.2.1 8.6 3.7 8.3 8.8-.3 5-4 8.4-8.9 8.1-5-.3-8.5-3.9-8.2-8.9.3-4.8 3.9-8.1 8.8-8Z"/>
      <path d="M9.5 9c.2-1.6 1.2-2.5 2.8-2.5 1.8 0 2.9 1 2.8 2.6 0 2-2.3 2.2-2.5 4.2M12.4 16.7h.1"/>
    </g>
    <g v-else-if="name === 'note'">
      <path d="M5 3.5h11.2l3 3v14H5z"/>
      <path d="M16.2 3.7v3.4h3M8.1 10h7.7M8 13.4h8M8 16.8h5" class="doodle-light"/>
    </g>
    <g v-else-if="name === 'arrow'">
      <path d="M4 12.4c4.7-.5 9.6-.5 15.5 0"/>
      <path d="m15 7.4 4.8 5-5 4.4"/>
    </g>
    <g v-else-if="name === 'sun'">
      <path d="M15.9 12.1a3.9 3.9 0 1 1-7.8-.2 3.9 3.9 0 0 1 7.8.2ZM12 2.6v2M12 19.4v2M2.7 12h2M19.3 12h2M5.3 5.4l1.4 1.4M17.3 17.3l1.4 1.4M18.8 5.3l-1.5 1.4M6.7 17.3l-1.5 1.4"/>
    </g>
    <g v-else-if="name === 'moon'">
      <path d="M18.8 15.3A8.1 8.1 0 0 1 8.7 5.2a8.2 8.2 0 1 0 10.1 10.1Z"/>
      <path d="m16.3 4 .4 1.2 1.2.4-1.2.4-.4 1.2-.4-1.2-1.2-.4 1.2-.4z" class="doodle-light"/>
    </g>
    <g v-else-if="name === 'user'">
      <path d="M15.8 8.1a3.8 3.8 0 1 1-7.6-.2 3.8 3.8 0 0 1 7.6.2ZM4.4 20c.5-4.2 3-6.3 7.6-6.3s7.1 2.1 7.6 6.3"/>
    </g>
    <g v-else-if="name === 'spark'">
      <path d="m12 2.8 1.6 5.6 5.6 1.6-5.6 1.7-1.6 5.6-1.6-5.6L4.8 10l5.6-1.6z"/>
      <path d="m18.5 15 .7 2.1 2.1.7-2.1.7-.7 2.1-.7-2.1-2.1-.7 2.1-.7z" class="doodle-light"/>
    </g>
    <g v-else-if="name === 'check'">
      <path d="M4.4 12.8c1.6 1.3 3.1 2.8 4.5 4.6 3.2-4.5 6.6-8.3 10.4-11.3"/>
      <path d="M20.1 12.4c-.1 5-3.4 8.2-8.2 8.1-5.1-.1-8.6-3.8-8.3-8.7.3-5 4-8.3 9-8.1 2 .1 3.7.7 5 1.8" class="doodle-light"/>
    </g>
    <g v-else-if="name === 'calendar'">
      <path d="M4.2 5.7h15.6v14.1H4.2zM7.5 3v5M16.5 3v5M4.4 9.1h15"/>
      <path d="M7.5 12.1h.1M12 12.1h.1M16.5 12.1h.1M7.5 16h.1M12 16h.1M16.5 16h.1" class="doodle-light"/>
    </g>
    <g v-else-if="name === 'target'">
      <path d="M12 3.1c5.2 0 8.8 3.6 8.8 8.8 0 5.2-3.6 8.9-8.8 8.9-5.1 0-8.8-3.7-8.8-8.9S6.9 3.1 12 3.1Z"/>
      <path d="M12 7.1c2.9 0 4.9 2 4.9 4.9s-2 4.9-4.9 4.9-4.9-2-4.9-4.9 2-4.9 4.9-4.9ZM12 10.6c.8 0 1.4.6 1.4 1.4s-.6 1.4-1.4 1.4-1.4-.6-1.4-1.4.6-1.4 1.4-1.4Z" class="doodle-light"/>
    </g>
    <g v-else-if="name === 'logout'">
      <path d="M10.5 4H5.2v16h5.3M13.4 8.1l4 3.9-4 4M8.7 12h8.4"/>
    </g>
  </svg>
</template>

<style scoped>
.doodle-icon {
  display: block;
  overflow: visible;
  stroke: currentColor;
  stroke-linecap: round;
  stroke-linejoin: round;
  stroke-width: 1.9;
  vector-effect: non-scaling-stroke;
}

.doodle-light {
  opacity: 0.58;
  stroke-width: 1.45;
}
</style>