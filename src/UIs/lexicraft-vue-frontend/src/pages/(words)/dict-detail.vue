<script lang="tsx" setup>
import {detail} from '@/apis'
import BackIcon from '@/components/BackIcon.vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseIcon from '@/components/BaseIcon.vue'
import BasePage from '@/components/BasePage.vue'
import BaseTable from '@/components/BaseTable.vue'
import PopConfirm from '@/components/PopConfirm.vue'
import WordItem from '@/components/WordItem.vue'
import BaseInput from '@/components/base/BaseInput.vue'
import Textarea from '@/components/base/Textarea.vue'
import Form from '@/components/base/form/Form.vue'
import FormItem from '@/components/base/form/FormItem.vue'
import Toast from '@/components/base/toast/Toast.ts'
import DeleteIcon from '@/components/icon/DeleteIcon.vue'
import {AppEnv, DictId, LIB_JS_URL, TourConfig} from '@/config/env.ts'
import {getCurrentStudyWord} from '@/hooks/dict.ts'
import EditBook from '@/components/article/components/EditBook.vue'
import PracticeSettingDialog from '@/components/word/components/PracticeSettingDialog.vue'
import {useBaseStore} from '@/stores/base.ts'
import {useRuntimeStore} from '@/stores/runtime.ts'
import {useSettingStore} from '@/stores/setting.ts'
import {getDefaultDict} from '@/types/func.ts'
import {_getDictDataByUrl, _nextTick, convertToWord, isMobile, loadJsLib, reverse, shuffle, useNav} from '@/utils'
import {MessageBox} from '@/utils/MessageBox.tsx'
import {nanoid} from 'nanoid'
import {computed, onMounted, onUnmounted, reactive, ref, watch} from 'vue'
import {useRoute, useRouter} from 'vue-router'
import {wordDelete} from '@/apis/words.ts'
import {copyOfficialDict} from '@/apis/dict.ts'
import {PRACTICE_WORD_CACHE} from '@/utils/cache.ts'
import {Sort} from '@/types/enum.ts'

const runtimeStore = useRuntimeStore()
const base = useBaseStore()
const router = useRouter()
const route = useRoute()
const isMob = isMobile()
let loading = $ref(false)
let allList = $ref([])

const getDefaultFormWord = () => {
  return {
    id: '',
    word: '',
    phonetic0: '',
    phonetic1: '',
    trans: '',
    sentences: '',
    phrases: '',
    synos: '',
    relWords: '',
    etymology: '',
  }
}
let isOperate = $ref(false)
let wordForm = $ref(getDefaultFormWord())
let wordFormRef = $ref<any>()
const wordRules: any = reactive({
  word: [
    {required: true, message: '请输入单词', trigger: 'blur'},
    {max: 100, message: '名称不能超过100个字符', trigger: 'blur'},
  ],
})
let studyLoading = $ref(false)

function syncDictInMyStudyList(study = false) {
  _nextTick(() => {
    //这里不能移，一定要先找到对应的词典，再去改id。不然先改id，就找不到对应的词典了
    let rIndex = base.word.bookList.findIndex(v => v.id === runtimeStore.editDict.id)

    runtimeStore.editDict.words = allList
    let temp = runtimeStore.editDict
    if (!temp.custom && ![DictId.wordKnown, DictId.wordWrong, DictId.wordCollect].includes(temp.id)) {
      temp.custom = true
      if (!temp.id.includes('_custom')) {
        temp.id += '_custom_' + nanoid(6)
      }
    }
    temp.length = temp.words.length
    if (rIndex > -1) {
      base.word.bookList[rIndex] = getDefaultDict(temp)
      if (study) base.word.studyIndex = rIndex
    } else {
      base.word.bookList.push(getDefaultDict(temp))
      if (study) base.word.studyIndex = base.word.bookList.length - 1
    }
  }, 100)
}

async function onSubmitWord() {
  // return console.log('wordFormRef',wordFormRef,wordFormRef.validate)
  await wordFormRef.validate(valid => {
    if (valid) {
      let data: any = convertToWord(wordForm)
      //todo 可以检查的更准确些，比如json对比
      if (data.id) {
        let r = allList.find(v => v.id === data.id)
        if (r) {
          Object.assign(r, data)
          Toast.success('修改成功')
        } else {
          Toast.success('修改失败，未找到单词')
          return
        }
      } else {
        data.id = nanoid(6)
        data.checked = false
        let r = allList.find(v => v.word === wordForm.word)
        if (r) {
          Toast.warning('已有相同名称单词！')
          return
        } else allList.push(data)
        Toast.success('添加成功')
        wordForm = getDefaultFormWord()
      }
      syncDictInMyStudyList()
    } else {
      Toast.warning('请填写完整')
    }
  })
}

