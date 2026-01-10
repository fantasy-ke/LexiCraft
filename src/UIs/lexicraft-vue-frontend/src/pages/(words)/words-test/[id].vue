<script setup lang="ts">
import {onMounted, ref} from 'vue'
import BasePage from '@/components/BasePage.vue'
import BaseButton from '@/components/BaseButton.vue'
import VolumeIcon from '@/components/icon/VolumeIcon.vue'
import {useRoute, useRouter} from 'vue-router'
import {useBaseStore} from '@/stores/base.ts'
import type {Dict, Word} from '@/types/types.ts'
import {_getDictDataByUrl, shuffle} from '@/utils'
import {useRuntimeStore} from '@/stores/runtime.ts'
import {usePlayBeep, usePlayCorrect, usePlayWordAudio} from '@/hooks/sound.ts'
import Toast from '@/components/base/toast/Toast.ts'

type Candidate = { word: string, wordObj?: Word }
type Question = {
  stem: Word,
  candidates: Candidate[],
  optionTexts: string[],
  correctIndex: number,
  selectedIndex: number,
  submitted: boolean
}

const route = useRoute()
const router = useRouter()
const base = useBaseStore()
const runtimeStore = useRuntimeStore()
const playBeep = usePlayBeep()
const playCorrect = usePlayCorrect()
const playWordAudio = usePlayWordAudio()

let loading = $ref(false)
let dict = $ref<Dict>()
let questions = $ref<Question[]>([])
let index = $ref(0)

function getWordByText(val: string, list: Word[]): Word | undefined {
  let r = list.find(v => v.word.toLowerCase() === val.toLowerCase())
  return r
}

function pickRelVariant(w: Word, list: Word[]): Candidate | null {
  let rels = w.relWords?.rels || []
  for (let i = 0; i < rels.length; i++) {
    for (let j = 0; j < rels[i].words.length; j++) {
      let c = rels[i].words[j].c
      let r = getWordByText(c, list)
      if (r && r.word.toLowerCase() !== w.word.toLowerCase()) {
        return {word: r.word, wordObj: r}
      }
    }
  }
  return null
}

function pickSynonym(w: Word, list: Word[]): Candidate | null {
  let synos = w.synos || []
  for (let i = 0; i < synos.length; i++) {
    for (let j = 0; j < synos[i].ws.length; j++) {
      let c = synos[i].ws[j]
      let r = getWordByText(c, list)
      if (r && r.word.toLowerCase() !== w.word.toLowerCase()) {
        return {word: r.word, wordObj: r}
      }
    }
  }
  return null
}

function pickSamePos(w: Word, list: Word[]): Candidate | null {
  let pos = (w.trans?.[0]?.pos || '').trim()
  let samePos = list.filter(v => v.word.toLowerCase() !== w.word.toLowerCase() && v.trans?.some(t => t.pos === pos))
  if (samePos.length) {
    let r = samePos[Math.floor(Math.random() * samePos.length)]
    return {word: r.word, wordObj: r}
  }
  return null
}

function buildQuestion(w: Word, list: Word[]): Question {
  let candidates: Candidate[] = []
  candidates.push({word: w.word, wordObj: w})
  let c1 = pickRelVariant(w, list) || pickSynonym(w, list) || pickSamePos(w, list)
  let c2 = null as Candidate | null
  let tried = new Set<string>([w.word.toLowerCase()])
  if (c1) tried.add(c1.word.toLowerCase())
  let attempts = 0
  while (!c2 && attempts < 5) {
    c2 = pickSynonym(w, list) || pickSamePos(w, list) || pickRelVariant(w, list)
    if (c2 && tried.has(c2.word.toLowerCase())) c2 = null
    attempts++
  }
  if (!c1) {
    let rand = list.filter(v => v.word.toLowerCase() !== w.word.toLowerCase())
    if (rand.length) c1 = {word: rand[Math.floor(Math.random() * rand.length)].word, wordObj: getWordByText(rand[Math.floor(Math.random() * rand.length)].word, list)}
  }
  if (!c2) {
    let rand = list.filter(v => v.word.toLowerCase() !== w.word.toLowerCase() && v.word.toLowerCase() !== c1?.word.toLowerCase())
    if (rand.length) c2 = {word: rand[Math.floor(Math.random() * rand.length)].word, wordObj: getWordByText(rand[Math.floor(Math.random() * rand.length)].word, list)}
  }
  if (c1) candidates.push(c1)
  if (c2) candidates.push(c2)
  const labels = candidates.map(v => formatCandidateText(v))
  const order = shuffle([0,1,2])
  const optionTexts = order.map(i => labels[i])
  const correctIndex = order.indexOf(0)
  return {
    stem: w,
    candidates,
    optionTexts,
    correctIndex,
    selectedIndex: -1,
    submitted: false
  }
}

