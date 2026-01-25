<script lang="ts" setup>
import BackIcon from '@/components/BackIcon.vue'
import Empty from '@/components/Empty.vue'
import ArticleList from '@/components/list/ArticleList.vue'
import {useBaseStore} from '@/stores/base.ts'
import type {Article, Dict} from '@/types/types'
import {useRuntimeStore} from '@/stores/runtime.ts'
import BaseButton from '@/components/BaseButton.vue'
import {useRoute, useRouter} from 'vue-router'
import EditBook from '@/components/article/components/EditBook.vue'
import {computed, onMounted, onUnmounted, watch} from 'vue'
import {_dateFormat, _getDictDataByUrl, _nextTick, msToHourMinute, resourceWrap, total, useNav} from '@/utils'
import {getDefaultArticle, getDefaultDict} from '@/types/func.ts'
import Toast from '@/components/base/toast/Toast.ts'
import ArticleAudio from '@/components/article/components/ArticleAudio.vue'
import {MessageBox} from '@/utils/MessageBox.tsx'
import {useSettingStore} from '@/stores/setting.ts'
import {useFetch} from '@vueuse/core'
import {DICT_LIST} from '@/config/env.ts'
import BaseIcon from '@/components/BaseIcon.vue'
import Switch from '@/components/base/Switch.vue'
import {useGetDict} from '@/hooks/dict.ts'
import {DictType} from '@/types/enum.ts'

const runtimeStore = useRuntimeStore()
const settingStore = useSettingStore()
const store = useBaseStore()
const router = useRouter()
const route = useRoute()
const {nav} = useNav()

let isEdit = $ref(false)
let isAdd = $ref(false)
let studyLoading = $ref(false)

let selectArticle: Article = $ref(getDefaultArticle({id: -1}))

function handleCheckedChange(val) {
  selectArticle = val.item
}

async function startPractice() {
  let sbook = runtimeStore.editDict
  if (!sbook.articles.length) {
    return Toast.warning('没有文章可学习！')
  }
  studyLoading = true
  await store.changeBook(sbook)
  studyLoading = false

  window.umami?.track('startStudyArticle', {
    name: sbook.name,
    custom: sbook.custom,
    complete: sbook.complete,
    s: `name:${sbook.name},index:${sbook.lastLearnIndex},title:${sbook.articles[sbook.lastLearnIndex].title}`,
  })
  nav('/app/practice-articles/' + sbook.id)
}

const showBookDetail = computed(() => {
  return !(isAdd || isEdit)
})

const {loading} = useGetDict()

onMounted(() => {
  if (route.query?.isAdd) {
    isAdd = true
    runtimeStore.editDict = getDefaultDict()
  }
  window.addEventListener('resize', handleResize)
  runtimeStore.pageTitle = runtimeStore.editDict.name
})

watch(() => runtimeStore.editDict.name, (val) => {
  if (val) runtimeStore.pageTitle = val
})

onUnmounted(() => {
  runtimeStore.pageTitle = ''
  window.removeEventListener('resize', handleResize)
})

function handleResize() {
  if (displayMode === 'inline') {
    positionTranslations()
  }
}

function formClose() {
  if (isEdit) isEdit = false
  else router.back()
}

const {data: book_list} = useFetch(resourceWrap(DICT_LIST.ARTICLE.ALL)).json()

function reset() {
  MessageBox.confirm(
      '继续此操作会重置所有文章，并从官方书籍获取最新文章列表，学习记录不会被重置。确认恢复默认吗？',
      '恢复默认',
      async () => {
        let dict = book_list.value.find(v => v.url === runtimeStore.editDict.url) as Dict
        if (dict && dict.id) {
          dict = await _getDictDataByUrl(dict, DictType.article)
          let rIndex = store.article.bookList.findIndex(v => v.id === runtimeStore.editDict.id)
          if (rIndex > -1) {
            let item = store.article.bookList[rIndex]
            item.custom = false
            item.id = dict.id
            item.articles = dict.articles
            if (item.lastLearnIndex >= item.articles.length) {
              item.lastLearnIndex = item.articles.length - 1
            }
            runtimeStore.editDict = item
            Toast.success('恢复成功')
            return
          }
        }
        Toast.error('恢复失败')
      }
  )
}