async function batchDel(ids: string[]) {
  let localHandle = () => {
    ids.map(id => {
      let rIndex2 = allList.findIndex(v => v.id === id)
      if (rIndex2 > -1) {
        if (id === wordForm.id) {
          wordForm = getDefaultFormWord()
        }
        allList.splice(rIndex2, 1)
      }
    })
    tableRef.value.getData()
    syncDictInMyStudyList()
  }

  let cloudHandle = async dictId => {
    let res = await wordDelete(null, {
      wordIds: ids,
      dictId,
    })
    if (res.success) {
      tableRef.value.getData()
    } else {
      return Toast.error(res.msg ?? '删除失败')
    }
  }

  if (AppEnv.CAN_REQUEST) {
    if (dict.custom) {
      if (dict.sync) {
        await cloudHandle(dict.id)
      } else {
        localHandle()
      }
    } else {
      let r = await copyOfficialDict(null, {id: dict.id})
      if (r.success) {
        await cloudHandle(r.data.id)
        getDetail(r.data.id)
      } else {
        //todo 权限判断，能否复制
        return Toast.error(r.msg)
      }
    }
  } else {
    localHandle()
  }
}

//把word对象的字段全转成字符串
function word2Str(word) {
  let res = getDefaultFormWord()
  res.id = word.id
  res.word = word.word
  res.phonetic1 = word.phonetic1
  res.phonetic0 = word.phonetic0
  res.trans = word.trans.map(v => (v.pos + v.cn).replaceAll('"', '')).join('\n')
  res.sentences = word.sentences.map(v => (v.c + '\n' + v.cn).replaceAll('"', '')).join('\n\n')
  res.phrases = word.phrases.map(v => (v.c + '\n' + v.cn).replaceAll('"', '')).join('\n\n')
  res.synos = word.synos.map(v => (v.pos + v.cn + '\n' + v.ws.join('/')).replaceAll('"', '')).join('\n\n')
  res.relWords = word.relWords.root
      ? '词根:' +
      word.relWords.root +
      '\n\n' +
      word.relWords.rels
          .map(v => (v.pos + '\n' + v.words.map(v => v.c + ':' + v.cn).join('\n')).replaceAll('"', ''))
          .join('\n\n')
      : ''
  res.etymology = word.etymology.map(v => (v.t + '\n' + v.d).replaceAll('"', '')).join('\n\n')
  return res
}

function editWord(word) {
  isOperate = true
  wordForm = word2Str(word)
  if (isMob) activeTab = 'edit'
}

function addWord() {
  // setTimeout(wordListRef?.scrollToBottom, 100)
  isOperate = true
  wordForm = getDefaultFormWord()
  if (isMob) activeTab = 'edit'
}

function closeWordForm() {
  isOperate = false
  wordForm = getDefaultFormWord()
  if (isMob) activeTab = 'list'
}

let isEdit = $ref(false)
let isAdd = $ref(false)
let activeTab = $ref<'list' | 'edit'>('list') // 移动端标签页状态

const showBookDetail = computed(() => {
  return !(isAdd || isEdit)
})

onMounted(async () => {
  if (route.query?.isAdd) {
    isAdd = true
    runtimeStore.editDict = getDefaultDict()
  } else {
    if (!runtimeStore.editDict.id) {
      return router.push('/word')
    } else {
      if (
          !runtimeStore.editDict.words.length &&
          !runtimeStore.editDict.custom &&
          ![DictId.wordCollect, DictId.wordWrong, DictId.wordKnown].includes(
              runtimeStore.editDict.en_name || runtimeStore.editDict.id
          )
      ) {
        loading = true
        let r = await _getDictDataByUrl(runtimeStore.editDict)
        runtimeStore.editDict = r
      }
      if (base.word.bookList.find(book => book.id === runtimeStore.editDict.id)) {
        if (AppEnv.CAN_REQUEST) {
          getDetail(runtimeStore.editDict.id)
        }
      }
      loading = false
    }
  }

  allList = runtimeStore.editDict.words
  tableRef.value.getData()

  runtimeStore.pageTitle = runtimeStore.editDict.name
})

watch(() => runtimeStore.editDict.name, (val) => {
  if (val) runtimeStore.pageTitle = val
})

onUnmounted(() => {
  runtimeStore.pageTitle = ''
})

async function getDetail(id) {
  //todo 优化：这里只返回详情
  let res = await detail({id})
  if (res.success) {
    runtimeStore.editDict = res.data
  }
}

function formClose() {
  if (isEdit) isEdit = false
  else router.back()
}

