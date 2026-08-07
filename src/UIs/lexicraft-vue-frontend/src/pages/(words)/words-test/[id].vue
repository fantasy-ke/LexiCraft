<script lang="ts" setup>
import {onMounted} from 'vue'
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
import DoodleIcon from '@/components/doodle/DoodleIcon.vue'

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
let feedbackMessage = $ref('')

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
    if (rand.length) c1 = {
      word: rand[Math.floor(Math.random() * rand.length)].word,
      wordObj: getWordByText(rand[Math.floor(Math.random() * rand.length)].word, list)
    }
  }
  if (!c2) {
    let rand = list.filter(v => v.word.toLowerCase() !== w.word.toLowerCase() && v.word.toLowerCase() !== c1?.word.toLowerCase())
    if (rand.length) c2 = {
      word: rand[Math.floor(Math.random() * rand.length)].word,
      wordObj: getWordByText(rand[Math.floor(Math.random() * rand.length)].word, list)
    }
  }
  if (c1) candidates.push(c1)
  if (c2) candidates.push(c2)
  const labels = candidates.map(v => formatCandidateText(v))
  const order = shuffle([0, 1, 2])
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

const successMessages = ['Nice!', '太强了！', '这一笔记得很牢！', '漂亮，继续写下去！']
const retryMessages = ['红笔圈一下，下次就记住了。', '差一点，正确答案已经浮出来啦。', '没关系，这个词值得再看一眼。']

function select(i: number) {
  let q = questions[index]
  if (!q || q.submitted) return
  q.selectedIndex = i
  q.submitted = true
  if (i === q.correctIndex) {
    feedbackMessage = successMessages[Math.floor(Math.random() * successMessages.length)]
    playCorrect()
  } else {
    feedbackMessage = retryMessages[Math.floor(Math.random() * retryMessages.length)]
    playBeep()
    let temp = q.stem.word.toLowerCase()
    if (!base.wrong.words.find((v: Word) => v.word.toLowerCase() === temp)) {
      base.wrong.words.push(q.stem)
      base.wrong.length = base.wrong.words.length
    }
  }
}

function next() {
  if (index < questions.length - 1) {
    index++
    feedbackMessage = ''
  }
}

function end() {
  router.back()
}

onMounted(init)
</script>

<template>
  <BasePage>
    <div v-if="questions.length" class="test-sheet">
      <span class="sheet-tape" aria-hidden="true"></span>
      <header class="test-heading">
        <div>
          <span class="hand-label">WORD CHECK · {{ index + 1 }} / {{ questions.length }}</span>
          <h1>测试：{{ dict?.name }}</h1>
        </div>
        <div class="progress-doodle" aria-hidden="true">
          <span :style="{width: `${((index + 1) / questions.length) * 100}%`}"></span>
        </div>
      </header>

      <section class="question-block">
        <div class="question-word">
          <span class="question-label">这一个词是——</span>
          <div>
            <strong>{{ questions[index].stem.word }}</strong>
            <VolumeIcon :cb="() => playWordAudio(questions[index].stem.word)" :simple="true" title="播放发音"/>
          </div>
        </div>

        <div class="option-list">
          <button
              v-for="(opt, i) in questions[index].optionTexts"
              :key="i"
              :class="{
                'is-correct': questions[index].submitted && i === questions[index].correctIndex,
                'is-wrong': questions[index].submitted && i === questions[index].selectedIndex && i !== questions[index].correctIndex
              }"
              :disabled="questions[index].submitted"
              class="option-note"
              type="button"
              @click="select(i)"
          >
            <span class="option-letter">{{ ['A', 'B', 'C'][i] }}</span>
            <span>{{ opt }}</span>
            <DoodleIcon v-if="questions[index].submitted && i === questions[index].correctIndex" name="check" :size="25"/>
          </button>
        </div>

        <div
            v-if="questions[index].submitted"
            :class="questions[index].selectedIndex === questions[index].correctIndex ? 'feedback-note--success' : 'feedback-note--wrong'"
            class="feedback-note"
            role="status"
        >
          <DoodleIcon :name="questions[index].selectedIndex === questions[index].correctIndex ? 'spark' : 'note'" :size="34"/>
          <div>
            <h2>{{ feedbackMessage }}</h2>
            <p v-if="questions[index].selectedIndex !== questions[index].correctIndex">
              正确答案：<strong>{{ questions[index].stem.word }}</strong>
              <span>{{ questions[index].optionTexts[questions[index].correctIndex] }}</span>
            </p>
            <p v-else>声音、拼写和释义已经对上了，继续保持这份手感。</p>
          </div>
        </div>

        <div v-if="questions[index].submitted" class="answer-notes">
          <div v-for="candidate in questions[index].candidates" :key="candidate.word" class="answer-note">
            <strong>{{ candidate.word }}</strong>
            <span>{{ candidate.wordObj?.trans?.map(v => v.cn).join('；') || '当前词典未收录释义' }}</span>
          </div>
        </div>

        <footer class="test-actions">
          <BaseButton type="primary" @click="next">继续测试</BaseButton>
          <BaseButton type="info" @click="end">结束</BaseButton>
        </footer>
      </section>
    </div>
  </BasePage>
</template>

