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
    <div v-loading="isFetching" class="card min-h-50">
      <div class="flex items-center relative gap-2">
        <BackIcon class="z-2" @Click='router.back'/>
        <div v-if="showSearchInput" class="flex flex-1 gap-4">
          <BaseInput v-model="searchKey" autofocus class="flex-1" clearable placeholder="请输入书籍名称/缩写/类别"
                     prefix-icon/>
          <BaseButton @click="showSearchInput = false, searchKey = ''">取消</BaseButton>
        </div>
        <div v-else class="py-1 flex flex-1 justify-end">
          <span class="page-title absolute w-full center">书籍列表</span>
          <BaseIcon class="z-1"
                    @click="showSearchInput = true">
            <IconFluentSearch24Regular/>
          </BaseIcon>
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