function formatCandidateText(c: Candidate): string {
  const w = c.wordObj
  if (!w || !w.trans || !w.trans.length) return '当前词典未收录释义'

  const cleanCn = (cn: string, head: string) => {
    let t = cn || ''
    // 去掉含英文的括号片段（避免出现人名或英文拼写）
    t = t.replace(/（[^）]*[A-Za-z][^）]*）/g, '')
    // 去掉“时态/过去式/复数”等形态说明
    t = t.replace(/(时\s*态|过去式|过去分词|现在分词|复数|第三人称|比较级|最高级)[:：].*/g, '')
    // 去掉直接出现的英文词头
    const headEsc = head.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
    t = t.replace(new RegExp(headEsc, 'gi'), '')
    // 统一分隔符为中文分号
    t = t.replace(/[;；]\s*/g, '；')
    // 收尾空白
    t = t.trim()
    return t
  }

  const parts = w.trans
    .map(v => {
      const pos = (v.pos || '').trim()
      const cn = cleanCn(v.cn || '', w.word)
      if (/^\s*【名】/.test(v.cn || '')) return ''
      if (!cn) return ''
      return `${pos ? '- ' + pos + ' ' : '- '}${cn}`
    })
    .filter(Boolean)

  return parts.length ? parts.join('；') : '当前词典未收录释义'
}

async function init() {
  let dictId: any = route.params.id
  let d = base.word.bookList.find(v => v.id === dictId)
  if (!d) d = base.sdict
  if (!d?.id) return router.push('/words')
  if (!d.words.length && runtimeStore.editDict?.id === d.id) {
    loading = true
    let r = await _getDictDataByUrl(runtimeStore.editDict)
    d = r
    loading = false
  }
  dict = d
  if (!dict.words.length) {
    return Toast.warning('没有单词可测试！')
  }
  const wordList = shuffle(dict.words)
  questions = wordList.map(w => buildQuestion(w, dict.words))
  index = 0
}

function select(i: number) {
  let q = questions[index]
  if (!q || q.submitted) return
  q.selectedIndex = i
  q.submitted = true
  if (i === q.correctIndex) {
    playCorrect()
  } else {
    playBeep()
    let temp = q.stem.word.toLowerCase()
    if (!base.wrong.words.find((v: Word) => v.word.toLowerCase() === temp)) {
      base.wrong.words.push(q.stem)
      base.wrong.length = base.wrong.words.length
    }
  }
}

function next() {
  if (index < questions.length - 1) index++
}

function end() {
  router.back()
}

onMounted(init)
</script>

<template>
  <BasePage>
    <div class="card flex flex-col">
      <div class="flex items-center justify-between">
        <div class="page-title">测试：{{ dict?.name }}</div>
        <div class="text-base">{{ index + 1 }} / {{ questions.length }}</div>
      </div>
      <div class="line my-2"></div>

      <div v-if="questions.length" class="flex flex-col gap-4">
        <div class="text-2xl en-article-family flex items-center gap-2">
          <span>题目：{{ questions[index].stem.word }}</span>
          <VolumeIcon :simple="true" :title="'发音'" :cb="() => playWordAudio(questions[index].stem.word)"/>
        </div>
        <div class="grid gap-2">
          <div
            v-for="(opt,i) in questions[index].optionTexts"
            :key="i"
            class="option border rounded p-2 cursor-pointer"
            :class="{
              'text-green-600': questions[index].submitted && i === questions[index].correctIndex,
              'text-red-600': questions[index].submitted && i === questions[index].selectedIndex && i !== questions[index].correctIndex
            }"
            @click="select(i)"
          >
            <span>(<span class="italic">{{ ['A','B','C'][i] }}</span>) {{ opt }}</span>
          </div>
        </div>

        <div v-if="questions[index].submitted" class="mt-4">
          <div class="mb-2 text-base">选项解析：</div>
          <div class="grid gap-2 grid-cols-1 md:grid-cols-3">
            <div v-for="(c,i) in questions[index].candidates" :key="i" class="p-2 rounded bg-[--bg-card-secend]">
              <div class="en-article-family text-lg">{{ c.word }}</div>
              <div class="mt-1 text-sm">{{ c.wordObj?.trans?.map(v => v.cn).join('；') || '当前词典未收录释义' }}</div>
            </div>
          </div>
        </div>

        <div class="mt-6 flex">
          <BaseButton type="primary" @click="next">继续测试</BaseButton>
          <BaseButton type="info" @click="end">结束</BaseButton>
        </div>
      </div>
    </div>
  </BasePage>
</template>

<style scoped>
.option:hover { background: var(--color-second); }
</style>
