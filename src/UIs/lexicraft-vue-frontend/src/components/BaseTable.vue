<script setup lang="tsx">
import { nextTick, onMounted, useSlots } from 'vue'
import MiniDialog from '@/components/dialog/MiniDialog.vue'
import BaseIcon from '@/components/BaseIcon.vue'
import BaseButton from '@/components/BaseButton.vue'
import { debounce } from '@/utils'
import PopConfirm from '@/components/PopConfirm.vue'
import Empty from '@/components/Empty.vue'
import Pagination from '@/components/base/Pagination.vue'
import Checkbox from '@/components/base/checkbox/Checkbox.vue'
import DeleteIcon from '@/components/icon/DeleteIcon.vue'
import Dialog from '@/components/dialog/Dialog.vue'
import BaseInput from '@/components/base/BaseInput.vue'
import { Host } from '@/config/env.ts'
import { Sort } from '@/types/enum.ts'

const props = withDefaults(
  defineProps<{
    loading?: boolean
    showToolbar?: boolean
    showCheckbox?: boolean
    showPagination?: boolean
    exportLoading?: boolean
    importLoading?: boolean
    request?: Function
    list?: any[]
  }>(),
  {
    loading: true,
    showCheckbox: false,
    showToolbar: true,
    showPagination: true,
    exportLoading: false,
    importLoading: false,
  }
)

const emit = defineEmits<{
  add: []
  click: [
    val: {
      item: any
      index: number
    },
  ]
  import: [e: Event]
  export: []
  del: [ids: number[]]
  sort: [type: Sort, pageNo: number, pageSize: number]
}>()

let listRef: any = $ref()
let showCheckbox = $ref(false)

function scrollToBottom() {
  nextTick(() => {
    listRef?.scrollTo(0, listRef.scrollHeight)
  })
}

function scrollToTop() {
  nextTick(() => {
    listRef?.scrollTo(0, 0)
  })
}

function scrollToItem(index: number) {
  nextTick(() => {
    listRef?.children[index]?.scrollIntoView({ block: 'center', behavior: 'smooth' })
  })
}

let selectIds = $ref([])
let selectAll = $computed(() => {
  return !!selectIds.length
})

function toggleSelect(item) {
  let rIndex = selectIds.findIndex(v => v === item.id)
  if (rIndex > -1) {
    selectIds.splice(rIndex, 1)
  } else {
    selectIds.push(item.id)
  }
}

function toggleSelectAll() {
  if (selectAll) {
    selectIds = []
  } else {
    selectIds = params.list.map(v => v.id)
  }
}

let showSortDialog = $ref(false)
let showSearchInput = $ref(false)
let showImportDialog = $ref(false)

const closeImportDialog = () => (showImportDialog = false)

function sort(type: Sort) {
  if ([Sort.reverse, Sort.random].includes(type)) {
    emit('sort', type, params.pageNo, params.pageSize)
  } else {
    emit('sort', type, 1, params.total)
  }
  showSortDialog = false
}

function handleBatchDel() {
  emit('del', selectIds)
  selectIds = []
}

const s = useSlots()

defineExpose({
  scrollToBottom,
  scrollToItem,
  closeImportDialog,
  getData,
})

let loading2 = $ref(false)

let params = $ref({
  pageNo: 1,
  pageSize: 50,
  total: 0,
  list: [],
  sortType: null,
  searchKey: '',
})

function search(key: string) {
  if (!params.searchKey) {
    params.pageNo = 1
  }
  params.searchKey = key
  getData()
}

function cancelSearch() {
  params.searchKey = ''
  showSearchInput = false
  getData()
}

async function getData() {
  if (props.request) {
    loading2 = true
    let { list, total } = await props.request(params)
    params.list = list
    params.total = total
    loading2 = false
  } else {
    params.list = props.list ?? []
  }
}

function handlePageNo(e) {
  params.pageNo = e
  getData()
  scrollToTop()
}

onMounted(async () => {
  getData()
})

