<template>
  <div class="ink-home">
    <header class="floating-brand" aria-label="LexiCraft 首页导航">
      <button class="brand-stamp" type="button" @click="scrollToSection('hero')">
        <span class="brand-mark" aria-hidden="true">L</span>
        <span>
          <strong>LexiCraft</strong>
          <small>把词写进记忆里</small>
        </span>
      </button>

      <div class="corner-actions">
        <button class="language-pin" type="button" @click="toggleLang">
          {{ lang === 'zh' ? 'EN' : '中' }}
        </button>
        <button class="ink-link" type="button" @click="handleLogin">{{ copy.nav.login }}</button>
        <button class="ink-button ink-button--small" type="button" @click="startLearning">
          {{ copy.nav.start }}
          <DoodleIcon name="arrow" :size="18"/>
        </button>
      </div>
    </header>

    <main>
      <section id="hero" class="hero-section" aria-labelledby="hero-title">
        <div class="hero-doodle hero-doodle--left" aria-hidden="true">word<br><span>by word</span></div>
        <div class="hero-doodle hero-doodle--right" aria-hidden="true">记住它！</div>

        <div class="hero-copy">
          <p class="eyebrow"><DoodleIcon name="spark" :size="18"/> {{ copy.hero.eyebrow }}</p>
          <h1 id="hero-title">
            <span>{{ copy.hero.line1 }}</span>
            <span>{{ copy.hero.line2Prefix }} <em>{{ copy.hero.highlight }}</em></span>
          </h1>
          <p class="hero-description">{{ copy.hero.description }}</p>
          <div class="hero-actions">
            <button class="ink-button" type="button" @click="startLearning">
              {{ copy.hero.primary }}
              <DoodleIcon name="arrow" :size="21"/>
            </button>
            <button class="paper-button" type="button" @click="scrollToSection('method')">
              {{ copy.hero.secondary }}
            </button>
          </div>
        </div>

        <div class="learning-scene" aria-label="手绘学习桌插画">
          <div class="scene-tape scene-tape--left"></div>
          <div class="scene-tape scene-tape--right"></div>
          <svg class="scene-art" viewBox="0 0 1160 500" role="img" aria-label="一本打开的词汇笔记、铅笔和学习进度涂鸦">
            <path class="sketch-fill sketch-paper" d="M82 76C261 47 417 63 572 98c162-39 331-44 508-10l-22 333c-164-27-324-20-479 29-151-48-312-55-478-22Z"/>
            <path class="sketch-line" d="M82 76C261 47 417 63 572 98c162-39 331-44 508-10l-22 333c-164-27-324-20-479 29-151-48-312-55-478-22Z"/>
            <path class="sketch-line sketch-light" d="M574 101c-10 107-8 225 5 348M122 122c126-16 250-7 404 23M630 137c128-22 251-23 402-5"/>
            <path class="sketch-line" d="M146 183c69-13 131-10 218 5M146 221c98-14 202-8 334 12M146 261c111-11 231-3 348 15"/>
            <path class="sketch-line sketch-green" d="m153 320 31 29 57-68"/>
            <path class="sketch-line sketch-red" d="M301 326c51-34 152-19 168 25 16 45-44 76-109 67-63-9-96-53-59-92Z"/>
            <text class="scene-word" x="322" y="365">remember</text>
            <path class="sketch-line" d="M657 183h263M657 224h315M657 265h218"/>
            <text class="scene-note" x="668" y="345">Nice!</text>
            <path class="sketch-line sketch-yellow" d="m897 301 16 37 39 5-31 25 9 39-34-21-34 21 10-38-31-26 39-5z"/>
            <path class="sketch-line pencil" d="M1001 410 810 270l24-31 191 143 14 47Z"/>
            <path class="sketch-line" d="m1015 398 24 31-39-12"/>
            <path class="sketch-line sketch-light" d="M106 441c236-20 426-5 474 9 127-27 292-35 475-27"/>
          </svg>
          <div class="scene-caption">
            <span class="caption-dot"></span>
            {{ copy.hero.caption }}
          </div>
        </div>
      </section>

      <section id="method" class="notes-section" aria-labelledby="notes-title">
        <div class="section-heading">
          <span class="section-index">01 / DAILY RHYTHM</span>
          <h2 id="notes-title">{{ copy.notes.title }}</h2>
          <p>{{ copy.notes.description }}</p>
        </div>

        <div class="notes-board">
          <article class="quote-note">
            <span class="pin" aria-hidden="true"></span>
            <DoodleIcon name="note" :size="32"/>
            <p class="quote-label">{{ copy.notes.quoteLabel }}</p>
            <blockquote>“{{ dailyQuote.text }}”</blockquote>
            <cite>— {{ dailyQuote.author }}</cite>
            <span class="pencil-underline" aria-hidden="true"></span>
          </article>

          <article class="streak-note">
            <div class="note-header">
              <div>
                <span class="hand-label">{{ copy.notes.streakLabel }}</span>
                <strong>7 DAYS</strong>
              </div>
              <DoodleIcon name="calendar" :size="38"/>
            </div>
            <div class="doodle-calendar" aria-label="七天连续学习示意">
              <div v-for="(day, index) in weekDays" :key="day" :class="{done: index < 5, today: index === 5}" class="doodle-day">
                <span>{{ day }}</span>
                <DoodleIcon v-if="index < 5" name="check" :size="25"/>
                <span v-else-if="index === 5" class="today-dot"></span>
              </div>
            </div>
            <p>{{ copy.notes.streakTip }}</p>
          </article>

          <article class="mistake-note">
            <div class="mistake-demo">
              <span class="wrong-word">memmory</span>
              <span class="correction-arrow">↗</span>
              <span class="right-word">memory</span>
            </div>
            <h3>{{ copy.notes.mistakeTitle }}</h3>
            <p>{{ copy.notes.mistakeDescription }}</p>
          </article>
        </div>
      </section>

      <section id="features" class="features-section" aria-labelledby="features-title">
        <div class="section-heading section-heading--center">
          <span class="section-index">02 / LEARNING KIT</span>
          <h2 id="features-title">{{ copy.features.title }}</h2>
          <p>{{ copy.features.description }}</p>
        </div>

        <div class="feature-path">
          <article v-for="(feature, index) in copy.features.items" :key="feature.title" class="feature-row">
            <span class="feature-number">0{{ index + 1 }}</span>
            <div class="feature-icon" :class="`feature-icon--${index + 1}`">
              <DoodleIcon :name="feature.icon" :size="42"/>
            </div>
            <div class="feature-copy">
              <h3>{{ feature.title }}</h3>
              <p>{{ feature.description }}</p>
            </div>
            <span class="feature-scribble" aria-hidden="true">{{ feature.scribble }}</span>
          </article>
        </div>
      </section>

      <section class="final-note" aria-labelledby="final-title">
        <DoodleIcon name="spark" :size="35"/>
        <p class="hand-label">{{ copy.final.label }}</p>
        <h2 id="final-title">{{ copy.final.title }}</h2>
        <p>{{ copy.final.description }}</p>
        <button class="ink-button" type="button" @click="startLearning">
          {{ copy.final.action }}
          <DoodleIcon name="arrow" :size="21"/>
        </button>
      </section>
    </main>

    <nav class="floating-index" aria-label="页面章节">
      <button type="button" @click="scrollToSection('hero')"><DoodleIcon name="home" :size="20"/> <span>{{ copy.nav.home }}</span></button>
      <button type="button" @click="scrollToSection('method')"><DoodleIcon name="calendar" :size="20"/> <span>{{ copy.nav.rhythm }}</span></button>
      <button type="button" @click="scrollToSection('features')"><DoodleIcon name="spark" :size="20"/> <span>{{ copy.nav.features }}</span></button>
    </nav>

    <footer class="ink-footer">
      <span>LexiCraft · {{ new Date().getFullYear() }}</span>
      <span>{{ copy.footer }}</span>
    </footer>
  </div>
