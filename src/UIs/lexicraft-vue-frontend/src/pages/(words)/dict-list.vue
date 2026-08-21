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


function groupByDictTags(dictList: DictResource[]): Record<string, DictResource[]> {
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
  return grouped
}

const {data: dict_list, isFetching} = useFetch(resourceWrap(DICT_LIST.WORD.ALL)).json()

const groupedByCategoryAndTag = $computed(() => {
  let data: [string, Record<string, DictResource[]>][] = []
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
    <div v-loading="isFetching" class="catalog-page dict-list-page">
      <div class="catalog-header">
        <BackIcon class="catalog-back" @click="router.back"/>
        
        <div v-if="showSearchInput" class="catalog-search">
          <div class="catalog-search__field">
            <BaseInput v-model="searchKey" autofocus clearable placeholder="搜索想学习的词典"/>
            <IconFluentSearch24Regular aria-hidden="true"/>
          </div>
          <BaseButton type="info" class="catalog-cancel" @click="showSearchInput = false, searchKey = ''">取消</BaseButton>
        </div>

        <div v-else class="catalog-heading">
          <div>
            <p class="catalog-kicker">AVAILABLE DICTIONARIES</p>
            <h1>词典列表</h1>
          </div>
          <button class="catalog-search-button" type="button" aria-label="打开搜索" @click="showSearchInput = true">
            <IconFluentSearch24Regular aria-hidden="true"/>
            <span>搜索</span>
          </button>
        </div>
      </div>
      <div v-if="searchKey" class="catalog-results">
        <DictList
            v-if="searchList.length "
            :list="searchList"
            :select-id="'-1'"
            quantifier="词"
            @selectDict="selectDict"/>
        <Empty v-else text="没有相关词典"/>
      </div>
      <div v-else class="catalog-results">
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
.catalog-page { min-height: 200px; padding: clamp(18px, 3vw, 34px) !important; overflow: hidden; color: var(--text-primary); }
.catalog-header { display: flex; align-items: flex-start; gap: 20px; margin-bottom: 34px; }
.catalog-back { flex: 0 0 auto; margin-top: 3px; color: var(--text-secondary); }
.catalog-back:hover { color: var(--accent); }
.catalog-heading, .catalog-search { display: flex; min-width: 0; flex: 1; align-items: center; justify-content: space-between; gap: 20px; }
.catalog-kicker { margin: 0 0 8px; color: var(--accent); font-family: var(--font-mono); font-size: 10px; letter-spacing: .14em; }
.catalog-heading h1 { margin: 0; color: var(--text-primary); font-family: var(--font-heading); font-size: clamp(28px, 3vw, 42px); font-weight: 500; line-height: 1.08; }
.catalog-search__field { position: relative; min-width: 0; flex: 1; }
.catalog-search__field :deep(.base-input) { width: 100%; border-width: 0 0 1px; border-radius: 0; background: transparent; }
.catalog-search__field > svg { position: absolute; top: 50%; right: 2px; color: var(--text-tertiary); pointer-events: none; transform: translateY(-50%); }
.catalog-cancel { flex: 0 0 auto; }
.catalog-search-button { display: inline-flex; align-items: center; gap: 8px; padding: 8px 0; border: 0; border-bottom: 1px solid var(--border-strong); color: var(--text-secondary); background: transparent; cursor: pointer; font: inherit; font-size: 12px; }
.catalog-search-button:hover { color: var(--accent); border-color: var(--accent); }
.catalog-search-button svg { width: 16px; height: 16px; }
.catalog-results { min-width: 0; }
@media (max-width: 640px) {
  .catalog-page { padding: 16px !important; }
  .catalog-header { gap: 12px; margin-bottom: 26px; }
  .catalog-heading, .catalog-search { align-items: flex-start; flex-direction: column; gap: 14px; }
  .catalog-search { flex-direction: row; align-items: center; }
  .catalog-cancel { padding-inline: 8px; }
}
@media (max-width: 420px) {
  .catalog-search { align-items: stretch; flex-direction: column; }
  .catalog-cancel { align-self: flex-end; }
}
</style>