const currentPractice = $computed(() => {
  if (runtimeStore.editDict.statistics?.length) {
    return runtimeStore.editDict.statistics.filter(v => v.title === selectArticle.title)
  }
  return []
})

const totalSpend = $computed(() => {
  if (runtimeStore.editDict.statistics?.length) {
    return msToHourMinute(total(runtimeStore.editDict.statistics, 'spend'))
  }
  return 0
})

function next() {
  if (!settingStore.articleAutoPlayNext) return
  startPlay = true
  let index = runtimeStore.editDict.articles.findIndex(v => v.id === selectArticle.id)
  if (index > -1) {
    //如果是最后一个
    if (index === runtimeStore.editDict.articles.length - 1) index = -1
    selectArticle = runtimeStore.editDict.articles[index + 1]
  }
}

const list = $computed(() => {
  return [
    getDefaultArticle({
      title: '介绍',
      id: -1,
    }),
  ].concat(runtimeStore.editDict.articles)
})
console.log('list', list)

let showTranslate = $ref(true)
let startPlay = $ref(false)
let showDisplayMode = $ref(false)
let displayMode = $ref<'card' | 'inline' | 'line'>('inline')
let articleWrapperRef = $ref<HTMLElement>()

const handleVolumeUpdate = (volume: number) => {
  settingStore.articleSoundVolume = volume
}

const handleSpeedUpdate = (speed: number) => {
  settingStore.articleSoundSpeed = speed
}

// 计算段落数量
const paragraphCount = $computed(() => {
  if (!selectArticle.text) return 0
  return selectArticle.text.split('\n\n').filter(p => p.trim()).length
})

// 判断是否应该在段落下显示译文（card 模式且段落数 > 1）
const shouldShowInlineTranslation = $computed(() => {
  return displayMode === 'card' && paragraphCount > 1
})

// 定位翻译到原文下方
function positionTranslations() {
  if (loading.value || selectArticle.id === -1) return
  _nextTick(() => {
    const articleRect = articleWrapperRef.getBoundingClientRect()
    selectArticle.textTranslate.split('\n\n').forEach((paragraph, paraIndex) => {
      paragraph.split('\n').forEach((sentence, sentIndex) => {
        const location = `${paraIndex}-${sentIndex}`
        const sentenceClassName = `.word-${location}-0`
        const sentenceEl = articleWrapperRef?.querySelector(sentenceClassName)
        const translateClassName = `.translate-${location}`
        const translateEl = articleWrapperRef?.querySelector(translateClassName) as HTMLDivElement

        if (sentenceEl && translateEl && sentence) {
          const sentenceRect = sentenceEl.getBoundingClientRect()
          translateEl.style.opacity = '1'
          translateEl.style.top = sentenceRect.top - articleRect.top + 24 + 'px'
          const spaceEl = translateEl.firstElementChild as HTMLElement
          if (spaceEl) {
            spaceEl.style.width = sentenceRect.left - articleRect.left + 'px'
          }
        }
      })
    })
  })
}

// 监听显示模式和文章变化，重新定位翻译
watch([() => displayMode, () => selectArticle.id, () => showTranslate], () => {
  if (displayMode !== 'card') {
    positionTranslations()
  }
})
</script>

