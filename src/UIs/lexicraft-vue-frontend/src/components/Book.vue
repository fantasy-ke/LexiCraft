<script lang="ts" setup>
import type {Dict} from "@/types/types";
import Checkbox from "@/components/base/checkbox/Checkbox.vue";

interface IProps {
  item?: Partial<Dict>;
  quantifier?: string
  isAdd: boolean
  showCheckbox?: boolean
  checked?: boolean
  showProgress?: boolean
  isUser?: boolean
  addText?: string
}

const props = withDefaults(defineProps<IProps>(), {
  showProgress: true,
  isUser: false,
  addText: '添加词典'
})

defineEmits<{
  check: []
}>()
</script>

<template>
  <div v-if="!isAdd" :id="item?.id" class="book-card">
    <div class="book-cover">
      <!-- Cover Image or Placeholder -->
      <img v-if="item?.cover" :alt="item.name" :src="item.cover" class="img-cover"/>
      <div v-else class="cover-placeholder">
        <span>{{ item?.name?.charAt(0) || 'D' }}</span>
      </div>

      <!-- Status Tags -->
      <div v-if="item?.lastLearnIndex >= item?.length" class="status-tag complete">已完成</div>

      <!-- Custom Tag -->
      <div v-if="item.custom" class="status-tag custom">自定义</div>

      <!-- Checkbox for batch operations -->
      <div v-if="showCheckbox" class="checkbox-wrapper" @click.stop>
        <Checkbox :model-value="checked" @change="$emit('check')"/>
      </div>
    </div>

    <div class="book-info">
      <h4 :title="item?.name" class="book-title">{{ item?.name }}</h4>
      <div class="book-meta">
        <span v-if="showProgress" class="progress">
          {{ item?.lastLearnIndex }}/{{ item?.length }} {{ quantifier }}
        </span>
        <span v-else class="progress">
          {{ item?.length }} {{ quantifier }}
        </span>
      </div>
    </div>
  </div>

  <!-- Add Button Style -->
  <div v-else id="no-book" class="book-card add-card">
    <div class="add-content">
      <IconFluentAdd16Regular class="add-icon"/>
      <span class="add-text">{{ addText }}</span>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.book-card {
  display: flex;
  width: 100%;
  min-width: 0;
  min-height: 174px;
  overflow: hidden;
  flex-direction: column;
  border: 1px solid var(--color-item-border);
  border-radius: var(--radius-card);
  color: var(--text-primary);
  background: var(--color-card-bg);
  cursor: pointer;
  transition: border-color .18s ease, background .18s ease;
}
.book-card:hover { border-color: var(--accent); }
.book-cover { position: relative; height: 108px; overflow: hidden; border-bottom: 1px solid var(--border-color); background: var(--surface-muted); }
.img-cover { width: 100%; height: 100%; object-fit: cover; }
.cover-placeholder { display: grid; width: 100%; height: 100%; place-items: center; color: var(--accent); background: var(--surface-muted); font-family: var(--font-heading); font-size: 1.8rem; font-weight: 650; }
.status-tag { position: absolute; top: .55rem; z-index: 2; padding: .22rem .48rem; border: 1px solid currentColor; border-radius: 4px; color: var(--accent); background: var(--surface-overlay); font-size: .6rem; font-weight: 700; }
.status-tag.complete { right: .55rem; color: var(--success); }
.status-tag.custom { left: .55rem; }
.checkbox-wrapper { position: absolute; bottom: .45rem; left: .45rem; z-index: 3; padding: 3px; border-radius: 6px; background: var(--surface-overlay); }
.book-info { display: flex; min-height: 64px; flex: 1; flex-direction: column; justify-content: space-between; gap: .45rem; padding: .72rem .78rem .78rem; }
.book-title { margin: 0; overflow: hidden; color: var(--text-primary); font-family: var(--font-sans); font-size: .88rem; font-weight: 700; line-height: 1.35; text-overflow: ellipsis; white-space: nowrap; }
.book-meta { display: flex; justify-content: space-between; color: var(--text-tertiary); font-family: var(--font-sans); font-size: .7rem; line-height: 1.2; }
.add-card { border-style: dashed; background: color-mix(in srgb, var(--surface-card) 76%, transparent); }
.add-card:hover { background: var(--hover-bg); }
.add-content { display: flex; min-height: 172px; flex-direction: column; align-items: center; justify-content: center; gap: .55rem; color: var(--text-tertiary); }
.add-icon { width: 1.7rem; height: 1.7rem; color: var(--accent); }
.add-text { font-size: .8rem; font-weight: 650; }
:global(html[data-theme-style='zen'] .book-card) { box-shadow: none; }
@media (max-width: 560px) { .book-cover { height: 94px; } .book-card { min-height: 156px; } .add-content { min-height: 154px; } }
</style>