let showPracticeSettingDialog = $ref(false)

const store = useBaseStore()
const settingStore = useSettingStore()
const {nav} = useNav()

//todo 可以和首页合并
async function startPractice(query = {}) {
  localStorage.removeItem(PRACTICE_WORD_CACHE.key)
  studyLoading = true
  await base.changeDict(runtimeStore.editDict)
  studyLoading = false
  window.umami?.track('startStudyWord', {
    name: store.sdict.name,
    index: store.sdict.lastLearnIndex,
    perDayStudyNumber: store.sdict.perDayStudyNumber,
    custom: store.sdict.custom,
    complete: store.sdict.complete,
    wordPracticeMode: settingStore.wordPracticeMode,
  })
  let currentStudy = getCurrentStudyWord()
  if (!currentStudy.new.length && !currentStudy.review.length && !currentStudy.write.length && !currentStudy.shuffle.length) {
    studyLoading = false
    return Toast.warning('没有可学习的单词！')
  }
  nav('practice-words/' + store.sdict.id, query, {taskWords: currentStudy})
}

async function addMyStudyList() {
  if (!runtimeStore.editDict.words.length) {
    return Toast.warning('没有单词可学习！')
  }
  if (!settingStore.disableShowPracticeSettingDialog) {
    showPracticeSettingDialog = true
    return
  }
  startPractice()
}

async function startTest() {
  loading = true
  await base.changeDict(runtimeStore.editDict)
  loading = false
  nav('word-test/' + store.sdict.id)
}

let exportLoading = $ref(false)
let importLoading = $ref(false)
let tableRef = ref()

function importData(e) {
  let file = e.target.files[0]
  if (!file) return

  let reader = new FileReader()
  reader.onload = async function (s) {
    let data = s.target.result
    importLoading = true
    const XLSX = await loadJsLib('XLSX', LIB_JS_URL.XLSX)
    let workbook = XLSX.read(data, {type: 'binary'})
    let res: any[] = XLSX.utils.sheet_to_json(workbook.Sheets['Sheet1'])
    if (res.length) {
      let words = res
          .map(v => {
            if (v['单词']) {
              let data = null
              try {
                data = convertToWord({
                  id: nanoid(6),
                  word: v['单词'],
                  phonetic0: v['音标①'] ?? '',
                  phonetic1: v['音标②'] ?? '',
                  trans: v['翻译'] ?? '',
                  sentences: v['例句'] ?? '',
                  phrases: v['短语'] ?? '',
                  synos: v['近义词'] ?? '',
                  relWords: v['同根词'] ?? '',
                  etymology: v['词源'] ?? '',
                })
              } catch (e) {
                console.error('导入单词报错' + v['单词'], e.message)
              }
              return data
            }
          })
          .filter(v => v)
      if (words.length) {
        let repeat = []
        let noRepeat = []
        words.map((v: any) => {
          let rIndex = runtimeStore.editDict.words.findIndex(s => s.word === v.word)
          if (rIndex > -1) {
            v.index = rIndex
            repeat.push(v)
          } else {
            noRepeat.push(v)
          }
        })

        runtimeStore.editDict.words = runtimeStore.editDict.words.concat(noRepeat)

        if (repeat.length) {
          MessageBox.confirm(
              '单词"' + repeat.map(v => v.word).join(', ') + '" 已存在，是否覆盖原单词？',
              '检测到重复单词',
              () => {
                repeat.map(v => {
                  runtimeStore.editDict.words[v.index] = v
                  delete runtimeStore.editDict.words[v.index]['index']
                })
              },
              null,
              () => {
                tableRef.value.closeImportDialog()
                e.target.value = ''
                importLoading = false
                allList = runtimeStore.editDict.words
                tableRef.value.getData()
                syncDictInMyStudyList()
                Toast.success('导入成功！')
              }
          )
        } else {
          tableRef.value.closeImportDialog()
          e.target.value = ''
          importLoading = false
          allList = runtimeStore.editDict.words
          tableRef.value.getData()
          syncDictInMyStudyList()
          Toast.success('导入成功！')
        }
      } else {
        Toast.warning('导入失败！原因：没有数据/未认别到数据')
      }
    } else {
      Toast.warning('导入失败！原因：没有数据')
    }
    e.target.value = ''
    importLoading = false
  }
  reader.readAsBinaryString(file)
}