</template>

<script lang="ts" setup>
import {computed, ref} from 'vue'
import {useRouter} from 'vue-router'
import DoodleIcon from '@/components/doodle/DoodleIcon.vue'

const router = useRouter()
const lang = ref<'zh' | 'en'>('zh')
const weekDays = ['一', '二', '三', '四', '五', '六', '日']

const quotes = [
  {text: 'A different language is a different vision of life.', author: 'Federico Fellini'},
  {text: 'The limits of my language mean the limits of my world.', author: 'Ludwig Wittgenstein'},
  {text: 'Small steps every day make a language feel like home.', author: 'LexiCraft'},
  {text: 'Words are, in my not-so-humble opinion, our most inexhaustible source of magic.', author: 'J. K. Rowling'},
  {text: 'Language is the road map of a culture.', author: 'Rita Mae Brown'}
]

const translations = {
  zh: {
    nav: {home: '首页', rhythm: '每日节奏', features: '学习工具', login: '登录', start: '开始学习'},
    hero: {
      eyebrow: '一间有纸张温度的英语学习室',
      line1: '把陌生的单词，',
      line2Prefix: '慢慢写成',
      highlight: '你的语言',
      description: '用语境阅读、科学复习和一笔一画的即时反馈，让每次练习都像翻开一本真正属于你的学习手账。',
      primary: '写下今天的第一个词',
      secondary: '看看学习方法',
      caption: '不是冷冰冰的答题器，是会陪你变强的词汇笔记。'
    },
    notes: {
      title: '每天留下一点学习痕迹',
      description: '奖励、错题和连续学习都应该看得见。它们不是统计数字，而是你亲手写下的成长证据。',
      quoteLabel: '今日一句 · TODAY’S NOTE',
      streakLabel: '连续打卡',
      streakTip: '涂满一格就向前一步，不追求完美，只保持手感。',
      mistakeTitle: '错字也值得被认真对待',
      mistakeDescription: '用红笔圈出问题，再让正确答案自然浮现；不羞辱错误，只把它变成下一次的线索。'
    },
    features: {
      title: '把学习工具装进一张纸里',
      description: '减少层层菜单，把最重要的动作放到眼前：读、记、练、回看。',
      items: [
        {title: '在语境里遇见单词', description: '从文章和例句进入词义，而不是孤立地背诵列表。', scribble: 'read it', icon: 'book' as const},
        {title: '按记忆节奏回来复习', description: '让需要复习的内容主动出现，把精力留给真正容易遗忘的词。', scribble: 'again!', icon: 'target' as const},
        {title: '获得有人情味的反馈', description: '答对时给你一句真诚鼓励，答错时清楚标注正确答案。', scribble: 'Nice!', icon: 'spark' as const}
      ]
    },
    final: {label: 'READY WHEN YOU ARE', title: '今天，先认真记住一个词。', description: '不用等完整计划，也不用一次学很多。打开你的词汇本，从下一笔开始。', action: '进入我的学习桌'},
    footer: 'Made with paper, ink and a little courage.'
  },
  en: {
    nav: {home: 'Home', rhythm: 'Daily rhythm', features: 'Learning kit', login: 'Sign in', start: 'Start'},
    hero: {
      eyebrow: 'A warmer place to build your English',
      line1: 'Turn unfamiliar words',
      line2Prefix: 'into',
      highlight: 'your language',
      description: 'Learn through context, thoughtful review and human feedback — like keeping a notebook that grows with you.',
      primary: 'Write down your first word',
      secondary: 'See the method',
      caption: 'Not a cold quiz machine. A vocabulary notebook that roots for you.'
    },
    notes: {
      title: 'Leave a visible learning trace',
      description: 'Rewards, mistakes and streaks should feel tangible — proof of growth written by your own hand.',
      quoteLabel: 'TODAY’S NOTE',
      streakLabel: 'Learning streak',
      streakTip: 'Fill one square and move forward. Keep the rhythm, not perfection.',
      mistakeTitle: 'Mistakes deserve careful attention',
      mistakeDescription: 'Circle the problem in red, then reveal the right answer without turning an error into punishment.'
    },
    features: {
      title: 'A learning kit on one sheet of paper',
      description: 'Fewer layers, clearer actions: read, remember, practise and look back.',
      items: [
        {title: 'Meet words in context', description: 'Begin with stories and examples instead of isolated vocabulary lists.', scribble: 'read it', icon: 'book' as const},
        {title: 'Return at the right moment', description: 'Spend your attention on the words most likely to fade.', scribble: 'again!', icon: 'target' as const},
        {title: 'Get feedback with a pulse', description: 'A real cheer when you are right and a clear correction when you miss.', scribble: 'Nice!', icon: 'spark' as const}
      ]
    },
    final: {label: 'READY WHEN YOU ARE', title: 'Today, remember one word well.', description: 'No perfect plan required. Open your notebook and begin with the next mark.', action: 'Enter my learning desk'},
    footer: 'Made with paper, ink and a little courage.'
  }
}