<style lang="scss" scoped>
.test-sheet { position: relative; margin: 16px 0 80px; padding: clamp(25px, 5vw, 55px); border: 2px solid var(--ink); border-radius: 24px 16px 27px 18px; background: var(--paper-card); box-shadow: 10px 12px 0 color-mix(in srgb, var(--ink) 14%, transparent); }
.test-sheet::before { position: absolute; inset: 12px; border: 1px dashed color-mix(in srgb, var(--ink) 22%, transparent); border-radius: inherit; content: ''; pointer-events: none; }
.sheet-tape { position: absolute; z-index: 2; top: -11px; left: 50%; width: 110px; height: 26px; border: 1px solid color-mix(in srgb, var(--ink) 16%, transparent); background: color-mix(in srgb, var(--chalk-yellow) 72%, transparent); transform: translateX(-50%) rotate(-3deg); }
.test-heading, .question-block { position: relative; z-index: 1; }
.test-heading { display: flex; align-items: flex-end; justify-content: space-between; gap: 25px; padding-bottom: 22px; border-bottom: 2px solid var(--ink); }
.hand-label { color: var(--pencil-red); font-family: var(--font-hand); font-size: 12px; font-weight: 800; letter-spacing: .12em; }
h1 { margin: 8px 0 0; font-family: var(--font-display); font-size: clamp(30px, 4vw, 50px); font-weight: 500; }
.progress-doodle { width: min(260px, 32vw); height: 12px; padding: 2px; border: 2px solid var(--ink); border-radius: 10px 7px 12px 8px; transform: rotate(-1deg); }
.progress-doodle span { display: block; height: 100%; border-radius: inherit; background: var(--moss-green); transition: width .25s ease; }
.question-block { padding-top: 35px; }
.question-label { display: block; margin-bottom: 8px; color: var(--text-secondary); font-family: var(--font-hand); font-size: 14px; transform: rotate(-1deg); transform-origin: left; }
.question-word > div { display: flex; align-items: center; gap: 12px; }
.question-word strong { font-family: var(--font-display); font-size: clamp(42px, 6vw, 72px); font-weight: 500; letter-spacing: -.04em; }
.option-list { display: grid; margin-top: 32px; gap: 13px; }
.option-note { position: relative; display: grid; min-height: 72px; align-items: center; padding: 13px 18px; border: 2px solid var(--ink); border-radius: 13px 9px 15px 10px; color: var(--ink); background: transparent; cursor: pointer; font: inherit; grid-template-columns: 42px 1fr 30px; gap: 14px; text-align: left; transition: transform .18s ease, background .18s ease, box-shadow .18s ease; }
.option-note:nth-child(2) { transform: rotate(.3deg); }
.option-note:nth-child(3) { transform: rotate(-.4deg); }
.option-note:not(:disabled):hover { background: var(--hover-bg); box-shadow: 4px 5px 0 color-mix(in srgb, var(--ink) 13%, transparent); transform: translateY(-2px) rotate(-.5deg); }
.option-letter { display: grid; width: 32px; height: 32px; place-items: center; border: 1.7px solid var(--ink); border-radius: 50% 44% 52% 46%; font-family: var(--font-hand); font-weight: 800; }
.option-note.is-correct { color: var(--paper-card); background: var(--moss-green); }
.option-note.is-wrong { color: var(--pencil-red); background: color-mix(in srgb, var(--pencil-red) 9%, transparent); }
.option-note.is-wrong::after { position: absolute; inset: -6px -8px; border: 3px solid var(--pencil-red); border-radius: 48% 53% 47% 55%; content: ''; pointer-events: none; transform: rotate(-1.2deg); }
.feedback-note { display: flex; align-items: flex-start; gap: 15px; margin-top: 24px; padding: 22px; border: 2px solid var(--ink); box-shadow: 5px 6px 0 color-mix(in srgb, var(--ink) 12%, transparent); transform: rotate(-.4deg); }
.feedback-note--success { background: color-mix(in srgb, var(--moss-green) 25%, var(--paper-card)); }
.feedback-note--wrong { background: color-mix(in srgb, var(--pencil-red) 24%, var(--paper-card)); }
.feedback-note h2 { margin: 0; font-family: var(--font-hand); font-size: 29px; }
.feedback-note p { margin: 7px 0 0; color: var(--text-secondary); line-height: 1.55; }
.feedback-note p strong { margin-right: 7px; color: var(--pencil-red); font-family: var(--word-font-family); font-size: 18px; }
.feedback-note p span { display: block; margin-top: 4px; }
.answer-notes { display: grid; margin-top: 22px; grid-template-columns: repeat(3, 1fr); gap: 12px; }
.answer-note { padding: 16px; border: 1.5px dashed var(--ink); background: var(--bg-card-secend); transform: rotate(-.5deg); }
.answer-note:nth-child(2) { transform: rotate(.8deg); }
.answer-note strong, .answer-note span { display: block; }
.answer-note strong { font-family: var(--font-display); font-size: 22px; }
.answer-note span { margin-top: 6px; color: var(--text-secondary); font-size: 13px; line-height: 1.5; }
.test-actions { display: flex; gap: 10px; margin-top: 30px; }
.option-note:focus-visible { outline: 3px solid var(--pencil-red); outline-offset: 4px; }

@media (max-width: 720px) {
  .test-sheet { margin-top: 8px; padding: 28px 18px; }
  .test-heading { display: block; }
  .progress-doodle { width: 100%; margin-top: 18px; }
  .option-note { grid-template-columns: 38px 1fr 25px; padding-inline: 12px; }
  .answer-notes { grid-template-columns: 1fr; }
  .test-actions { flex-wrap: wrap; }
}
</style>
