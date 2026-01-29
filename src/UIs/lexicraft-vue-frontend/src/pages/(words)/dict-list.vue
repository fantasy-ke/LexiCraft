<script lang="ts" setup>
import {_nextTick, groupBy, isMobile, loadJsLib, resourceWrap, useNav} from "@/utils";
import BasePage from "@/components/BasePage.vue";
import type {DictResource} from "@/types/types.ts";
import {useRuntimeStore} from "@/stores/runtime.ts";
import BaseIcon from "@/components/BaseIcon.vue";
import Empty from "@/components/Empty.vue";
import BaseButton from "@/components/BaseButton.vue";
import DictList from "@/components/list/DictList.vue";
import BackIcon from "@/components/BackIcon.vue";
import DictGroup from "@/components/list/DictGroup.vue";
import {useBaseStore} from "@/stores/base.ts";
import {useRouter} from "vue-router";
import {computed, watch} from "vue";
import {getDefaultDict} from "@/types/func.ts";
import {useFetch} from "@vueuse/core";
import {DICT_LIST, LIB_JS_URL, TourConfig} from "@/config/env.ts";
import BaseInput from "@/components/base/BaseInput.vue";
import {useSettingStore} from "@/stores/setting.ts";

const {nav} = useNav()
const runtimeStore = useRuntimeStore()
const settingStore = useSettingStore()
const store = useBaseStore()
const router = useRouter()

function selectDict(e: any) {
  console.log(e.dict)
  getDictDetail(e.dict)
}

async function getDictDetail(val: DictResource) {
  runtimeStore.editDict = getDefaultDict(val)
  nav('/app/dict-detail', {from: 'list'})
}


function groupByDictTags(dictList: DictResource[]): [string, DictResource[]][] {
  const grouped = dictList.reduce<Record<string, DictResource[]>>((result, dict) => {
    dict.tags.forEach((tag) => {
      if (result[tag]) {
        result[tag].push(dict)
      } else {
        result[tag] = [dict]
      }
    })
    return result
  }, {})
  return Object.entries(grouped)
}

const {data: dict_list, isFetching} = useFetch(resourceWrap(DICT_LIST.WORD.ALL)).json()

const groupedByCategoryAndTag = $computed(() => {
  let data: [string, [string, DictResource[]][]] [] = []
  if (!dict_list.value) return data
  const groupByCategory = groupBy(dict_list.value, 'category')
  for (const [key, value] of Object.entries(groupByCategory)) {
    data.push([key, groupByDictTags(value as DictResource[])])
  }
  if (data.length > 3) {
    [data[2], data[3]] = [data[3], data[2]];
  }
  // console.log('data', data)
  return data
})

let showSearchInput = $ref(false)
let searchKey = $ref('')

const searchList = computed<any[]>(() => {
  if (searchKey) {
    let s = searchKey.toLowerCase()
    return dict_list.value.filter((item) => {
      return item.id.toLowerCase().includes(s)
          || item.name.toLowerCase().includes(s)
          || item.category.toLowerCase().includes(s)
          || item.tags.join('').replace('所有', '').toLowerCase().includes(s)
          || item?.url?.toLowerCase?.().includes?.(s)
    })
  }
  return []
})

watch(dict_list, (val) => {
  if (!val.length) return
  let cet4 = val.find(v => v.id === 'cet4')
  if (!cet4) return
  _nextTick(async () => {
    const Shepherd = await loadJsLib('Shepherd', LIB_JS_URL.SHEPHERD);
    const tour = new Shepherd.Tour(TourConfig);
    tour.on('cancel', () => {
      localStorage.setItem('tour-guide', '1');
    });
    tour.addStep({
      id: 'step2',
      text: '选一本自己准备学习的词典',
      attachTo: {element: '#cet4', on: 'bottom'},
      buttons: [
        {
          text: `下一步（2/${TourConfig.total}）`,
          action() {
            tour.next()
            selectDict({dict: cet4})
          }
        }
      ]
    });

    const r = localStorage.getItem('tour-guide');
    if (settingStore.first && !r && !isMobile()) {
      tour.start();
    }
  }, 500)
})

</script>

<template>
  <BasePage>
    <div v-loading="isFetching" class="card-white min-h-200 dict-list-page p-8! overflow-hidden relative">
      <!-- Decorator Background -->
      <div class="absolute -right-20 -top-20 w-80 h-80 bg-blue-500/5 rounded-full blur-3xl pointer-events-none"></div>
      <div class="absolute -left-20 bottom-20 w-64 h-64 bg-indigo-500/5 rounded-full blur-3xl pointer-events-none"></div>

      <div class="flex items-center relative gap-6 header-section mb-10">
        <div class="z-10">
          <BackIcon class="transition-transform hover:-translate-x-1" @click='router.back'/>
        </div>
        
        <div v-if="showSearchInput" class="flex flex-1 gap-4 items-center z-10">
          <div class="flex-1 relative">
            <BaseInput v-model="searchKey" autofocus class="w-full !rounded-2xl !bg-slate-50 dark:!bg-slate-800/40 border-none shadow-inner" clearable placeholder="搜索您想学习的词典..."/>
            <IconFluentSearch24Regular class="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none"/>
          </div>
          <BaseButton type="info" class="rounded-xl px-6 h-11 bg-white/50 dark:bg-slate-800/50" @click="showSearchInput = false, searchKey = ''">取消</BaseButton>
        </div>
        
        <div v-else class="flex flex-1 items-center justify-between z-10">
          <div class="page-title-group">
            <h1 class="text-3xl font-black grad-text m-0 leading-tight">词典列表</h1>
            <p class="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mt-1">AVAILABLE DICTIONARIES</p>
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
            quantifier="词"
            @selectDict="selectDict"/>
        <Empty v-else text="没有相关词典"/>
      </div>
      <div v-else class="w-full">
        <DictGroup
            v-for="item in groupedByCategoryAndTag"
            :category="item[0]"
            :groupByTag="item[1]"
            :select-id="store.sdict.id"
            quantifier="词"
            @selectDict="selectDict"
        />
      </div>
    </div>
  </BasePage>
</template>

<style lang="scss" scoped>
// 移动端适配
@media (max-width: 768px) {
  .dict-list-page {
    padding: 0.8rem;
    margin-bottom: 1rem;

    .header-section {
      flex-direction: column;
      gap: 0.5rem;

      .flex.flex-1.gap-4 {
        width: 100%;

        .base-input {
          font-size: 0.9rem;
        }

        .base-button {
          padding: 0.5rem 0.8rem;
          font-size: 0.9rem;
        }
      }

      .py-1.flex.flex-1.justify-end {
        width: 100%;

        .page-title {
          font-size: 1.2rem;
        }

        .base-icon {
          font-size: 1.2rem;
        }
      }
    }

    .mt-4 {
      margin-top: 0.8rem;
    }
  }
}

// 超小屏幕适配
@media (max-width: 480px) {
  .dict-list-page {
    padding: 0.5rem;

    .header-section {
      .flex.flex-1.gap-4 {
        .base-input {
          font-size: 0.8rem;
        }

        .base-button {
          padding: 0.4rem 0.6rem;
          font-size: 0.8rem;
        }
      }

      .py-1.flex.flex-1.justify-end {
        .page-title {
          font-size: 1rem;
        }

        .base-icon {
          font-size: 1rem;
        }
      }
    }
  }
}
</style>