const copy = computed(() => translations[lang.value])
const dailyQuote = computed(() => quotes[Math.floor(Date.now() / 86400000) % quotes.length])

const toggleLang = () => {
  lang.value = lang.value === 'zh' ? 'en' : 'zh'
}

const startLearning = () => router.push('/app/dashboard')
const handleLogin = () => router.push('/login')

const scrollToSection = (id: string) => {
  document.getElementById(id)?.scrollIntoView({behavior: 'smooth'})
}
</script>

<style lang="scss" scoped>
.ink-home {
  min-height: 100vh;
  overflow: hidden;
  color: var(--ink);
  background:
      linear-gradient(rgba(32, 39, 35, 0.024) 1px, transparent 1px),
      linear-gradient(90deg, rgba(32, 39, 35, 0.018) 1px, transparent 1px),
      var(--paper);
  background-size: 34px 34px;
  font-family: var(--font-family);
}

button { font: inherit; }

.floating-brand {
  position: absolute;
  inset: 0 0 auto;
  z-index: 20;
  pointer-events: none;
}

.brand-stamp,
.corner-actions {
  position: absolute;
  top: 26px;
  pointer-events: auto;
}

.brand-stamp {
  left: clamp(20px, 4vw, 64px);
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 0;
  color: var(--ink);
  background: transparent;
  border: 0;
  cursor: pointer;
  text-align: left;
  transform: rotate(-1deg);
}

