<template>
  <div class="dashboard-page">
    <section class="welcome-sheet">
      <div class="welcome-copy">
        <p class="date-line">{{ currentDate }} · {{ greeting }}</p>
        <h1>嗨，{{ displayName }}。<br><span>今天想写下哪个词？</span></h1>
        <p class="welcome-description">别急着完成一整本词典。先专心记住眼前这一小页，剩下的交给每天的重复。</p>
        <div class="welcome-actions">
          <button class="primary-action" type="button" @click="continueLearning">
            <DoodleIcon name="words" :size="24"/>
            {{ primaryCourse ? '继续上次的单词' : '挑一本词典开始' }}
            <DoodleIcon name="arrow" :size="20"/>
          </button>
          <button class="secondary-action" type="button" @click="router.push('/app/articles')">
            <DoodleIcon name="book" :size="22"/>
            去读一篇文章
          </button>
        </div>
      </div>

      <div class="welcome-sketch" aria-hidden="true">
        <span class="sketch-tape"></span>
        <svg viewBox="0 0 470 330">
          <path class="paper-fill" d="M55 34c104 7 211 4 333-7l25 256c-128 16-242 21-357 10Z"/>
          <path class="line" d="M55 34c104 7 211 4 333-7l25 256c-128 16-242 21-357 10ZM87 83c72 3 146 0 240-7M88 119c105 3 199-2 275-9M90 155c79 4 157 2 235-5"/>
          <path class="line green" d="m96 219 29 26 55-72"/>
          <path class="line red" d="M214 194c55-21 132-8 139 34 7 38-49 64-99 55-54-9-79-67-40-89Z"/>
          <text x="229" y="241">brave</text>
          <path class="line yellow" d="m386 57 8 20 22 2-17 14 5 21-18-11-18 12 5-21-18-13 22-2z"/>
        </svg>
        <span class="sketch-note">one page<br>at a time!</span>
      </div>
    </section>

    <section class="dashboard-grid">
      <article class="streak-card paper-card">
        <div class="card-heading">
          <div>
            <span class="hand-label">KEEP THE RHYTHM</span>
            <h2>连续 {{ checkInStats.consecutiveDays }} 天</h2>
          </div>
          <DoodleIcon name="calendar" :size="38"/>
        </div>
        <div class="week-grid" aria-label="本周打卡记录">
          <div v-for="(day, index) in weekDays" :key="day" :class="{done: checkInStats.weekHistory[index], today: index === todayIndex}" class="week-day">
            <span>{{ day }}</span>
            <DoodleIcon v-if="checkInStats.weekHistory[index]" name="check" :size="25"/>
            <i v-else-if="index === todayIndex"></i>
          </div>
        </div>
        <p>累计留下 {{ checkInStats.totalDays }} 页学习痕迹。今天也不用写很多，只要别让这一页空着。</p>
      </article>

      <article class="quote-card paper-card">
        <span class="quote-pin" aria-hidden="true"></span>
        <DoodleIcon name="note" :size="31"/>
        <span class="hand-label">TODAY'S SENTENCE</span>
        <blockquote>“{{ dailyQuote.text }}”</blockquote>
        <cite>— {{ dailyQuote.author }}</cite>
      </article>

      <article class="stats-card paper-card">
        <div class="card-heading">
          <div>
            <span class="hand-label">THIS WEEK</span>
            <h2>本周手账</h2>
          </div>
          <DoodleIcon name="target" :size="38"/>
        </div>
        <div class="stats-list">
          <div><strong>{{ currentStats.duration }}</strong><span>学习时长</span></div>
          <div><strong>{{ currentStats.completed }}</strong><span>完成课程</span></div>
          <div><strong>{{ checkInStats.consecutiveDays }}</strong><span>连续天数</span></div>
        </div>
        <div class="mini-heatmap" aria-label="近期学习强度">
          <span v-for="(level, index) in heatmapLevels" :key="index" :class="`level-${level}`"></span>
        </div>
      </article>
    </section>

    <section class="course-section">
      <div class="section-heading">
        <div>
          <span class="hand-label">MY NOTEBOOKS</span>
          <h2>正在写的词汇本</h2>
        </div>
        <button class="text-action" type="button" @click="goToShop">去挑一本 <DoodleIcon name="arrow" :size="18"/></button>
      </div>

      <div v-if="myCourses.length" class="course-list">
        <button v-for="(course, index) in myCourses" :key="course.id" class="course-book" type="button" @click="navigateToCourse(course)">
          <span class="book-index">0{{ index + 1 }}</span>
          <span class="book-cover" :style="{ '--book-accent': bookColors[index % bookColors.length] }">
            <span class="book-binding"></span>
            <DoodleIcon :name="index % 2 ? 'book' : 'words'" :size="36"/>
            <strong>{{ course.name }}</strong>
          </span>
          <span class="book-detail">
            <strong>{{ course.lastLearnIndex || 0 }} / {{ course.length }} 词</strong>
            <span class="progress-track"><i :style="{width: `${courseProgress(course)}%`}"></i></span>
            <small>{{ course.complete ? '这一册已经写完，太强了！' : '继续写下一页' }}</small>
          </span>
          <DoodleIcon class="book-arrow" name="arrow" :size="24"/>
        </button>
      </div>

      <button v-else class="empty-notebook" type="button" @click="goToShop">
        <span class="empty-plus">+</span>
        <strong>这里还没有词汇本</strong>
        <span>挑一本喜欢的词典，开始写第一行。</span>
      </button>
    </section>
  </div>