<template>
  <div class="center h-screen">
    <div
        class="bg-second w-full 3xl:w-7/10 2xl:w-8/10 xl:w-full 2xl:card 2xl:h-[97vh] h-full p-3 box-border overflow-hidden mb-0"
    >
      <div v-if="showBookDetail" v-loading="loading" class="flex box-border flex-col h-full">
        <!-- New Rich Header -->
        <div class="content-header">
          <div class="header-bg"></div>
          <div class="header-content">
            <div class="left-section">
              <BackIcon class="back-btn"/>
              <div class="info-box">
                <div class="title-row">
                  <h1 class="dict-title">{{ runtimeStore.editDict.name }}</h1>
                  <span v-if="runtimeStore.editDict.custom" class="badge custom">Custom</span>
                </div>

                <div class="meta-row">
                  <span class="meta-item">
                    <IconFluentDocumentMultiple20Regular/>
                    {{ list.length - 1 }} Articles
                  </span>
                  <span v-if="totalSpend" class="meta-item">
                    <IconFluentClock20Regular/>
                    {{ totalSpend }} Learning
                  </span>
                </div>

                <div v-if="runtimeStore.editDict.description" :title="runtimeStore.editDict.description"
                     class="description">
                  {{ runtimeStore.editDict.description }}
                </div>
              </div>
            </div>

            <div class="right-section">
              <div class="action-group">
                <BaseButton :loading="studyLoading || loading" size="large" type="primary" @click="startPractice">
                  <div class="flex items-center gap-1">
                    <IconFluentHatGraduation20Regular/>
                    <span>开始学习</span>
                  </div>
                </BaseButton>
                <BaseButton size="large" type="info" @click="router.push('/batch-edit-article')">
                  <IconFluentAppsListDetail20Regular/>
                  <span>文章管理</span>
                </BaseButton>
              </div>

              <div class="secondary-actions">
                <BaseButton :loading="studyLoading || loading" type="text" @click="isEdit = true">
                  编辑详情
                </BaseButton>
                <BaseButton
                    v-if="runtimeStore.editDict.custom && runtimeStore.editDict.url"
                    type="text"
                    @click="reset"
                >
                  恢复默认
                </BaseButton>
              </div>
            </div>
          </div>
        </div>
        <div class="flex flex-1 overflow-hidden mt-3">
          <div class="3xl:w-80 2xl:w-60 xl:w-55 lg:w-50 overflow-auto">
            <ArticleList
                v-if="list.length"
                :active-id="selectArticle.id"
                :list="list"
                :show-desc="true"
                @click="handleCheckedChange"
            >
            </ArticleList>
            <Empty v-else/>
          </div>
          <div class="flex-1 shrink-0 pl-4 flex flex-col overflow-hidden">
            <template v-if="selectArticle.id">
              <template v-if="selectArticle.id === -1">
                <div class="flex gap-4 mt-2">
                  <img
                      v-if="runtimeStore.editDict?.cover"
                      :src="runtimeStore.editDict?.cover"
                      alt=""
                      class="w-30 rounded-md"
                  />
                  <div class="text-lg">{{ runtimeStore.editDict.description }}</div>
                </div>
                <div v-if="totalSpend" class="text-base mt-10">总学习时长：{{ totalSpend }}</div>
              </template>
              <template v-else>
                <div class="flex-1 overflow-auto pb-30">
                  <div>
                    <div class="flex justify-between items-center relative">
                      <span>
                        <span class="text-3xl">{{ selectArticle.title }}</span>
                        <span v-if="showTranslate" class="ml-6 text-2xl">{{ selectArticle.titleTranslate }}</span>
                      </span>
                      <div class="flex items-center gap-2 mr-4">
                        <BaseIcon :title="`开关释义显示`" @click="showTranslate = !showTranslate">
                          <IconFluentTranslate16Regular v-if="showTranslate"/>
                          <IconFluentTranslateOff16Regular v-else/>
                        </BaseIcon>
                        <BaseIcon
                            :disabled="!showTranslate"
                            :title="`切换显示模式`"
                            @click="showDisplayMode = !showDisplayMode"
                        >
                          <IconFluentTextAlignLeft16Regular/>
                        </BaseIcon>
                      </div>
                    </div>

                    <div v-if="showDisplayMode" class="flex gap-1 mr-4 justify-end">
                      <BaseIcon title="逐行显示" @click="displayMode = 'inline'">
                        <IconFluentTextPositionThrough20Regular/>
                      </BaseIcon>
                      <BaseIcon title="单行显示" @click="displayMode = 'line'">
                        <IconFluentTextAlignLeft16Regular/>
                      </BaseIcon>
                      <BaseIcon title="对照显示" @click="displayMode = 'card'">
                        <IconFluentAlignSpaceFitVertical20Regular/>
                      </BaseIcon>
                    </div>

                    <div v-if="selectArticle?.question?.text" class="mt-2 text-2xl">
                      <div>Question: {{ selectArticle?.question?.text }}</div>
                      <div
                          v-if="showTranslate && (displayMode !== 'card' || shouldShowInlineTranslation)"
                          class="text-xl color-translate-second"
                      >
                        问题: {{ selectArticle?.question?.translate }}
                      </div>
                    </div>
                  </div>

                  <div
                      ref="articleWrapperRef"
                      :class="[showTranslate && displayMode !== 'card' && 'tall']"
                      class="article-content mt-6"
                  >
                    <article>
                      <template v-for="(t, i) in selectArticle.text.split('\n\n')" :key="`para-${i}`">
                        <div class="article-row w-full mb-10">
                          <span
                              v-for="(w, j) in t.split('\n')"
                              :key="`${i}-${j}`"
                              :class="displayMode === 'line' && 'block'"
                          >
                            <span
                                v-for="(s, n) in w.split(' ').filter(Boolean)"
                                :key="`${i}-${j}-${n}`"
                                :class="`inline-block word-${i}-${j}-${n}`"
                            ><span>{{ s }}</span>
                              <span class="space"></span>
                            </span>
                          </span>
                        </div>

                        <!-- 当 card 模式且段落数 > 1 时，在每个段落下显示对应译文 -->
                        <div
                            v-if="shouldShowInlineTranslation && showTranslate && selectArticle.textTranslate"
                            class="trans-row text-xl color-translate-second -mt-7 mb-10"
                        >
                          <div v-if="selectArticle.textTranslate.split('\n\n')[i]">
                            {{ selectArticle.textTranslate.split('\n\n')[i] }}
                          </div>
                        </div>
                      </template>
                      <div class="text-right italic">
                        <div v-if="selectArticle?.quote?.text" class="text-2xl">{{ selectArticle?.quote?.text }}</div>
                        <div
                            v-if="
                            selectArticle?.quote?.translate &&
                            showTranslate &&
                            (displayMode !== 'card' || shouldShowInlineTranslation)
                          "
                            class="trans-row text-xl color-translate-second"
                        >
                          {{ selectArticle?.quote?.translate }}
                        </div>
                      </div>
                    </article>

                    <template v-if="showTranslate && selectArticle.textTranslate">
                      <div v-if="displayMode !== 'card'" class="translate color-translate-second">
                        <div
                            v-for="(t, i) in selectArticle.textTranslate.split('\n\n')"
                            class="break-words w-full section"
                        >
                          <div v-for="(w, j) in t.split('\n')" :key="`${i}-${j}`" :class="`row translate-${i}-${j}`">
                            <span class="space"></span>
                            <span>{{ w }}</span>
                          </div>
                        </div>
                      </div>
                      <template v-else>
                        <!-- 当段落数 <= 1 时，保持原样在文章末尾显示译文 -->
                        <template v-if="!shouldShowInlineTranslation">
                          <div class="line my-10"></div>
                          <div class="text-xl line-height-normal space-y-5">
                            <div v-if="selectArticle?.question?.translate" class="mt-2">
                              问题: {{ selectArticle?.question?.translate }}
                            </div>
                            <div v-for="t in selectArticle.textTranslate.split('\n\n')" class="trans-row">{{ t }}</div>
                            <div class="trans-row text-right italic">{{ selectArticle?.quote?.translate }}</div>
                          </div>
                        </template>
                      </template>
                    </template>
                  </div>
                  <template v-if="currentPractice.length">
                    <div class="line my-10"></div>
                    <div class="font-family text-base pr-2">
                      <div class="text-2xl font-bold">学习记录</div>
                      <div class="mt-1 mb-3">总学习时长：{{ msToHourMinute(total(currentPractice, 'spend')) }}</div>
                      <div
                          v-for="i in currentPractice"
                          class="item border border-item border-solid mt-2 p-2 bg-[var(--bg-history)] rounded-md flex justify-between"
                      >
                        <span class="color-gray">{{ _dateFormat(i.startDate) }}</span>
                        <span>{{ msToHourMinute(i.spend) }}</span>
                      </div>
                    </div>
                  </template>
                </div>
                <div
                    v-if="selectArticle.audioSrc || selectArticle.audioFileId"
                    class="border-t-1 border-t-gray-300 border-solid border-0 center gap-2 pt-4"
                >
                  <ArticleAudio
                      :article="selectArticle"
                      :autoplay="settingStore.articleAutoPlayNext && startPlay"
                      @ended="next"
                      @update-speed="handleSpeedUpdate"
                      @update-volume="handleVolumeUpdate"
                  />
                  <div class="flex items-center gap-1">
                    <span>结束后播放下一篇</span>
                    <Switch v-model="settingStore.articleAutoPlayNext"/>
                  </div>
                </div>
              </template>
            </template>
            <Empty v-else/>
          </div>
        </div>
      </div>
      <div v-else class="">
        <div class="dict-header flex justify-between items-center relative">
          <BackIcon class="dict-back z-2" @click="isAdd ? $router.back() : (isEdit = false)"/>
          <div class="dict-title absolute text-2xl text-align-center w-full">
            {{ runtimeStore.editDict.id ? '修改' : '创建' }}书籍
          </div>
        </div>
        <div class="center">
          <EditBook :is-add="isAdd" :is-book="true" @close="formClose" @submit="isEdit = isAdd = false"/>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.dict-detail-card {
  height: calc(100vh - 3rem);
}

