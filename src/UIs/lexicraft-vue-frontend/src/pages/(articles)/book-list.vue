<script lang="ts" setup>
import {resourceWrap, useNav} from "@/utils";
import BasePage from "@/components/BasePage.vue";
import type {DictResource} from "@/types/types.ts";
import {useRuntimeStore} from "@/stores/runtime.ts";
import BaseIcon from "@/components/BaseIcon.vue";
import Empty from "@/components/Empty.vue";
import BaseButton from "@/components/BaseButton.vue";
import DictList from "@/components/list/DictList.vue";
import BackIcon from "@/components/BackIcon.vue";
import {useRouter} from "vue-router";
import {computed} from "vue";
import {getDefaultDict} from "@/types/func.ts";
import {useFetch} from "@vueuse/core";
import {DICT_LIST} from "@/config/env.ts";
import BaseInput from "@/components/base/BaseInput.vue";

const {nav} = useNav()
const runtimeStore = useRuntimeStore()
const router = useRouter()

function selectDict(e) {
  console.log(e.dict)
  getDictDetail(e.dict)
}

async function getDictDetail(val: DictResource) {
  runtimeStore.editDict = getDefaultDict(val)
  nav('/app/book-detail', {from: 'list'})
}

let showSearchInput = $ref(false)
let searchKey = $ref('')
const {data: bookList, isFetching} = useFetch(resourceWrap(DICT_LIST.ARTICLE.ALL)).json()

const searchList = computed<any[]>(() => {
  if (searchKey) {
    let s = searchKey.toLowerCase()
    return bookList.value.filter((item) => {
      return item.id.toLowerCase().includes(s)
          || item.name.toLowerCase().includes(s)
          || item.category.toLowerCase().includes(s)
          || item.tags.join('').replace('所有', '').toLowerCase().includes(s)
          || item?.url?.toLowerCase?.().includes?.(s)
    })
  }
  return []
})

</script>

<template>
  <BasePage>
    <div v-loading="isFetching" class="card-white min-h-200 p-8! overflow-hidden relative">
      <!-- Decorator Background -->
      <div class="absolute -right-20 -top-20 w-80 h-80 bg-indigo-500/5 rounded-full blur-3xl pointer-events-none"></div>
      <div class="absolute -left-20 bottom-20 w-64 h-64 bg-blue-500/5 rounded-full blur-3xl pointer-events-none"></div>

      <div class="flex items-center relative gap-6 header-section mb-10">
        <div class="z-10">
          <BackIcon class="transition-transform hover:-translate-x-1" @Click='router.back'/>
        </div>
        
        <div v-if="showSearchInput" class="flex flex-1 gap-4 items-center z-10">
          <div class="flex-1 relative">
            <BaseInput v-model="searchKey" autofocus class="w-full !rounded-2xl !bg-slate-50 dark:!bg-slate-800/40 border-none shadow-inner" clearable placeholder="搜索你想阅读的书籍..."/>
            <IconFluentSearch24Regular class="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none"/>
          </div>
          <BaseButton type="info" class="rounded-xl px-6 h-11 bg-white/50 dark:bg-slate-800/50" @click="showSearchInput = false, searchKey = ''">取消</BaseButton>
        </div>
        
        <div v-else class="flex flex-1 items-center justify-between z-10">
          <div class="page-title-group">
            <h1 class="text-3xl font-black grad-text m-0 leading-tight">书籍列表</h1>
            <p class="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mt-1">EXPLORE LIBRARY</p>
          </div>
          <div class="w-12 h-12 rounded-2xl bg-slate-50 dark:bg-slate-800/40 border border-slate-100 dark:border-slate-800 center cursor-pointer hover:bg-white dark:hover:bg-slate-800 transition-all duration-300 shadow-sm group" @click="showSearchInput = true">
            <IconFluentSearch24Regular class="text-xl text-slate-600 dark:text-slate-300 group-hover:scale-110 transition-transform"/>
          </div>
        </div>
      </div>
      <div v-if="searchKey" class="mt-4">
        <DictList
            v-if="searchList.length "
            :list="searchList"
            :select-id="'-1'"
            quantifier="篇"
            @selectDict="selectDict"/>
        <Empty v-else text="没有相关书籍"/>
      </div>
      <div v-else class="w-full mt-2">
        <DictList
            v-if="bookList?.length "
            :list="bookList"
            :select-id="'-1'"
            quantifier="篇"
            @selectDict="selectDict"/>
      </div>
    </div>
  </BasePage>
</template>

<style lang="scss" scoped>
</style>