defineRender(() => {
  const d = item => (
    <Checkbox modelValue={selectIds.includes(item.id)} onChange={() => toggleSelect(item)} size="large" />
  )

  return (
    <div class="flex flex-col gap-3">
      {props.showToolbar && (
        <div>
          {showSearchInput ? (
            <div class="flex gap-4">
              <BaseInput
                clearable
                modelValue={params.searchKey}
                onUpdate:modelValue={debounce(e => search(e), 500)}
                class="flex-1"
                autofocus
              >
                {{
                  subfix: () => <IconFluentSearch24Regular class="text-lg text-gray" />,
                }}
              </BaseInput>
              <BaseButton onClick={cancelSearch}>取消</BaseButton>
            </div>
          ) : (
            <div class="flex justify-between items-center">
              {showCheckbox ? (
                <div class="flex gap-2 items-center">
                  <Checkbox
                    disabled={!params.list.length}
                    onChange={() => toggleSelectAll()}
                    modelValue={selectAll}
                    size="large"
                  />
                  <span>
                  {selectIds.length} / {params.total}
                </span>
                </div>
              ) : <div>{params.total}条</div>}


              <div class="flex gap-2 relative">
                {selectIds.length && showCheckbox ? (
                  <PopConfirm title="确认删除所有选中数据？" onConfirm={handleBatchDel}>
                    <BaseButton type="info">确认</BaseButton>
                  </PopConfirm>
                ) : null}

                <BaseIcon onClick={() => (showCheckbox = !showCheckbox)} title="批量删除">
                  <DeleteIcon />
                </BaseIcon>

                <BaseIcon onClick={() => (showImportDialog = true)} title="导入">
                  <IconSystemUiconsImport />
                </BaseIcon>
                <BaseIcon onClick={() => emit('export')} title="导出">
                  {props.exportLoading ? <IconEosIconsLoading /> : <IconPhExportLight />}
                </BaseIcon>
                <BaseIcon onClick={() => emit('add')} title="添加单词">
                  <IconFluentAdd20Regular />
                </BaseIcon>
                <BaseIcon
                  disabled={!params.list.length}
                  title="改变顺序"
                  onClick={() => (showSortDialog = !showSortDialog)}
                >
                  <IconFluentArrowSort20Regular />
                </BaseIcon>
                <BaseIcon
                  disabled={!params.list.length}
                  onClick={() => (showSearchInput = !showSearchInput)}
                  title="搜索"
                >
                  <IconFluentSearch20Regular />
                </BaseIcon>
                <MiniDialog
                  modelValue={showSortDialog}
                  onUpdate:modelValue={e => (showSortDialog = e)}
                  style="width: 8rem;"
                >
                  <div class="mini-row-title">列表顺序设置</div>
                  <div class="flex flex-col gap2 btn-no-margin">
                    <BaseButton onClick={() => sort(Sort.reverse)}>翻转当前页</BaseButton>
                    <BaseButton onClick={() => sort(Sort.reverseAll)}>翻转所有</BaseButton>
                    <div class="line"></div>
                    <BaseButton onClick={() => sort(Sort.random)}>随机当前页</BaseButton>
                    <BaseButton onClick={() => sort(Sort.randomAll)}>随机所有</BaseButton>
                  </div>
                </MiniDialog>
              </div>
            </div>
          )}
        </div>
      )}

      <div class="relative flex-1 overflow-hidden">
        {params.list.length ? (
          <div class="overflow-auto h-full" ref={e => (listRef = e)}>
            {params.list.map((item, index) => {
              return (
                <div class="list-item-wrapper" key={item.word}>
                  {s.default({
                    checkbox: showCheckbox ? d : () => void 0,
                    item,
                    index: params.pageSize * (params.pageNo - 1) + index + 1,
                  })}
                </div>
              )
            })}
          </div>
        ) : !loading2 ? (
          <Empty />
        ) : null}
        {loading2 && (
          <div class="absolute top-0 left-0 bottom-0 right-0 bg-black bg-op-10  center text-4xl">
            <IconEosIconsLoading color="gray" />
          </div>
        )}
      </div>

      {props.showPagination && (
        <div class="flex justify-end">
          <Pagination
            currentPage={params.pageNo}
            onUpdate:current-page={handlePageNo}
            pageSize={params.pageSize}
            onUpdate:page-size={e => (params.pageSize = e)}
            pageSizes={[20, 50, 100, 200]}
            layout="total,sizes"
            total={params.total}
          />
        </div>
      )}

      <Dialog modelValue={showImportDialog} onUpdate:modelValue={closeImportDialog} title="导入教程">
        <div className="w-100 p-4 pt-0">
          <div>请按照模板的格式来填写数据</div>
          <div class="color-red">单词项为必填，其他项可不填</div>
          <div>翻译：一行一个翻译，前面词性，后面内容（如n.取消）；多个翻译请换行</div>
          <div>
            例句：一行原文，一行译文；多个请换<span class="color-red">两</span>行
          </div>
          <div>
            短语：一行原文，一行译文；多个请换<span class="color-red">两</span>行
          </div>
          <div>同义词、同根词、词源：请前往官方词典，然后编辑其中某个单词，参考其格式</div>
          <div class="mt-6">
            模板下载地址：<a href={`https://${Host}/libs/单词导入模板.xlsx`}>单词导入模板</a>
          </div>
          <div class="mt-4">
            <BaseButton
              onClick={() => {
                let d: HTMLDivElement = document.querySelector('#upload-trigger')
                d.click()
              }}
              loading={props.importLoading}
            >
              导入
            </BaseButton>
            <input
              id="upload-trigger"
              type="file"
              accept=".csv, application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, application/vnd.ms-excel"
              onChange={e => emit('import', e)}
              class="w-0 h-0 opacity-0"
            />
          </div>
        </div>
      </Dialog>
    </div>
  )
})
</script>
<style scoped lang="scss"></style>