async function exportData() {
  exportLoading = true
  const XLSX = await loadJsLib('XLSX', LIB_JS_URL.XLSX)
  let list = runtimeStore.editDict.words
  let filename = runtimeStore.editDict.name
  let wb = XLSX.utils.book_new()
  let sheetData = list.map(v => {
    let t = word2Str(v)
    return {
      单词: t.word,
      '音标①': t.phonetic0,
      '音标②': t.phonetic1,
      翻译: t.trans,
      例句: t.sentences,
      短语: t.phrases,
      近义词: t.synos,
      同根词: t.relWords,
      词源: t.etymology,
    }
  })
  wb.Sheets['Sheet1'] = XLSX.utils.json_to_sheet(sheetData)
  wb.SheetNames = ['Sheet1']
  XLSX.writeFile(wb, `${filename}.xlsx`)
  Toast.success(filename + ' 导出成功！')
  exportLoading = false
}

watch(
    () => loading,
    val => {
      if (!val) return
      _nextTick(async () => {
        const Shepherd = await loadJsLib('Shepherd', LIB_JS_URL.SHEPHERD)
        const tour = new Shepherd.Tour(TourConfig)
        tour.on('cancel', () => {
          localStorage.setItem('tour-guide', '1')
        })
        tour.addStep({
          id: 'step3',
          text: '点击这里开始学习',
          attachTo: {element: '#study', on: 'bottom'},
          buttons: [
            {
              text: `下一步（3/${TourConfig.total}）`,
              action() {
                tour.next()
                addMyStudyList()
              },
            },
          ],
        })

        tour.addStep({
          id: 'step4',
          text: '这里可以选择学习模式、设置学习数量、修改学习进度',
          attachTo: {element: '#mode', on: 'bottom'},
          beforeShowPromise() {
            return new Promise(resolve => {
              const timer = setInterval(() => {
                if (document.querySelector('#mode')) {
                  clearInterval(timer)
                  setTimeout(resolve, 500)
                }
              }, 100)
            })
          },
          buttons: [
            {
              text: `下一步（4/${TourConfig.total}）`,
              action() {
                tour.next()
                startPractice({guide: 1})
              },
            },
          ],
        })

        const r = localStorage.getItem('tour-guide')
        if (settingStore.first && !r && !isMobile()) {
          tour.start()
        }
      }, 500)
    }
)

const dict = $computed(() => runtimeStore.editDict)

//获取本地单词列表
function getLocalList({pageNo, pageSize, searchKey}) {
  let list = allList
  let total = allList.length
  if (searchKey.trim()) {
    list = allList.filter(v => v.word.toLowerCase().includes(searchKey.trim().toLowerCase()))
    total = list.length
  }
  list = list.slice((pageNo - 1) * pageSize, (pageNo - 1) * pageSize + pageSize)
  return {list, total}
}

async function requestList({pageNo, pageSize, searchKey}) {
  if (!dict.custom && ![DictId.wordCollect, DictId.wordWrong, DictId.wordKnown].includes(dict.en_name || dict.id)) {
    // 非自定义词典，直接请求json

    //如果没数据则请求
    if (!allList.length) {
      let r = await _getDictDataByUrl(dict)
      allList = r.words
    }
    return getLocalList({pageNo, pageSize, searchKey})
  } else {
    // 自定义词典

    //如果登录了,则请求后端数据
    if (AppEnv.CAN_REQUEST) {
      //todo 加上sync标记
      if (dict.sync || true) {
        //todo 优化：这里应该只返回列表
        let res = await detail({id: dict.id, pageNo, pageSize})
        if (res.success) {
          return {list: res.data.words, total: res.data.length}
        }
        return {list: [], total: 0}
      }
    } else {
      //未登录则用本地保存的数据
      allList = dict.words
    }
    return getLocalList({pageNo, pageSize, searchKey})
  }
}

function onSort(type: Sort, pageNo: number, pageSize: number) {
  if (AppEnv.CAN_REQUEST) {
  } else {
    let fun = reverse
    if ([Sort.reverse, Sort.reverseAll].includes(type)) {
      fun = reverse
    } else if ([Sort.random, Sort.randomAll].includes(type)) {
      fun = shuffle
    }
    allList = allList
        .slice(0, pageSize * (pageNo - 1))
        .concat(fun(allList.slice(pageSize * (pageNo - 1), pageSize * (pageNo - 1) + pageSize)))
        .concat(allList.slice(pageSize * (pageNo - 1) + pageSize))
    runtimeStore.editDict.words = allList
    Toast.success('操作成功')
    tableRef.value.getData()
    syncDictInMyStudyList()
  }
}