</template>

<script lang="ts" setup>
import {computed} from 'vue'
import {useRouter} from 'vue-router'
import {useBaseStore} from '@/stores/base'
import {useUserStore} from '@/stores/user'
import type {Dict} from '@/types/types'
import DoodleIcon from '@/components/doodle/DoodleIcon.vue'

const router = useRouter()
const baseStore = useBaseStore()
const userStore = useUserStore()
const weekDays = ['一', '二', '三', '四', '五', '六', '日']
const bookColors = [
  'color-mix(in srgb, var(--pencil-red) 24%, var(--paper-card))',
  'color-mix(in srgb, var(--moss-green) 26%, var(--paper-card))',
  'color-mix(in srgb, var(--chalk-yellow) 40%, var(--paper-card))',
  'color-mix(in srgb, var(--chalk-blue) 30%, var(--paper-card))'
]

// 数据接口尚未提供学习统计，沿用原页面的展示数据，不在视觉任务中伪造新的持久化逻辑。
const checkInStats = {
  consecutiveDays: 5,
  totalDays: 48,
  weekHistory: [false, true, true, true, true, true, false]
}
const currentStats = {duration: '5h 20m', completed: 2}
const heatmapLevels = [0, 0, 1, 2, 1, 3, 2, 1, 0, 2, 3, 3, 1, 0, 0, 1, 2, 2, 1, 0, 3, 3, 2, 1, 1, 0, 1, 0]
const quotes = [
  {text: 'A different language is a different vision of life.', author: 'Federico Fellini'},
  {text: 'The limits of my language mean the limits of my world.', author: 'Ludwig Wittgenstein'},
  {text: 'Small steps every day make a language feel like home.', author: 'LexiCraft'},
  {text: 'Language is the road map of a culture.', author: 'Rita Mae Brown'}
]

const myCourses = computed(() => baseStore.word.bookList.slice(3, 7))
const primaryCourse = computed(() => myCourses.value[0])
const displayName = computed(() => userStore.user?.username || '学习者')
const dailyQuote = computed(() => quotes[Math.floor(Date.now() / 86400000) % quotes.length])
const todayIndex = computed(() => (new Date().getDay() + 6) % 7)
const currentDate = computed(() => new Intl.DateTimeFormat('zh-CN', {month: 'long', day: 'numeric', weekday: 'long'}).format(new Date()))
const greeting = computed(() => {
  const hour = new Date().getHours()
  if (hour < 11) return '早上好'
  if (hour < 18) return '下午好'
  return '晚上好'
})