.brand-mark {
  display: grid;
  width: 42px;
  height: 42px;
  place-items: center;
  color: var(--paper-card);
  background: var(--ink);
  border: 2px solid var(--ink);
  border-radius: 51% 45% 48% 43%;
  font-family: var(--font-display);
  font-size: 27px;
  font-style: italic;
  box-shadow: 3px 3px 0 var(--pencil-red);
}

.brand-stamp strong { display: block; font-family: var(--font-display); font-size: 19px; }
.brand-stamp small { display: block; margin-top: 1px; color: var(--ink-soft); font-size: 11px; letter-spacing: .08em; }

.corner-actions {
  right: clamp(20px, 4vw, 64px);
  display: flex;
  align-items: center;
  gap: 10px;
}

.language-pin,
.ink-link {
  border: 0;
  color: var(--ink);
  background: transparent;
  cursor: pointer;
}

.language-pin {
  width: 40px;
  height: 40px;
  border: 1.8px solid var(--ink);
  border-radius: 48% 52% 44% 56%;
  font-family: var(--font-hand);
  font-weight: 700;
  transform: rotate(3deg);
}

.ink-link { padding: 10px 12px; font-weight: 700; }
.ink-link:hover { color: var(--pencil-red); }

.ink-button,
.paper-button {
  display: inline-flex;
  min-height: 52px;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 0 24px;
  border: 2px solid var(--ink);
  border-radius: 16px 13px 18px 12px;
  cursor: pointer;
  font-weight: 800;
  transition: transform .2s ease, box-shadow .2s ease, background .2s ease;
}

.ink-button {
  color: var(--paper-card);
  background: var(--ink);
  box-shadow: 5px 6px 0 rgba(217, 87, 69, .72);
}

.ink-button:hover { transform: translate(-2px, -2px) rotate(-1deg); box-shadow: 8px 9px 0 rgba(217, 87, 69, .72); }
.ink-button--small { min-height: 44px; padding: 0 18px; box-shadow: 3px 4px 0 rgba(217, 87, 69, .72); }
.paper-button { color: var(--ink); background: color-mix(in srgb, var(--paper-card) 45%, transparent); box-shadow: 4px 5px 0 rgba(32, 39, 35, .12); }
.paper-button:hover { transform: translateY(-2px) rotate(1deg); background: var(--paper-card); }

.hero-section {
  position: relative;
  display: flex;
  min-height: 100vh;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 132px clamp(20px, 6vw, 96px) 80px;
}

.hero-copy { position: relative; z-index: 2; width: min(1050px, 100%); text-align: center; }
.eyebrow { display: inline-flex; align-items: center; gap: 8px; margin: 0 0 22px; color: var(--ink-soft); font-size: 13px; font-weight: 800; letter-spacing: .13em; text-transform: uppercase; }

h1 {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(56px, 8.7vw, 132px);
  font-weight: 500;
  letter-spacing: -.055em;
  line-height: .92;
}

