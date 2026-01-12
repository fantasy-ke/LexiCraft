<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'
import { useSettingStore } from '@/stores/setting'
import { getShortcutKey, useEventListener } from '@/hooks/event'
import { checkAndUpgradeSaveDict, checkAndUpgradeSaveSetting, cloneDeep, loadJsLib, sleep } from '@/utils'
import BaseButton from '@/components/BaseButton.vue'
import { useBaseStore } from '@/stores/base'
import {
  APP_NAME,
  APP_VERSION,
  AppEnv,
  DefaultShortcutKeyMap,
  Host,
  IS_DEV,
  LIB_JS_URL,
  LOCAL_FILE_KEY,
} from '@/config/env'
import BasePage from '@/components/BasePage.vue'
import Toast from '@/components/base/toast/Toast'
import { set } from 'idb-keyval'
import { useRuntimeStore } from '@/stores/runtime'
import { useExport } from '@/hooks/export'
import useTheme from '@/hooks/theme.ts'
import Log from '@/components/setting/Log.vue'
import About from '@/components/About.vue'
import CommonSetting from '@/components/setting/CommonSetting.vue'
import ArticleSetting from '@/components/setting/ArticleSetting.vue'
import WordSetting from '@/components/setting/WordSetting.vue'
import { PRACTICE_ARTICLE_CACHE, PRACTICE_WORD_CACHE } from '@/utils/cache'

const emit = defineEmits<{
  toggleDisabledDialogEscKey: [val: boolean]
}>()

const tabIndex = $ref(0)
const settingStore = useSettingStore()
const runtimeStore = useRuntimeStore()
const store = useBaseStore()

//@ts-ignore
const gitLastCommitHash = ref(LATEST_COMMIT_HASH)

let editShortcutKey = $ref('')

const disabledDefaultKeyboardEvent = $computed(() => {
  return editShortcutKey && tabIndex === 3
})

watch(
  () => disabledDefaultKeyboardEvent,
  v => {
    emit('toggleDisabledDialogEscKey', !!v)
  }
)

// 监听编辑快捷键状态变化，自动聚焦输入框
watch(
  () => editShortcutKey,
  newVal => {
    if (newVal) {
      // 使用nextTick确保DOM已更新
      nextTick(() => {
        focusShortcutInput()
      })
    }
  }
)

useEventListener('keydown', (e: KeyboardEvent) => {
  if (!disabledDefaultKeyboardEvent) return

  // 确保阻止浏览器默认行为
  e.preventDefault()
  e.stopPropagation()

  let shortcutKey = getShortcutKey(e)
  // console.log('e', e, e.keyCode, e.ctrlKey, e.altKey, e.shiftKey)
  // console.log('key', shortcutKey)

  // if (shortcutKey[shortcutKey.length-1] === '+') {
  //   settingStore.shortcutKeyMap[editShortcutKey] = DefaultShortcutKeyMap[editShortcutKey]
  //   return ElMessage.warning('设备失败！')
  // }

  if (editShortcutKey) {
    if (shortcutKey === 'Delete') {
      settingStore.shortcutKeyMap[editShortcutKey] = ''
    } else {
      // 忽略单独的修饰键
      if (
        shortcutKey === 'Ctrl+' ||
        shortcutKey === 'Alt+' ||
        shortcutKey === 'Shift+' ||
        e.key === 'Control' ||
        e.key === 'Alt' ||
        e.key === 'Shift'
      ) {
        return
      }

      for (const [k, v] of Object.entries(settingStore.shortcutKeyMap)) {
        if (v === shortcutKey && k !== editShortcutKey) {
          settingStore.shortcutKeyMap[editShortcutKey] = DefaultShortcutKeyMap[editShortcutKey]
          return Toast.warning('快捷键重复！')
        }
      }
      settingStore.shortcutKeyMap[editShortcutKey] = shortcutKey
    }
  }
})

function handleInputBlur() {
  // 输入框失焦时结束编辑状态
  editShortcutKey = ''
}

function handleBodyClick() {
  if (editShortcutKey) {
    editShortcutKey = ''
  }
}

function focusShortcutInput() {
  // 找到当前正在编辑的快捷键输入框
  const inputElements = document.querySelectorAll('.set-key input')
  if (inputElements && inputElements.length > 0) {
    // 聚焦第一个找到的输入框
    const inputElement = inputElements[0] as HTMLInputElement
    inputElement.focus()
  }
}