const courseProgress = (course: Dict) => course.length ? Math.min(100, Math.round(((course.lastLearnIndex || 0) / course.length) * 100)) : 0
const navigateToCourse = (course: Dict) => {
  baseStore.changeDict(course)
  router.push('/app/words')
}
const goToShop = () => router.push('/app/dict-list')
const continueLearning = () => primaryCourse.value ? navigateToCourse(primaryCourse.value) : goToShop()
</script>

<style lang="scss" scoped>
.dashboard-page { max-width: 1320px; margin: 0 auto; padding: 14px 8px 40px; }
.welcome-sheet { position: relative; display: grid; min-height: 430px; overflow: hidden; padding: clamp(32px, 5vw, 70px); border: 2px solid var(--ink); border-radius: 29px 18px 36px 21px; background: var(--paper-card); box-shadow: 11px 13px 0 color-mix(in srgb, var(--ink) 14%, transparent); grid-template-columns: 1.08fr .92fr; align-items: center; }
.welcome-sheet::before { position: absolute; inset: 13px; border: 1px dashed color-mix(in srgb, var(--ink) 25%, transparent); border-radius: inherit; content: ''; pointer-events: none; }
.welcome-copy { position: relative; z-index: 2; }
.date-line, .hand-label { color: var(--pencil-red); font-family: var(--font-hand); font-size: 12px; font-weight: 800; letter-spacing: .11em; text-transform: uppercase; }
h1 { margin: 14px 0 0; font-family: var(--font-display); font-size: clamp(45px, 5.4vw, 78px); font-weight: 500; letter-spacing: -.05em; line-height: 1; }
h1 span { color: var(--pencil-red); font-family: var(--font-hand); font-size: .76em; font-weight: 650; }
.welcome-description { max-width: 590px; margin: 24px 0 0; color: var(--text-secondary); font-size: 17px; line-height: 1.75; }
.welcome-actions { display: flex; flex-wrap: wrap; gap: 14px; margin-top: 30px; }
.primary-action, .secondary-action, .text-action { display: inline-flex; align-items: center; justify-content: center; gap: 9px; cursor: pointer; font: inherit; font-weight: 800; }
.primary-action, .secondary-action { min-height: 50px; padding: 0 19px; border: 2px solid var(--ink); border-radius: 14px 11px 16px 12px; }
.primary-action { color: var(--paper-card); background: var(--ink); box-shadow: 5px 6px 0 var(--pencil-red); }
.secondary-action { color: var(--ink); background: transparent; box-shadow: 4px 5px 0 color-mix(in srgb, var(--ink) 13%, transparent); }
.primary-action:hover, .secondary-action:hover { transform: translateY(-2px) rotate(-.5deg); }
.welcome-sketch { position: relative; align-self: stretch; min-height: 300px; }
.welcome-sketch svg { width: 100%; height: 100%; filter: drop-shadow(8px 10px 0 color-mix(in srgb, var(--ink) 10%, transparent)); }
.paper-fill { fill: var(--paper-deep); }
.line { fill: none; stroke: var(--ink); stroke-width: 4.5; stroke-linecap: round; stroke-linejoin: round; }
.green { stroke: var(--moss-green); stroke-width: 9; }
.red { stroke: var(--pencil-red); stroke-width: 6; }
.yellow { fill: color-mix(in srgb, var(--chalk-yellow) 65%, transparent); stroke: #a98731; }
.welcome-sketch text { fill: var(--ink); font-family: var(--font-display); font-size: 38px; }
.sketch-tape { position: absolute; z-index: 2; top: 11px; left: 39%; width: 86px; height: 25px; border: 1px solid color-mix(in srgb, var(--ink) 15%, transparent); background: color-mix(in srgb, var(--chalk-yellow) 70%, transparent); transform: rotate(-5deg); }
.sketch-note { position: absolute; right: 1%; bottom: 1%; color: var(--moss-green); font-family: var(--font-hand); font-size: 21px; font-weight: 800; line-height: 1.1; transform: rotate(-7deg); }
.dashboard-grid { display: grid; margin-top: 34px; grid-template-columns: 1.3fr .85fr 1fr; gap: 24px; }
.paper-card { position: relative; min-height: 270px; padding: 28px; border: 2px solid var(--ink); color: var(--ink); box-shadow: 7px 8px 0 color-mix(in srgb, var(--ink) 13%, transparent); }
.streak-card { background: color-mix(in srgb, var(--pencil-red) 20%, var(--paper-card)); transform: rotate(-.5deg); }
.quote-card { background: color-mix(in srgb, var(--chalk-yellow) 42%, var(--paper-card)); transform: rotate(1deg); }
.stats-card { background: var(--paper-card); transform: rotate(-.4deg); }
.card-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 15px; }
.card-heading h2, .section-heading h2 { margin: 5px 0 0; font-family: var(--font-display); font-size: clamp(27px, 2.6vw, 39px); font-weight: 500; }
.week-grid { display: grid; margin-top: 25px; grid-template-columns: repeat(7, 1fr); gap: 7px; }
.week-day { display: flex; aspect-ratio: .78; flex-direction: column; align-items: center; justify-content: space-around; padding: 6px 2px; border: 1.6px solid var(--ink); border-radius: 7px 5px 8px 5px; font-family: var(--font-hand); font-size: 11px; transform: rotate(-1deg); }
.week-day:nth-child(2n) { transform: rotate(2deg); }
.week-day.done { color: var(--paper-card); background: var(--moss-green); }
.week-day.today { border: 3px solid var(--pencil-red); }
.week-day i { width: 9px; height: 9px; border-radius: 50%; background: var(--pencil-red); }
.streak-card > p { margin: 22px 0 0; color: var(--text-secondary); line-height: 1.65; }
.quote-pin { position: absolute; top: -9px; right: 45px; width: 17px; height: 17px; border: 2px solid var(--ink); border-radius: 50%; background: var(--pencil-red); }
.quote-card .hand-label { display: block; margin-top: 16px; }
.quote-card blockquote { margin: 15px 0 0; font-family: var(--font-display); font-size: clamp(25px, 2.3vw, 34px); line-height: 1.2; }
.quote-card cite { display: block; margin-top: 22px; color: var(--text-secondary); font-style: normal; }
.stats-list { display: grid; margin-top: 27px; grid-template-columns: repeat(3, 1fr); gap: 10px; }
.stats-list div { padding-right: 10px; border-right: 1px dashed var(--ink); }
.stats-list div:last-child { border-right: 0; }
.stats-list strong, .stats-list span { display: block; }
.stats-list strong { font-family: var(--font-display); font-size: 25px; }
.stats-list span { margin-top: 4px; color: var(--text-secondary); font-size: 11px; }
.mini-heatmap { display: grid; margin-top: 32px; grid-template-columns: repeat(14, 1fr); gap: 5px; }
.mini-heatmap span { aspect-ratio: 1; border: 1px solid color-mix(in srgb, var(--ink) 35%, transparent); border-radius: 3px 2px 4px 2px; transform: rotate(2deg); }
.mini-heatmap .level-1 { background: color-mix(in srgb, var(--moss-green) 35%, transparent); }
.mini-heatmap .level-2 { background: color-mix(in srgb, var(--moss-green) 65%, transparent); }
.mini-heatmap .level-3 { background: var(--moss-green); }
.course-section { margin-top: 60px; }
.section-heading { display: flex; align-items: flex-end; justify-content: space-between; gap: 24px; padding-bottom: 20px; border-bottom: 2px solid var(--ink); }
.text-action { padding: 10px 0; border: 0; color: var(--pencil-red); background: transparent; }
.course-list { border-bottom: 2px solid var(--ink); }
.course-book { position: relative; display: grid; width: 100%; min-height: 160px; align-items: center; padding: 22px 12px; border: 0; border-bottom: 1px dashed var(--ink); color: var(--ink); background: transparent; cursor: pointer; font: inherit; grid-template-columns: 64px 230px 1fr 45px; gap: 28px; text-align: left; }
.course-book:last-child { border-bottom: 0; }
.course-book:hover { background: color-mix(in srgb, var(--paper-card) 45%, transparent); }
.course-book:hover .book-cover { transform: translateY(-5px) rotate(-2deg); box-shadow: 7px 9px 0 color-mix(in srgb, var(--ink) 18%, transparent); }
.book-index { color: var(--pencil-red); font-family: var(--font-hand); font-weight: 800; }
.book-cover { position: relative; display: grid; min-height: 112px; padding: 18px 18px 15px 35px; overflow: hidden; border: 2px solid var(--ink); border-radius: 5px 13px 11px 5px; background: var(--book-accent); box-shadow: 4px 6px 0 color-mix(in srgb, var(--ink) 15%, transparent); transition: transform .2s ease, box-shadow .2s ease; }
.book-binding { position: absolute; inset: 0 auto 0 16px; width: 4px; border-inline: 1px solid var(--ink); opacity: .45; }
.book-cover strong { align-self: end; overflow: hidden; font-family: var(--font-display); font-size: 20px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }
.book-detail strong, .book-detail small { display: block; }
.book-detail strong { font-family: var(--font-display); font-size: 25px; }
.book-detail small { margin-top: 8px; color: var(--text-secondary); }
.progress-track { display: block; height: 8px; margin-top: 12px; overflow: hidden; border: 1.5px solid var(--ink); border-radius: 8px 5px 9px 5px; }
.progress-track i { display: block; height: 100%; background: var(--moss-green); }
.book-arrow { color: var(--pencil-red); }
.empty-notebook { display: flex; width: 100%; min-height: 190px; flex-direction: column; align-items: center; justify-content: center; gap: 8px; border: 2px dashed var(--ink); color: var(--text-secondary); background: transparent; cursor: pointer; font: inherit; }
.empty-notebook strong { color: var(--ink); font-family: var(--font-display); font-size: 26px; }
.empty-plus { display: grid; width: 45px; height: 45px; place-items: center; border: 2px solid var(--ink); border-radius: 50%; color: var(--ink); font-size: 29px; }
button:focus-visible { outline: 3px solid var(--pencil-red); outline-offset: 4px; }

@media (max-width: 1080px) {
  .welcome-sheet { grid-template-columns: 1fr; }
  .welcome-sketch { min-height: 260px; }
  .dashboard-grid { grid-template-columns: 1fr 1fr; }
  .stats-card { grid-column: 1 / -1; }
}

@media (max-width: 720px) {
  .dashboard-page { padding-inline: 0; }
  .welcome-sheet { padding: 30px 22px; border-radius: 22px 15px 25px 16px; }
  .welcome-sketch { min-height: 220px; }
  .welcome-actions { flex-direction: column; align-items: stretch; }
  .dashboard-grid { grid-template-columns: 1fr; gap: 20px; }
  .stats-card { grid-column: auto; }
  .paper-card { padding: 23px 19px; }
  .course-book { grid-template-columns: 40px 1fr 35px; gap: 13px; }
  .book-cover { min-height: 92px; }
  .book-detail { grid-column: 2 / -1; }
  .book-arrow { grid-column: 3; grid-row: 1; }
  .section-heading { align-items: flex-start; }
  .text-action { white-space: nowrap; }
}
</style>