h1 span { display: block; }
h1 em { position: relative; color: var(--pencil-red); font-family: var(--font-hand); font-size: 1.02em; font-weight: 600; letter-spacing: -.04em; }
h1 em::after { position: absolute; right: -2%; bottom: -9px; left: 0; height: 11px; border-top: 3px solid currentColor; border-radius: 50%; content: ''; transform: rotate(-2deg); opacity: .7; }

.hero-description { max-width: 680px; margin: 28px auto 0; color: var(--ink-soft); font-size: clamp(16px, 1.4vw, 20px); line-height: 1.75; }
.hero-actions { display: flex; flex-wrap: wrap; justify-content: center; gap: 18px; margin-top: 32px; }

.hero-doodle { position: absolute; color: var(--pencil-red); font-family: var(--font-hand); font-weight: 700; opacity: .8; pointer-events: none; }
.hero-doodle--left { top: 28%; left: 5%; font-size: clamp(18px, 2.2vw, 31px); transform: rotate(-10deg); }
.hero-doodle--left span { margin-left: 25px; }
.hero-doodle--right { top: 36%; right: 6%; font-size: clamp(18px, 2vw, 28px); transform: rotate(8deg); }
.hero-doodle--right::after { display: block; width: 70px; height: 28px; margin-left: -20px; border-bottom: 3px solid currentColor; border-radius: 50%; content: ''; transform: rotate(-12deg); }

.learning-scene {
  position: relative;
  width: min(1160px, 92vw);
  margin-top: 62px;
  padding: 22px 24px 15px;
  border: 2px solid var(--ink);
  border-radius: 26px 17px 31px 19px;
  background: color-mix(in srgb, var(--paper-deep) 82%, var(--chalk-yellow));
  box-shadow: 13px 15px 0 rgba(32, 39, 35, .14);
  transform: rotate(-.5deg);
}