/* Rich Content Header Styles */
.content-header {
  position: relative;
  background: linear-gradient(to bottom, var(--color-second), var(--color-primary));
  padding: 1.5rem 2rem;
  border-bottom: 1px solid var(--border-color);
  margin-bottom: 1rem;
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
    display: flex;
    gap: 0.5rem;

    .base-button {
      padding: 0 0.5rem;
      font-size: 0.9rem;
      color: #555 !important;
      min-width: unset;
      font-weight: 500;

      &:hover {
        color: var(--color-link) !important;
        text-decoration: underline;
      }
    }
  }
}

/* Mobile Adaptation */
@media (max-width: 768px) {
  .content-header {
    padding: 1rem;
    background: var(--bg-primary);
  }

  .header-content {
    flex-direction: column;
    gap: 1rem;
  }

  .left-section {
    width: 100%;
  }

  .cover-box {
    width: 70px;
    height: 70px;
  }

  .info-box {
    .title-row .dict-title {
      font-size: 1.25rem;
    }
  }

  .right-section {
    width: 100%;
    align-items: stretch;

    .action-group {
      width: 100%;

      .base-button {
        flex: 1;
      }
    }

    .secondary-actions {
      align-self: center;
    }
  }
}

// 打字式显示模式样式（复用 TypingArticle 的样式）
$translate-lh: 3.2;
$article-lh: 2.4;