defineRender(() => {
  return (
      <BasePage>
        {showBookDetail.value ? (

            <div class="card mb-0 dict-detail-card flex flex-col p-0! overflow-hidden">
              {/* Enhanced Premium Header */}
              <div class="content-header relative overflow-hidden p-8 pb-12">
                <div class="absolute inset-0 bg-gradient-to-br from-slate-900 via-indigo-950 to-slate-900 z-0"></div>
                <div class="absolute -right-20 -top-20 w-80 h-80 bg-blue-500/10 rounded-full blur-3xl z-1 animate-pulse"></div>
                <div class="absolute -left-20 -bottom-20 w-64 h-64 bg-indigo-500/10 rounded-full blur-3xl z-1"></div>
                
                <div class="header-content relative z-10 flex flex-col lg:flex-row justify-between items-start lg:items-center gap-8">
                  <div class="left-section flex items-center gap-6">
                    <BackIcon class="back-btn !text-white/80 hover:!text-white transition-all transform hover:-translate-x-1"/>
                    <div class="info-box space-y-2">
                      <div class="title-row flex items-center gap-3">
                        <h1 class="dict-title text-4xl font-black text-white m-0 tracking-tight">{runtimeStore.editDict.name}</h1>
                        {runtimeStore.editDict.custom && <span class="bg-amber-400 text-amber-950 font-black text-[10px] px-2 py-0.5 rounded-md uppercase tracking-widest shadow-lg shadow-amber-400/20">Custom</span>}
                      </div>
                      <div class="meta-row flex gap-4 text-white/60 font-bold text-sm">
                        <span class="meta-item flex items-center gap-1.5 hover:text-white/80 transition-colors">
                          <BaseIcon class="text-blue-400"><IconFluentBookNumber20Regular/></BaseIcon>
                          {runtimeStore.editDict.length || 0} WORDS
                        </span>
                        <span class="meta-item flex items-center gap-1.5 hover:text-white/80 transition-colors">
                          <BaseIcon class="text-emerald-400"><IconFluentTargetArrow20Regular/></BaseIcon>
                          {runtimeStore.editDict.lastLearnIndex || 0} LEARNED
                        </span>
                      </div>
                      {runtimeStore.editDict.description && <div class="description text-white/50 text-sm max-w-xl line-clamp-2 mt-2 font-medium italic" title={runtimeStore.editDict.description}>{runtimeStore.editDict.description}</div>}
                    </div>
                  </div>

                  <div class="right-section flex flex-col gap-4">
                    <div class="action-group flex gap-3">
                      <BaseButton 
                        loading={studyLoading || loading} 
                        class="!h-14 !px-8 !rounded-2xl !bg-white !text-slate-900 !border-none font-black text-lg shadow-2xl transition-all hover:scale-[1.05] active:scale-[0.95]"
                        onClick={addMyStudyList}
                        icon={<IconFluentHatGraduation24Filled class="text-blue-600"/>}
                      >
                        开始学习
                      </BaseButton>
                      <BaseButton 
                        loading={studyLoading || loading} 
                        class="!h-14 !px-8 !rounded-2xl !bg-white/10 !text-white !border-white/20 backdrop-blur-md font-black text-lg hover:!bg-white/20 transition-all hover:scale-[1.05] active:scale-[0.95]"
                        onClick={startTest}
                        icon={<IconFluentQuizNew20Regular/>}
                      >
                        测试
                      </BaseButton>
                    </div>
                    <div class="flex justify-end">
                      <BaseButton 
                        loading={studyLoading || loading} 
                        class="!text-white/60 hover:!text-white font-bold transition-colors !bg-transparent border-none"
                        onClick={() => (isEdit = true)}
                        icon={<IconFluentList24Regular class="mr-1.5"/>}
                      >
                        编辑详情
                      </BaseButton>
                    </div>
                  </div>
                </div>
              </div>

              {/* 移动端标签页导航 */}
              {isMob && isOperate && (
                  <div class="tab-navigation mb-3 mx-4">
                    <div class={`tab-item ${activeTab === 'list' ? 'active' : ''}`}
                         onClick={() => (activeTab = 'list')}>
                      单词列表
                    </div>
                    <div class={`tab-item ${activeTab === 'edit' ? 'active' : ''}`}
                         onClick={() => (activeTab = 'edit')}>
                      {wordForm.id ? '编辑' : '添加'}单词
                    </div>
                  </div>
              )}

              <div class="flex flex-1 overflow-hidden content-area bg-slate-50/50 dark:bg-slate-900/50">
                <div class={`word-list-section flex-1 h-full p-6 ${isMob && isOperate && activeTab !== 'list' ? 'mobile-hidden' : ''}`}>
                  <div class="card-white h-full flex flex-col p-0! overflow-hidden border-none shadow-xl shadow-slate-200/50 dark:shadow-none">
                    <BaseTable
                        ref={tableRef}
                        class="h-full"
                        request={requestList}
                        onDel={batchDel}
                        onSort={onSort}
                        onAdd={addWord}
                        onImport={importData}
                        onExport={exportData}
                        exportLoading={exportLoading}
                        importLoading={importLoading}
                    >
                    {val => (
                        <WordItem
                            showTransPop={false}
                            onClick={() => editWord(val.item)}
                            index={val.index}
                            showCollectIcon={false}
                            showMarkIcon={false}
                            item={val.item}
                        >
                          {{
                            prefix: () => val.checkbox(val.item),
                            suffix: () => (
                                <div class="flex flex-col">
                                  <BaseIcon class="option-icon" onClick={() => editWord(val.item)} title="编辑">
                                    <IconFluentTextEditStyle20Regular/>
                                  </BaseIcon>
                                  <PopConfirm title="确认删除？" onConfirm={() => batchDel([val.item.id])}>
                                    <BaseIcon class="option-icon" title="删除">
                                      <DeleteIcon/>
                                    </BaseIcon>
                                  </PopConfirm>
                                </div>
                            ),
                          }}
                        </WordItem>
                    )}
                  </BaseTable>
                </div></div>
                {isOperate ? (
                    <div
                        class={`edit-section flex-1 flex flex-col ${isMob && activeTab !== 'edit' ? 'mobile-hidden' : ''}`}>
                      <div class="common-title">{wordForm.id ? '修改' : '添加'}单词</div>
                      <Form
                          class="flex-1 overflow-auto pr-2"
                          ref={e => (wordFormRef = e)}
                          rules={wordRules}
                          model={wordForm}
                          label-width="7rem"
                      >
                        <FormItem label="单词" prop="word">
                          <BaseInput modelValue={wordForm.word}
                                     onUpdate:modelValue={e => (wordForm.word = e)}></BaseInput>
                        </FormItem>
                        <FormItem label="英音音标">
                          <BaseInput modelValue={wordForm.phonetic0}
                                     onUpdate:modelValue={e => (wordForm.phonetic0 = e)}/>
                        </FormItem>
                        <FormItem label="美音音标">
                          <BaseInput modelValue={wordForm.phonetic1}
                                     onUpdate:modelValue={e => (wordForm.phonetic1 = e)}/>
                        </FormItem>
                        <FormItem label="翻译">
                          <Textarea
                              modelValue={wordForm.trans}
                              onUpdate:modelValue={e => (wordForm.trans = e)}
                              placeholder="一行一个翻译，前面词性，后面内容（如n.取消）；多个翻译请换行"
                              autosize={{minRows: 6, maxRows: 10}}
                          />
                        </FormItem>
                        <FormItem label="例句">
                          <Textarea
                              modelValue={wordForm.sentences}
                              onUpdate:modelValue={e => (wordForm.sentences = e)}
                              placeholder="一行原文，一行译文；多个请换两行"
                              autosize={{minRows: 6, maxRows: 10}}
                          />
                        </FormItem>
                        <FormItem label="短语">
                          <Textarea
                              modelValue={wordForm.phrases}
                              onUpdate:modelValue={e => (wordForm.phrases = e)}
                              placeholder="一行原文，一行译文；多个请换两行"
                              autosize={{minRows: 6, maxRows: 10}}
                          />
                        </FormItem>
                        <FormItem label="同义词">
                          <Textarea
                              modelValue={wordForm.synos}
                              onUpdate:modelValue={e => (wordForm.synos = e)}
                              placeholder="请参考已有单词格式"
                              autosize={{minRows: 6, maxRows: 20}}
                          />
                        </FormItem>
                        <FormItem label="同根词">
                          <Textarea
                              modelValue={wordForm.relWords}
                              onUpdate:modelValue={e => (wordForm.relWords = e)}
                              placeholder="请参考已有单词格式"
                              autosize={{minRows: 6, maxRows: 20}}
                          />
                        </FormItem>
                        <FormItem label="词源">
                          <Textarea
                              modelValue={wordForm.etymology}
                              onUpdate:modelValue={e => (wordForm.etymology = e)}
                              placeholder="请参考已有单词格式"
                              autosize={{minRows: 6, maxRows: 10}}
                          />
                        </FormItem>
                      </Form>
                      <div class="center gap-4 mt-8">
                        <BaseButton 
                          class="!h-12 !px-8 !rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-600 font-bold hover:bg-slate-200 transition-all" 
                          onClick={closeWordForm}
                        >
                          关闭
                        </BaseButton>
                        <BaseButton 
                          class="!h-12 !px-10 !rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 text-white font-black shadow-lg shadow-blue-500/20 hover:scale-[1.02] active:scale-[0.98] transition-all" 
                          onClick={onSubmitWord}
                        >
                          保存单词
                        </BaseButton>
                      </div>
                    </div>
                ) : null}
              </div>
            </div>
        ) : (
            <div class="card mb-0 dict-detail-card flex flex-col justify-center items-center bg-slate-50 dark:bg-slate-950 p-12">
              <div class="w-full max-w-2xl">
                <EditBook isAdd={isAdd} isBook={false} onClose={formClose} onSubmit={() => (isEdit = isAdd = false)}/>
              </div>
            </div>
        )}

        <PracticeSettingDialog
            showLeftOption
            modelValue={showPracticeSettingDialog}
            onUpdate:modelValue={val => (showPracticeSettingDialog = val)}
            onOk={startPractice}
        />
      </BasePage>
    )
})
</script>