// 快捷键中文名称映射
function getShortcutKeyName(key: string): string {
  const shortcutKeyNameMap = {
    ShowWord: '显示单词',
    EditArticle: '编辑文章',
    Next: '下一个',
    Previous: '上一个',
    ToggleSimple: '切换已掌握状态',
    ToggleCollect: '切换收藏状态',
    NextChapter: '下一组',
    PreviousChapter: '上一组',
    RepeatChapter: '重复本组',
    DictationChapter: '默写本组',
    PlayWordPronunciation: '播放发音',
    ToggleShowTranslate: '切换显示翻译',
    ToggleDictation: '切换默写模式',
    ToggleTheme: '切换主题',
    ToggleConciseMode: '切换简洁模式',
    TogglePanel: '切换面板',
    RandomWrite: '随机默写',
    NextRandomWrite: '继续随机默写',
    KnowWord: '认识单词',
    UnknownWord: '不认识单词',
  }

  return shortcutKeyNameMap[key] || key
}

function resetShortcutKeyMap() {
  editShortcutKey = ''
  settingStore.shortcutKeyMap = cloneDeep(DefaultShortcutKeyMap)
  Toast.success('恢复成功')
}

let importLoading = $ref(false)

const { loading: exportLoading, exportData } = useExport()

function importJson(str: string, notice: boolean = true) {
  importLoading = true
  let obj = {
    version: -1,
    val: {
      setting: {},
      dict: {},
      [PRACTICE_WORD_CACHE.key]: {},
      [PRACTICE_ARTICLE_CACHE.key]: {},
      [APP_VERSION.key]: {},
    },
  }
  try {
    obj = JSON.parse(str)
    let data = obj.val
    let settingState = checkAndUpgradeSaveSetting(data.setting)
    settingState.load = true
    settingStore.setState(settingState)
    let baseState = checkAndUpgradeSaveDict(data.dict)
    baseState.load = true
    store.setState(baseState)
    if (obj.version >= 3) {
      try {
        let save: any = obj.val[PRACTICE_WORD_CACHE.key] || {}
        if (save.val && Object.keys(save.val).length > 0) {
          localStorage.setItem(PRACTICE_WORD_CACHE.key, JSON.stringify(obj.val[PRACTICE_WORD_CACHE.key]))
        }
      } catch (e) {
        //todo 上报
      }
    }
    if (obj.version >= 4) {
      try {
        let save: any = obj.val[PRACTICE_ARTICLE_CACHE.key] || {}
        if (save.val && Object.keys(save.val).length > 0) {
          localStorage.setItem(PRACTICE_ARTICLE_CACHE.key, JSON.stringify(obj.val[PRACTICE_ARTICLE_CACHE.key]))
        }
      } catch (e) {
        //todo 上报
      }
      try {
        let r: any = obj.val[APP_VERSION.key] || -1
        set(APP_VERSION.key, r)
        runtimeStore.isNew = r ? APP_VERSION.version > Number(r) : true
      } catch (e) {
        //todo 上报
      }
    }
    notice && Toast.success('导入成功！')
  } catch (err) {
    return Toast.error('导入失败！')
  } finally {
    importLoading = false
  }
}

let timer = -1
async function beforeImport() {
  if (!IS_DEV) {
    importLoading = true
    await exportData('已自动备份数据', 'LexionCraft数据备份.zip')
    await sleep(1500)
  }
  let d: HTMLDivElement = document.querySelector('#import')
  d.click()
  timer = setTimeout(() => (importLoading = false), 1000)
}

