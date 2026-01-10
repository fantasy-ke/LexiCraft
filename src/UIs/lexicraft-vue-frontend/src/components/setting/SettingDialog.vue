<script setup lang="ts">
import { defineAsyncComponent } from "vue";
import BaseIcon from "@/components/BaseIcon.vue";
import CommonSetting from "@/components/setting/CommonSetting.vue";
import WordSetting from "@/components/setting/WordSetting.vue";
import ArticleSetting from "@/components/setting/ArticleSetting.vue";

const Dialog = defineAsyncComponent(() => import('@/components/dialog/Dialog.vue'))

const props = defineProps<{
  type: 'article' | 'word'
}>()

const tabIndex = $ref(props.type === 'word' ? 1 : 2)
let show = $ref(false)

</script>

<template>
  <Dialog v-model="show" title="设置" padding>
    <div class="setting text-lg w-200 h-[60vh] text-md flex flex-col">
      <div class="flex flex-1 overflow-hidden">
        <div class="left">
          <div class="tabs">
            <div class="tab" :class="tabIndex === 1 && 'active'" @click="tabIndex = 1" v-if="type === 'word'">
              <IconFluentTextUnderlineDouble20Regular width="20"/>
              <span>单词设置</span>
            </div>
            <div class="tab" :class="tabIndex === 2 && 'active'" @click="tabIndex = 2" v-if="type === 'article'">
              <IconFluentBookLetter20Regular width="20"/>
              <span>文章设置</span>
            </div>
            <div class="tab" :class="tabIndex === 0 && 'active'" @click="tabIndex = 0">
              <IconFluentSettings20Regular width="20"/>
              <span>通用设置</span>
            </div>
          </div>
        </div>
        <div class="content">
          <CommonSetting v-if="tabIndex === 0"/>
          <WordSetting v-if="tabIndex === 1"/>
          <ArticleSetting v-if="tabIndex === 2"/>
        </div>
      </div>
    </div>
  </Dialog>
  <BaseIcon title="设置" @click="show = true;tabIndex = props.type === 'word' ? 1 : 2">
    <IconFluentSettings20Regular/>
  </BaseIcon>
</template>

<style scoped lang="scss">

.setting {

  .left {
    display: flex;
    flex-direction: column;
    justify-content: space-between;
    align-items: center;
    border-right: 2px solid var(--color-line);

    .tabs {
      padding: 1rem;
      padding-left: 0;
      display: flex;
      flex-direction: column;
      gap: .6rem;
      //color: #0C8CE9;

      .tab {
        @apply cursor-pointer flex items-center relative;
        padding: .6rem .9rem;
        border-radius: .5rem;
        width: 8rem;
        gap: .6rem;
        transition: all .5s;

        &:hover {
          background: var(--btn-primary);
          color: white;
        }

        &.active {
          background: var(--btn-primary);
          color: white;
        }
      }
    }
  }

  .content {
    flex: 1;
    height: 100%;
    overflow: auto;
    padding: 0 1.6rem;

    .line {
      border-bottom: 1px solid #c4c3c3;
    }
  }
}
</style>