<style lang="scss" scoped>
.dict-detail-card {
  height: calc(100vh - 3rem);
  display: flex;
  flex-direction: column;
}

/* Rich Content Header Styles */
.content-header {
  position: relative;
  background: linear-gradient(to bottom, var(--color-second), var(--color-primary));
  padding: 1.5rem 2rem;
  border-bottom: 1px solid var(--border-color);
  margin-bottom: 1rem;
  flex-shrink: 0;
}

.content-area {
  display: flex;
  flex: 1;
  overflow: hidden;
}

.word-list-section {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.edit-section {
  width: 44%;
  padding: 0 2rem;
  overflow-y: auto;
  border-left: 1px solid var(--border-color);

  .common-title {
    font-size: 1.2rem;
    font-weight: 600;
    color: var(--text-primary);
    margin-bottom: 1.5rem;
    padding-bottom: 1rem;
    border-bottom: 2px solid var(--border-color);
  }
}

.mobile-hidden {
  display: none !important;
}

// ... existing styles ...

.secondary-actions {
  .base-button {
    padding: 0;
    font-size: 0.9rem;
    color: var(--text-primary);
    font-weight: 500;

    &:hover {
      color: var(--color-link);
      text-decoration: underline;
    }
  }
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  position: relative;
  z-index: 2;
  gap: 2rem;
}

.left-section {
  display: flex;
  gap: 1.5rem;
  align-items: flex-start;
  flex: 1;
}

.back-btn {
  margin-top: 0.25rem;
}

.cover-box {
  width: 100px;
  height: 100px;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  flex-shrink: 0;
  background: #e2e8f0;

  .cover-img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  .cover-placeholder {
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, #e0e7ff 0%, #c7d2fe 100%);
    color: #4f46e5;
    font-size: 2.5rem;
    font-weight: 700;
  }
}

.info-box {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;

  .title-row {
    display: flex;
    align-items: center;
    gap: 0.8rem;

    .dict-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0;
    }

    .badge {
      font-size: 0.75rem;
      padding: 2px 8px;
      border-radius: 4px;
      background: var(--bg-tertiary);
      color: var(--text-secondary);

      &.custom {
        background: #f3e8ff;
        color: #9333ea;
      }
    }
  }

  .meta-row {
    display: flex;
    gap: 1.5rem;
    color: var(--text-tertiary);
    font-size: 0.9rem;

    .meta-item {
      display: flex;
      align-items: center;
      gap: 0.4rem;
    }
  }

  .description {
    font-size: 0.9rem;
    color: var(--text-secondary);
    line-height: 1.5;
    max-width: 600px;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }
}

