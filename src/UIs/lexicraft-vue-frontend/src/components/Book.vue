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
}

const props = withDefaults(defineProps<IProps>(), {
  showProgress: true,
  isUser: false
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
      <span class="add-text">添加词典</span>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.book-card {
  width: 100%; min-width: 0; min-height: 178px; overflow: hidden; display: flex; flex-direction: column;
  border: 1px solid var(--color-item-border); border-radius: max(10px, var(--radius-card));
  color: var(--text-primary); background: var(--color-card-bg); box-shadow: var(--control-shadow); cursor: pointer;
  transition: transform .22s ease, border-color .22s ease, box-shadow .22s ease;
}
.book-card:hover { border-color: var(--accent); box-shadow: var(--card-shadow); transform: translateY(-4px); }
.book-cover { position: relative; height: 112px; overflow: hidden; background: var(--surface-muted); }
.img-cover { width: 100%; height: 100%; object-fit: cover; }
.cover-placeholder { display: grid; width: 100%; height: 100%; place-items: center; color: var(--accent); background: linear-gradient(145deg, var(--accent-soft), var(--surface-muted)); font-family: var(--font-heading); font-size: 2rem; font-weight: 800; }
.status-tag { position: absolute; top: .55rem; z-index: 2; padding: .22rem .55rem; border-radius: 999px; color: var(--accent-contrast); background: var(--accent); box-shadow: var(--control-shadow); font-size: .62rem; font-weight: 800; letter-spacing: .04em; }
.status-tag.complete { right: .55rem; background: var(--success); }
.status-tag.custom { left: .55rem; }
.checkbox-wrapper { position: absolute; bottom: .45rem; left: .45rem; z-index: 3; padding: 3px; border-radius: 8px; background: var(--surface-overlay); }
.book-info { display: flex; min-height: 66px; flex: 1; flex-direction: column; justify-content: space-between; gap: .45rem; padding: .72rem .78rem .78rem; }
.book-title { margin: 0; overflow: hidden; color: var(--text-primary); font-family: var(--font-heading); font-size: .92rem; font-weight: 750; line-height: 1.3; text-overflow: ellipsis; white-space: nowrap; }
.book-meta { display: flex; justify-content: space-between; color: var(--text-tertiary); font-size: .72rem; line-height: 1.2; }
.add-card { border-style: dashed; background: color-mix(in srgb, var(--surface-card) 70%, transparent); }
.add-card:hover { background: var(--hover-bg); }
.add-content { display: flex; min-height: 176px; flex-direction: column; align-items: center; justify-content: center; gap: .55rem; color: var(--text-tertiary); }
.add-icon { width: 2rem; height: 2rem; color: var(--accent); }
.add-text { font-size: .82rem; font-weight: 700; }
:global(html[data-theme-style='editorial'] .book-card) { border-radius: 8px; }
:global(html[data-theme-style='zen'] .book-card) { border-radius: 6px; box-shadow: none; }
:global(html[data-theme-style='ink'] .book-card) { border-width: 1.5px; border-radius: 17px 13px 19px 14px; transform: rotate(-.18deg); }
:global(html[data-theme-style='ink'] .book-card:hover) { transform: translateY(-4px) rotate(.2deg); }
@media (max-width: 560px) { .book-cover { height: 96px; } .book-card { min-height: 160px; } .add-content { min-height: 158px; } }
</style>