.learning-scene::before { position: absolute; inset: 10px; border: 1px dashed rgba(32, 39, 35, .28); border-radius: inherit; content: ''; pointer-events: none; }
.scene-art { position: relative; display: block; width: 100%; filter: drop-shadow(0 12px 10px rgba(32, 39, 35, .12)); }
.scene-tape { position: absolute; z-index: 3; width: 105px; height: 28px; background: rgba(246, 230, 168, .72); border: 1px solid rgba(32, 39, 35, .16); }
.scene-tape--left { top: -12px; left: 10%; transform: rotate(-5deg); }
.scene-tape--right { top: -10px; right: 9%; transform: rotate(6deg); }
.sketch-fill { stroke: none; }
.sketch-paper { fill: var(--paper-card); }
.sketch-line { fill: none; stroke: var(--ink); stroke-width: 5; stroke-linecap: round; stroke-linejoin: round; }
.sketch-light { opacity: .45; stroke-width: 3; }
.sketch-green { stroke: var(--moss-green); stroke-width: 9; }
.sketch-red { stroke: var(--pencil-red); stroke-width: 7; }
.sketch-yellow { fill: rgba(231, 199, 95, .5); stroke: #b28b2f; }
.pencil { fill: rgba(217, 87, 69, .15); stroke: var(--pencil-red); }
.scene-word { fill: var(--ink); font-family: var(--font-display); font-size: 44px; }
.scene-note { fill: var(--moss-green); font-family: var(--font-hand); font-size: 58px; font-weight: 700; transform: rotate(-5deg); transform-origin: center; }
.scene-caption { position: absolute; right: 34px; bottom: 18px; display: flex; align-items: center; gap: 9px; padding: 10px 14px; border: 1.5px solid var(--ink); background: var(--chalk-yellow); font-family: var(--font-hand); font-size: 13px; font-weight: 700; transform: rotate(-1.5deg); }
.caption-dot { width: 8px; height: 8px; border-radius: 50%; background: var(--pencil-red); }

.notes-section,
.features-section { padding: 120px clamp(20px, 6vw, 96px); }
.notes-section { background: var(--paper-deep); border-block: 2px solid var(--ink); }
.section-heading { max-width: 740px; }
.section-heading--center { margin-inline: auto; text-align: center; }
.section-index { display: block; margin-bottom: 12px; color: var(--pencil-red); font-family: var(--font-hand); font-size: 13px; font-weight: 800; letter-spacing: .16em; }
.section-heading h2,
.final-note h2 { margin: 0; font-family: var(--font-display); font-size: clamp(42px, 6vw, 82px); font-weight: 500; letter-spacing: -.045em; line-height: 1.02; }
.section-heading p { max-width: 650px; margin: 20px 0 0; color: var(--ink-soft); font-size: 18px; line-height: 1.75; }
.section-heading--center p { margin-inline: auto; }

.notes-board { display: grid; max-width: 1220px; margin: 68px auto 0; grid-template-columns: 1.05fr 1.4fr .95fr; gap: 26px; align-items: stretch; }
.quote-note,
.streak-note,
.mistake-note { position: relative; min-height: 300px; padding: 32px; border: 2px solid var(--ink); color: var(--ink); box-shadow: 8px 9px 0 rgba(32, 39, 35, .13); }
.quote-note { background: color-mix(in srgb, var(--chalk-yellow) 42%, var(--paper-card)); transform: rotate(-1.5deg); }
.streak-note { background: var(--paper-card); transform: rotate(.6deg); }
.mistake-note { background: color-mix(in srgb, var(--pencil-red) 24%, var(--paper-card)); transform: rotate(1.5deg); }
.pin { position: absolute; top: -9px; left: 50%; width: 16px; height: 16px; border: 2px solid var(--ink); border-radius: 50%; background: var(--pencil-red); box-shadow: 2px 3px 0 rgba(32, 39, 35, .25); }
.quote-label,
.hand-label { margin: 15px 0 11px; font-family: var(--font-hand); font-size: 13px; font-weight: 800; letter-spacing: .08em; }
.quote-note blockquote { margin: 0; font-family: var(--font-display); font-size: clamp(25px, 2.3vw, 34px); line-height: 1.22; }
.quote-note cite { display: block; margin-top: 20px; color: var(--ink-soft); font-style: normal; }
.pencil-underline { position: absolute; right: 28px; bottom: 32px; width: 90px; height: 18px; border-bottom: 4px solid var(--pencil-red); border-radius: 50%; transform: rotate(-5deg); }
.note-header { display: flex; align-items: flex-start; justify-content: space-between; }
.note-header strong { display: block; margin-top: 5px; font-family: var(--font-display); font-size: 38px; }
.doodle-calendar { display: grid; margin-top: 30px; grid-template-columns: repeat(7, 1fr); gap: 9px; }
.doodle-day { display: flex; aspect-ratio: .82; flex-direction: column; align-items: center; justify-content: space-around; padding: 7px 2px; border: 1.7px solid var(--ink); border-radius: 8px 5px 9px 6px; background: transparent; font-family: var(--font-hand); font-size: 12px; transform: rotate(-1deg); }
.doodle-day:nth-child(2n) { transform: rotate(2deg); }
.doodle-day.done { color: var(--paper-card); background: var(--moss-green); }
.doodle-day.today { border: 3px solid var(--pencil-red); }
.today-dot { width: 12px; height: 12px; border-radius: 50%; background: var(--pencil-red); }
.streak-note > p { margin: 22px 0 0; color: var(--ink-soft); line-height: 1.6; }
.mistake-demo { display: flex; min-height: 132px; align-items: center; justify-content: center; gap: 8px; font-family: var(--word-font-family); font-size: clamp(20px, 2.2vw, 30px); }
.wrong-word { position: relative; color: var(--pencil-red); }
.wrong-word::after { position: absolute; inset: -13px -11px; border: 3px solid var(--pencil-red); border-radius: 48% 54% 51% 45%; content: ''; transform: rotate(-5deg); }
.correction-arrow { color: var(--pencil-red); font-family: var(--font-hand); font-size: 35px; transform: translateY(-25px) rotate(-12deg); }
.right-word { color: var(--moss-green); font-family: var(--font-hand); font-weight: 800; transform: rotate(-5deg); }
.mistake-note h3 { margin: 14px 0 8px; font-family: var(--font-display); font-size: 28px; }
.mistake-note p { margin: 0; color: var(--ink-soft); line-height: 1.65; }

.features-section { background: var(--paper); }
.feature-path { max-width: 1040px; margin: 70px auto 0; border-top: 2px solid var(--ink); }
.feature-row { position: relative; display: grid; min-height: 180px; align-items: center; grid-template-columns: 70px 100px 1fr 150px; gap: 26px; border-bottom: 2px solid var(--ink); }
.feature-number { color: var(--pencil-red); font-family: var(--font-hand); font-size: 17px; font-weight: 800; }
.feature-icon { display: grid; width: 82px; height: 82px; place-items: center; border: 2px solid var(--ink); border-radius: 43% 57% 48% 52%; box-shadow: 5px 5px 0 rgba(32, 39, 35, .15); transform: rotate(-3deg); }
.feature-icon--1 { background: color-mix(in srgb, var(--pencil-red) 22%, var(--paper-card)); }
.feature-icon--2 { background: color-mix(in srgb, var(--moss-green) 28%, var(--paper-card)); transform: rotate(2deg); }
.feature-icon--3 { background: color-mix(in srgb, var(--chalk-yellow) 38%, var(--paper-card)); }
.feature-copy h3 { margin: 0; font-family: var(--font-display); font-size: clamp(27px, 3vw, 40px); font-weight: 500; }
.feature-copy p { margin: 8px 0 0; color: var(--ink-soft); line-height: 1.65; }
.feature-scribble { color: var(--pencil-red); font-family: var(--font-hand); font-size: 27px; font-weight: 800; text-align: center; transform: rotate(-7deg); }

.final-note { max-width: 1000px; margin: 10px auto 130px; padding: 85px clamp(28px, 7vw, 90px); border: 2px solid var(--ink); background: var(--paper-card); box-shadow: 13px 15px 0 rgba(32, 39, 35, .14); text-align: center; transform: rotate(-.5deg); }
.final-note > p:not(.hand-label) { max-width: 650px; margin: 20px auto 30px; color: var(--ink-soft); font-size: 18px; line-height: 1.7; }
.final-note .ink-button { margin-inline: auto; }

.floating-index { position: fixed; z-index: 30; right: 50%; bottom: 20px; display: flex; padding: 8px; border: 2px solid var(--ink); border-radius: 18px 14px 20px 16px; background: color-mix(in srgb, var(--paper-card) 91%, transparent); box-shadow: 6px 7px 0 rgba(32, 39, 35, .18); backdrop-filter: blur(12px); transform: translateX(50%); }
.floating-index button { display: flex; align-items: center; gap: 7px; padding: 10px 15px; border: 0; border-radius: 11px; color: var(--ink); background: transparent; cursor: pointer; font-size: 13px; font-weight: 800; }
.floating-index button:hover { color: var(--paper-card); background: var(--ink); }
.ink-footer { display: flex; justify-content: space-between; gap: 20px; padding: 28px clamp(20px, 6vw, 96px) 92px; border-top: 1px dashed var(--ink); color: var(--ink-soft); font-size: 13px; }

button:focus-visible,
a:focus-visible { outline: 3px solid var(--pencil-red); outline-offset: 4px; }

@media (max-width: 980px) {
  .notes-board { grid-template-columns: 1fr 1fr; }
  .mistake-note { grid-column: 1 / -1; }
  .feature-row { grid-template-columns: 55px 90px 1fr; padding-block: 24px; }
  .feature-scribble { display: none; }
  .hero-doodle { display: none; }
}

@media (max-width: 720px) {
  .brand-stamp small,
  .ink-link,
  .corner-actions .ink-button { display: none; }
  .brand-stamp, .corner-actions { top: 18px; }
  .hero-section { min-height: auto; padding-top: 125px; }
  h1 { font-size: clamp(42px, 11vw, 68px); line-height: .98; }
  h1 span { white-space: nowrap; }
  .hero-description { font-size: 16px; }
  .hero-actions { flex-direction: column; align-items: stretch; }
  .learning-scene { width: 100%; margin-top: 45px; padding: 10px; border-radius: 18px; }
  .scene-caption { position: relative; right: auto; bottom: auto; margin: -5px 8px 5px; }
  .notes-section, .features-section { padding: 82px 18px; }
  .notes-board { grid-template-columns: 1fr; }
  .mistake-note { grid-column: auto; }
  .quote-note, .streak-note, .mistake-note { min-height: 0; }
  .doodle-calendar { gap: 5px; }
  .doodle-day { min-width: 0; }
  .feature-row { grid-template-columns: 46px 70px 1fr; gap: 14px; }
  .feature-icon { width: 60px; height: 60px; }
  .floating-index { bottom: 10px; width: calc(100% - 24px); justify-content: space-around; }
  .floating-index button { flex-direction: column; gap: 2px; padding: 6px 12px; font-size: 10px; }
  .ink-footer { flex-direction: column; padding-bottom: 100px; }
  .final-note { margin: 0 18px 110px; padding-block: 60px; }
}

@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after { scroll-behavior: auto !important; transition-duration: .01ms !important; }
}
</style>