.right-section {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.8rem;

  .action-group {
    display: flex;
    gap: 0.8rem;
  }

  .secondary-actions {
    .base-button {
      padding: 0;
      font-size: 0.9rem;
      color: var(--text-primary);
      font-weight: 500;

      &:hover {
        color: var(--color-link);
        text-decoration: underline;
      }
    }
  }
}

.word-list-section {
  width: 44%;
  padding-left: 2rem;
}

/* Mobile Adaptation */
@media (max-width: 768px) {
  .dict-detail-card {
    padding: 0 !important;
  }

  .content-header {
    padding: 1rem;
    background: var(--header-bg);
    margin-bottom: 0;
  }

  .header-content {
    flex-direction: column;
    gap: 1rem;
    align-items: stretch;
  }

  .left-section {
    width: 100%;
    gap: 1rem;
  }

  .cover-box {
    width: 70px;
    height: 70px;
    border-radius: 8px;
  }

  .info-box {
    .title-row {
      .dict-title {
        font-size: 1.25rem;
      }

      .badge {
        font-size: 0.7rem;
        padding: 2px 6px;
      }
    }

    .meta-row {
      font-size: 0.85rem;
      gap: 1rem;
    }

    .description {
      font-size: 0.85rem;
      -webkit-line-clamp: 3;
      line-clamp: 3;
    }
  }

  .right-section {
    width: 100%;
    align-items: stretch;
    gap: 0.75rem;

    .action-group {
      width: 100%;
      gap: 0.5rem;

      .base-button {
        flex: 1;
        font-size: 0.9rem;
        padding: 0.625rem 1rem;
      }
    }

    .secondary-actions {
      align-self: center;

      .base-button {
        font-size: 0.85rem;
      }
    }
  }

  // 标签页导航
  .tab-navigation {
    display: flex;
    border-bottom: 2px solid var(--border-color);
    margin: 0 1rem 1rem;
    gap: 0;
    position: sticky;
    top: 0;
    background: var(--header-bg);
    z-index: 5;

    .tab-item {
      flex: 1;
      padding: 0.75rem 1rem;
      text-align: center;
      cursor: pointer;
      font-size: 0.95rem;
      font-weight: 500;
      color: var(--text-tertiary);
      border-bottom: 2px solid transparent;
      margin-bottom: -2px;
      transition: all 0.2s ease;
      user-select: none;

      &:active {
        transform: scale(0.98);
        background: var(--hover-bg);
      }

      &.active {
        color: var(--text-active);
        border-bottom-color: var(--text-active);
        font-weight: 600;
      }
    }
  }

  // 内容区域
  .content-area {
    flex-direction: column;
    padding: 0 1rem 1rem;
  }

  .word-list-section {
    width: 100% !important;
    padding: 0 !important;

    &.mobile-hidden {
      display: none;
    }
  }

  .edit-section {
    width: 100% !important;
    margin-left: 0 !important;
    padding: 0 !important;

    &.mobile-hidden {
      display: none;
    }

    .common-title {
      font-size: 1rem;
      margin-bottom: 1rem;
      padding-bottom: 0.75rem;
      border-bottom: 1px solid var(--border-color);
    }

    :deep(.form-item) {
      margin-bottom: 1rem;

      .form-item-label {
        font-size: 0.875rem;
        margin-bottom: 0.5rem;
      }

      .base-input,
      .textarea {
        font-size: 0.9rem;
      }
    }

    .form-actions {
      position: sticky;
      bottom: 0;
      background: var(--header-bg);
      padding: 1rem 0;
      border-top: 1px solid var(--border-color);
      margin-top: 1rem;
      display: flex;
      gap: 0.75rem;

      .base-button {
        flex: 1;
      }
    }
  }

  // 旧版头部样式（如果使用）
  .dict-header {
    flex-direction: column;
    gap: 1rem;
    padding: 1rem;

    .dict-back {
      align-self: flex-start;
    }

    .dict-title {
      position: static !important;
      width: 100%;
      font-size: 1.25rem;
      text-align: left;
    }

    .dict-actions {
      width: 100%;
      justify-content: center;
      gap: 0.75rem;
      flex-wrap: wrap;

      .base-button {
        flex: 1;
        min-width: calc(50% - 0.375rem);
      }
    }
  }

  // 表格优化
  :deep(.base-table) {
    .table-header {
      padding: 0.75rem;
      flex-wrap: wrap;
      gap: 0.5rem;

      .search-input {
        width: 100%;
        order: -1;
      }

      .table-actions {
        width: 100%;
        justify-content: space-between;

        .base-button {
          font-size: 0.85rem;
          padding: 0.5rem 0.75rem;
        }
      }
    }

    .word-item {
      padding: 0.75rem;

      .word-content {
        font-size: 0.9rem;
      }

      .option-icon {
        font-size: 1.1rem;
      }
    }
  }
}

// 超小屏幕适配
@media (max-width: 480px) {
  .dict-detail-card {
    height: unset;
    min-height: calc(100vh - 1rem);
  }

  .content-header {
    padding: 0.75rem;
  }

  .left-section {
    gap: 0.75rem;
  }

  .cover-box {
    width: 60px;
    height: 60px;

    .cover-placeholder {
      font-size: 2rem;
    }
  }

  .info-box {
    .title-row .dict-title {
      font-size: 1.1rem;
    }

    .meta-row {
      font-size: 0.8rem;
      gap: 0.75rem;
    }
  }

  .right-section {
    .action-group {
      flex-direction: column;

      .base-button {
        width: 100%;
      }
    }
  }

  .tab-navigation {
    margin: 0 0.75rem 0.75rem;

    .tab-item {
      padding: 0.625rem 0.75rem;
      font-size: 0.875rem;
    }
  }

  .content-area {
    padding: 0 0.75rem 0.75rem;
  }

  .edit-section {
    :deep(.form-item) {
      .form-item-label {
        font-size: 0.8rem;
      }
    }
  }
}
</style>