.article-content {
  position: relative;
  font-size: 1.6rem;

  &.tall {
    article {
      line-height: $article-lh;
      color: var(--color-article);
    }
  }

  .article-row {
    word-break: keep-all;
    word-wrap: break-word;
    white-space: pre-wrap;
  }

  .trans-row {
    @apply cn-article-family font-bold;
  }

  article {
    @apply en-article-family;
  }

  .translate {
    @apply absolute top-0 left-0 h-full w-full text-xl pointer-events-none font-bold cn-article-family;
    line-height: $translate-lh;
    letter-spacing: 0.2rem;

    .row {
      @apply absolute left-0 w-full opacity-0 transition-all duration-300;
    }
  }
}

.space {
  @apply inline-block w-2 transition-all duration-300;
}

.sentence-translate-mobile {
  display: none;
  margin-top: 0.4rem;
  font-size: 0.9rem;
  line-height: 1.4;
  color: var(--color-font-3);
  font-family: var(--zh-article-family);
  word-break: break-word;
}

@media (max-width: 768px) {
  .dict-detail-card {
    height: calc(100vh - 2rem);
  }

  .dict-header {
    width: 100%;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    text-align: center;
    gap: 0.75rem;
  }

  .dict-header .dict-back {
    align-self: flex-start;
  }

  .dict-header .dict-title {
    position: static !important;
    width: 100%;
  }

  .dict-header .dict-actions {
    width: 100%;
    justify-content: center;
    gap: 0.75rem;

    .base-button {
      flex: 1 0 45%;
      min-width: 8rem;
    }
  }
}

@media (max-width: 480px) {
  .dict-header .dict-actions {
    flex-direction: column;

    .base-button {
      width: 100%;
      min-width: auto;
    }
  }
}

// 移动端适配 - 打字式显示模式
@media (max-width: 768px) {
  .article-content {
    article {
      .section {
        margin-bottom: 1rem;

        .sentence {
          font-size: 1rem;
          line-height: 1.6;
          word-break: break-word;
          margin-bottom: 0.5rem;
        }
      }
    }

    .translate {
      display: none;
    }
  }

  .sentence-translate-mobile {
    display: block;
  }
}

@media (max-width: 480px) {
  .article-content {
    article {
      .section {
        .sentence {
          font-size: 0.9rem;
          line-height: 1.5;
        }
      }
    }
  }

  .sentence-translate-mobile {
    font-size: 0.85rem;
    line-height: 1.35;
  }
}
</style>
```````` ;