async function importData(e) {
  clearTimeout(timer)
  importLoading = true
  let file = e.target.files[0]
  if (!file) return (importLoading = false)
  if (file.name.endsWith('.json')) {
    let reader = new FileReader()
    reader.onload = function (v) {
      let str: any = v.target.result
      if (str) {
        importJson(str)
      }
    }
    reader.readAsText(file)
  } else if (file.name.endsWith('.zip')) {
    try {
      const JSZip = await loadJsLib('JSZip', LIB_JS_URL.JSZIP)
      const zip = await JSZip.loadAsync(file)

      const dataFile = zip.file('data.json')
      if (!dataFile) {
        return Toast.error('缺少 data.json，导入失败')
      }

      const mp3Folder = zip.folder('mp3')
      if (mp3Folder) {
        const records: { id: string; file: Blob }[] = []
        for (const filename in zip.files) {
          if (filename.startsWith('mp3/') && filename.endsWith('.mp3')) {
            const entry = zip.file(filename)
            if (!entry) continue
            const blob = await entry.async('blob')
            const id = filename.replace(/^mp3\//, '').replace(/\.mp3$/, '')
            records.push({ id, file: blob })
          }
        }
        await set(LOCAL_FILE_KEY, records)
      }

      const str = await dataFile.async('string')
      importJson(str, false)

      Toast.success('导入成功！')
    } catch (e) {
      Toast.error(e?.message || e || '导入失败')
    } finally {
      importLoading = false
    }
  } else {
    Toast.error('不支持的文件类型')
  }
  importLoading = false
}


</script>

<template>
  <div class="setting-container">
    <div class="setting text-md flex flex-col h-full">
      <div class="flex flex-1 overflow-hidden">
        <div class="setting-left">
          <div class="tabs">
            <div class="tab" :class="tabIndex === 0 && 'active'" @click="tabIndex = 0">
              <IconFluentSettings20Regular class="tab-icon" />
              <span>通用设置</span>
            </div>
            <div class="tab" :class="tabIndex === 1 && 'active'" @click="tabIndex = 1">
              <IconFluentTextUnderlineDouble20Regular class="tab-icon" />
              <span>单词设置</span>
            </div>
            <div class="tab" :class="tabIndex === 2 && 'active'" @click="tabIndex = 2">
              <IconFluentBookLetter20Regular class="tab-icon" />
              <span>文章设置</span>
            </div>
            <div class="tab" :class="tabIndex === 4 && 'active'" @click="tabIndex = 4">
              <IconFluentDatabasePerson20Regular class="tab-icon" />
              <span>数据管理</span>
            </div>

            <div class="tab" :class="tabIndex === 3 && 'active'" @click="tabIndex = 3">
              <IconFluentKeyboardLayoutFloat20Regular class="tab-icon" />
              <span>快捷键设置</span>
            </div>

            <div
              class="tab"
              :class="tabIndex === 5 && 'active'"
              @click="
                () => {
                  tabIndex = 5
                  runtimeStore.isNew = false
                  set(APP_VERSION.key, APP_VERSION.version)
                }
              "
            >
              <IconFluentTextBulletListSquare20Regular class="tab-icon" />
              <span>更新日志</span>
              <div class="red-point" v-if="runtimeStore.isNew"></div>
            </div>
            <div class="tab" :class="tabIndex === 6 && 'active'" @click="tabIndex = 6">
              <IconFluentPerson20Regular class="tab-icon" />
              <span>关于</span>
            </div>
          </div>
        </div>
        <div class="setting-content flex-1 overflow-y-auto overflow-x-hidden p-6">
          <CommonSetting v-if="tabIndex === 0" />
          <WordSetting v-if="tabIndex === 1" />
          <ArticleSetting v-if="tabIndex === 2" />

          <div class="shortcut-body" v-if="tabIndex === 3">
            <div class="shortcut-header">
              <label class="main-title">功能</label>
              <div class="wrapper">快捷键 (点击修改)</div>
            </div>
            <div class="shortcut-list">
              <div class="shortcut-row" v-for="item of Object.entries(settingStore.shortcutKeyMap)">
                <label class="item-title">{{ getShortcutKeyName(item[0]) }}</label>
                <div class="wrapper" @click="editShortcutKey = item[0]">
                  <div class="set-key" v-if="editShortcutKey === item[0]">
                    <input
                      ref="shortcutInput"
                      :value="item[1] ? item[1] : '未设置快捷键'"
                      readonly
                      type="text"
                      @blur="handleInputBlur"
                    />
                    <span class="tip" @click.stop="editShortcutKey = ''"
                      >按键盘进行设置，<span class="text-red!">完成点击这里</span></span
                    >
                  </div>
                  <div v-else class="key-display">
                    <div v-if="item[1]" class="key-tag">{{ item[1] }}</div>
                    <span v-else class="text-tertiary">未设置快捷键</span>
                  </div>
                </div>
              </div>
            </div>
            <div class="shortcut-footer mt-6">
              <BaseButton @click="resetShortcutKeyMap">恢复默认快捷键</BaseButton>
            </div>
          </div>

          <div v-if="tabIndex === 4" class="data-management">
            <div class="info-card">
              <p>
                所有用户数据 <b class="text-red">保存在本地浏览器中</b>。如果您需要在不同的设备、浏览器上使用 {{ APP_NAME }}，
                您需要手动进行数据导出和导入。
              </p>
            </div>
            
            <div class="action-section mt-6">
              <h3 class="section-title">导出备份</h3>
              <p class="section-desc">导出的ZIP文件包含所有学习数据，可在其他设备上导入恢复。</p>
              <BaseButton :loading="exportLoading" size="large" class="mt-3" @click="exportData()"
                >导出数据备份 (ZIP)</BaseButton
              >
            </div>

            <div class="divider my-8"></div>

            <div class="action-section">
              <h3 class="section-title">导入恢复</h3>
              <p class="section-desc">
                导入数据将 <b class="text-red">完全覆盖</b> 当前所有数据，请谨慎操作。
                系统会在执行导入前自动备份当前数据。
              </p>
              <div class="flex gap-4 mt-4">
                <BaseButton size="large" @click="beforeImport" :loading="importLoading">导入数据恢复</BaseButton>
                <input
                  type="file"
                  id="import"
                  class="w-0 h-0 opacity-0"
                  accept="application/json,.zip,application/zip"
                  @change="importData"
                />
              </div>
            </div>
          </div>

          <!--          日志-->
          <Log v-if="tabIndex === 5" />

          <div v-if="tabIndex === 6" class="about-section center flex-col py-8">
            <About />
            <div class="text-md color-gray mt-10">Build {{ gitLastCommitHash }}</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.setting-container {
  height: 100%;
}

.setting {
  .setting-left {
    width: 200px;
    border-right: 1px solid var(--border-color);
    background: var(--sidebar-bg);
    display: flex;
    flex-direction: column;

    .tabs {
      padding: 1rem 0.5rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;

      .tab {
        @apply cursor-pointer flex items-center relative;
        padding: 0.75rem 1rem;
        border-radius: 8px;
        gap: 0.75rem;
        transition: all 0.2s;
        color: var(--text-secondary);

        .tab-icon {
          font-size: 1.2rem;
          color: var(--text-tertiary);
        }

        span {
          font-size: 0.9rem;
          font-weight: 500;
        }

        &:hover {
          background: var(--hover-bg);
          color: var(--text-primary);
          
          .tab-icon {
            color: var(--text-primary);
          }
        }

        &.active {
          background: var(--active-bg);
          color: var(--text-active);
          
          .tab-icon {
            color: var(--text-active);
          }
          
          span {
            font-weight: 600;
          }
        }
      }
    }
  }

  .setting-content {
    background: var(--header-bg);
    
    // 快捷键列表样式
    .shortcut-body {
      display: flex;
      flex-direction: column;
      height: 100%;

      .shortcut-header {
        display: flex;
        justify-content: space-between;
        padding-bottom: 1rem;
        border-bottom: 1px solid var(--border-color);
        margin-bottom: 1rem;
        
        .main-title {
          font-weight: 600;
          color: var(--text-primary);
        }
        
        .wrapper {
          font-size: 0.85rem;
          color: var(--text-tertiary);
        }
      }

      .shortcut-list {
        flex: 1;
        overflow-y: auto;
      }

      .shortcut-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 0.75rem 0;
        border-bottom: 1px solid var(--hover-bg);
        
        .item-title {
          color: var(--text-secondary);
          font-size: 0.95rem;
        }

        .wrapper {
          cursor: pointer;
          
          .key-tag {
            background: var(--hover-bg);
            padding: 0.25rem 0.6rem;
            border-radius: 4px;
            font-family: monospace;
            font-size: 0.85rem;
            border: 1px solid var(--border-color);
            color: var(--text-primary);
          }

          .set-key {
            display: flex;
            flex-direction: column;
            align-items: flex-end;
            gap: 0.5rem;

            input {
              width: 10rem;
              height: 2.2rem;
              text-align: center;
              border-radius: 6px;
              border: 2px solid var(--text-active);
              background: var(--layout-bg);
              color: var(--text-primary);
              font-family: monospace;
              outline: none;
            }
            
            .tip {
              font-size: 0.75rem;
              color: var(--text-tertiary);
            }
          }
        }
      }
    }

    // 数据管理样式
    .data-management {
      .info-card {
        padding: 1rem;
        background: var(--hover-bg);
        border-radius: 8px;
        border-left: 4px solid var(--text-active);
        color: var(--text-secondary);
        line-height: 1.6;
      }

      .section-title {
        font-size: 1.1rem;
        font-weight: 600;
        margin-bottom: 0.5rem;
        color: var(--text-primary);
      }

      .section-desc {
        font-size: 0.9rem;
        color: var(--text-tertiary);
      }

      .divider {
        height: 1px;
        background: var(--border-color);
      }
    }
  }
}

// 移动端适配
@media (max-width: 768px) {
  .setting {
    flex-direction: column;

    .setting-left {
      width: 100%;
      border-right: none;
      border-bottom: 1px solid var(--border-color);

      .tabs {
        flex-direction: row;
        overflow-x: auto;
        padding: 0.5rem;
        gap: 0.25rem;

        .tab {
          white-space: nowrap;
          padding: 0.5rem 0.75rem;
          flex-shrink: 0;

          span {
            display: inline;
          }
        }
      }
    }

    .setting-content {
      padding: 1rem;
      
      .shortcut-row {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.5rem;
        
        .wrapper {
          width: 100%;
          text-align: left;
          
          .set-key {
            align-items: flex-start;
            input {
              width: 100%;
            }
          }
        }
      }
    }
  }
}
</style>

