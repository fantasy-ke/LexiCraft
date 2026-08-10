<template>
  <div class="public-editorial editorial-home">
    <header class="reading-header" aria-label="LexiCraft 阅读首页">
      <button aria-label="??????" class="wordmark" type="button" @click="scrollToSection('opening')">
        <BrandLogo tagline="Language, carefully made."/>
      </button>
      <nav class="header-links" aria-label="主导航">
        <button type="button" @click="scrollToSection('method')">学习方法</button>
        <button type="button" @click="scrollToSection('experience')">阅读体验</button>
      </nav>
      <div class="header-actions">
        <button class="language-button" type="button" @click="toggleLang">{{ lang === 'zh' ? 'EN' : '中' }}</button>
        <button class="text-button" type="button" @click="handleLogin">{{ copy.nav.login }}</button>
        <button class="solid-button solid-button--small" type="button" @click="startLearning">{{ copy.nav.start }} <span>→</span></button>
      </div>
    </header>

    <main>
      <section id="opening" class="opening" aria-labelledby="home-title">
        <p class="edition-line"><span>ISSUE 01</span>{{ copy.hero.eyebrow }}<span>{{ currentDate }}</span></p>
        <h1 id="home-title">
          <span>{{ copy.hero.line1 }}</span>
          <span>{{ copy.hero.line2Prefix }} <em>{{ copy.hero.highlight }}</em></span>
        </h1>
        <p class="opening-deck">{{ copy.hero.description }}</p>
        <div class="opening-actions">
          <button class="solid-button" type="button" @click="startLearning">{{ copy.hero.primary }} <span>→</span></button>
          <button class="underlined-button" type="button" @click="scrollToSection('method')">{{ copy.hero.secondary }}</button>
        </div>

        <figure class="reading-preview">
          <div class="preview-topline">
            <span>LEXICRAFT / TODAY'S READING</span>
            <span>VOL. 07</span>
          </div>
          <div class="book-spread">
            <article class="book-page book-page--left">
              <p class="page-folio">12</p>
              <p class="page-kicker">A WORD IN CONTEXT</p>
              <h2>serendipity</h2>
              <p class="phonetic">/ˌser.ənˈdɪp.ə.ti/</p>
              <p class="definition">意外发现美好事物的幸运与惊喜。</p>
              <blockquote>“Learning a word is not collecting a label. It is meeting a new way to notice the world.”</blockquote>
              <div class="margin-rule"><span></span><span></span><span></span></div>
            </article>
            <article class="book-page book-page--right">
              <p class="page-folio">13</p>
              <p class="page-kicker">YOUR NOTE</p>
              <p class="hand-note">把今天学会的词，<br/>写进自己的生活。</p>
              <div class="sentence-lines">
                <span>We found the little bookshop by pure</span>
                <strong>serendipity.</strong>
              </div>
              <div class="editorial-stamp"><span>DAY</span><strong>07</strong><small>KEEP READING</small></div>
            </article>
          </div>
          <figcaption>{{ copy.hero.caption }}</figcaption>
        </figure>
      </section>

      <section id="method" class="reading-chapter" aria-labelledby="method-title">
        <aside class="chapter-aside">
          <span>CHAPTER</span><strong>01</strong><small>Daily rhythm</small>
        </aside>
        <div class="chapter-body">
          <p class="chapter-kicker">{{ copy.notes.quoteLabel }}</p>
          <blockquote class="daily-quote">“{{ dailyQuote.text }}”</blockquote>
          <p class="quote-author">— {{ dailyQuote.author }}</p>
          <h2 id="method-title">{{ copy.notes.title }}</h2>
          <p class="chapter-intro">{{ copy.notes.description }}</p>

          <ol class="learning-flow">
            <li>
              <span class="flow-number">I</span>
              <div><strong>先读语境</strong><p>不急着背释义，先看它如何在一句话里生长。</p></div>
              <em>read</em>
            </li>
            <li>
              <span class="flow-number">II</span>
              <div><strong>再做练习</strong><p>用输入和判断完成一次简短、有节奏的回忆。</p></div>
              <em>review</em>
            </li>
            <li>
              <span class="flow-number">III</span>
              <div><strong>留下痕迹</strong><p>让错题、笔记和连续学习天数成为你的阅读记录。</p></div>
              <em>notice</em>
            </li>
          </ol>
        </div>
      </section>

      <section id="experience" class="editorial-feature" aria-labelledby="experience-title">
        <div class="feature-copy">
          <p class="chapter-kicker">A QUIETER LEARNING TOOL</p>
          <h2 id="experience-title">{{ copy.features.title }}</h2>
          <p>{{ copy.features.description }}</p>
          <button class="underlined-button" type="button" @click="startLearning">开始今天的学习 <span>→</span></button>
        </div>
        <div class="feature-manuscript">
          <article v-for="(item, index) in copy.features.items" :key="item.title">
            <span>{{ String(index + 1).padStart(2, '0') }}</span>
            <div><h3>{{ item.title }}</h3><p>{{ item.description }}</p></div>
            <em>{{ item.scribble }}</em>
          </article>
        </div>
      </section>

      <section class="closing-page" aria-labelledby="closing-title">
        <span class="closing-mark">L</span>
        <p>{{ copy.final.label }}</p>
        <h2 id="closing-title">{{ copy.final.title }}</h2>
        <div class="closing-rule"></div>
        <p class="closing-description">{{ copy.final.description }}</p>
        <button class="solid-button" type="button" @click="startLearning">{{ copy.final.action }} <span>→</span></button>
      </section>
    </main>

    <footer class="reading-footer">
      <span>LexiCraft © {{ new Date().getFullYear() }}</span>
      <span>{{ copy.footer }}</span>
    </footer>
  </div>
</template>

<script lang="ts" setup>
import {computed, ref} from 'vue'
import {useRouter} from 'vue-router'
import BrandLogo from '@/components/BrandLogo.vue'

const router = useRouter()
const lang = ref<'zh' | 'en'>('zh')
const quotes = [
  {text: 'A different language is a different vision of life.', author: 'Federico Fellini'},
  {text: 'The limits of my language mean the limits of my world.', author: 'Ludwig Wittgenstein'},
  {text: 'Small steps every day make a language feel like home.', author: 'LexiCraft'},
  {text: 'Language is the road map of a culture.', author: 'Rita Mae Brown'}
]
const translations = {
  zh: {
    nav: {login: '登录', start: '开始学习'},
    hero: {eyebrow: '一本值得每天翻阅的语言手记', line1: '让每一个词', line2Prefix: '都成为', highlight: '你的语言', description: '温暖的纸张、有意义的语境和恰到好处的复习，让学习从待办清单变成每日阅读仪式。', primary: '翻开今日书页', secondary: '阅读学习方法', caption: '词汇、阅读与练习被收进同一本随你成长的语言书。'},
    notes: {quoteLabel: '每日一句 · TODAY’S PASSAGE', title: '学习可以很安静，也可以留下痕迹。', description: 'LexiCraft 把学习编排成阅读流：遇见一个词，在语境里理解它，然后在恰当的时刻重新相遇。进度不会喧哗，只会轻轻告诉你已经读了多远。'},
    features: {title: '像使用一本好书那样使用学习工具。', description: '少一些卡片网格和嵌套菜单，让内容、行动与反馈自然地向下流动。', items: [
      {title: '先有语境，再记忆', description: '先看见一个词如何生活在句子里，再决定如何记住它。', scribble: 'Context'},
      {title: '用节奏带回记忆', description: '需要关注的内容会自然返回，少一些选择，也少一些噪音。', scribble: 'Rhythm'},
      {title: '让反馈保有温度', description: '认真回应每一次完成，也清楚标记每一个需要重新理解的错误。', scribble: 'Care'}
    ]},
    final: {label: 'THE NEXT PAGE IS YOURS', title: '今天，从认真读好一个词开始。', description: '不需要完美计划。走进你的学习空间，让下一页现在开始。', action: '进入 LexiCraft'},
    footer: 'A quiet place for words, reading and memory.'
  },
  en: {
    nav: {login: 'Sign in', start: 'Start learning'},
    hero: {eyebrow: 'A language journal worth returning to', line1: 'Let every word', line2Prefix: 'become part of', highlight: 'your language', description: 'Warm paper, meaningful context and thoughtful review turn study from a checklist into a daily reading ritual.', primary: 'Open today’s page', secondary: 'Read the method', caption: 'Vocabulary, reading and practice gathered into one language book that keeps growing with you.'},
    notes: {quoteLabel: 'TODAY’S PASSAGE', title: 'Learning can be quiet and still leave a trace.', description: 'LexiCraft arranges study as a reading flow: meet a word, understand it in context, then return at the right moment. Progress never shouts. It simply shows how far you have read.'},
    features: {title: 'Use a learning tool like a well-made book.', description: 'Fewer card grids and nested menus. Content, action and feedback follow a natural reading order.', items: [
      {title: 'Context before memory', description: 'See how a word lives in a sentence before deciding how to remember it.', scribble: 'Context'},
      {title: 'Review with rhythm', description: 'What needs attention returns naturally, with less choosing and less noise.', scribble: 'Rhythm'},
      {title: 'Feedback with warmth', description: 'Success is acknowledged and mistakes are annotated with clarity.', scribble: 'Care'}
    ]},
    final: {label: 'THE NEXT PAGE IS YOURS', title: 'Begin today by reading one word well.', description: 'No perfect plan required. Enter your learning room and let the next page begin now.', action: 'Enter LexiCraft'},
    footer: 'A quiet place for words, reading and memory.'
  }
}

const copy = computed(() => translations[lang.value])
const dailyQuote = computed(() => quotes[Math.floor(Date.now() / 86400000) % quotes.length])
const currentDate = computed(() => new Intl.DateTimeFormat(lang.value === 'zh' ? 'zh-CN' : 'en-US', {month: 'short', day: '2-digit'}).format(new Date()))
const toggleLang = () => { lang.value = lang.value === 'zh' ? 'en' : 'zh' }
const startLearning = () => router.push('/app/dashboard')
const handleLogin = () => router.push('/login')
const scrollToSection = (id: string) => document.getElementById(id)?.scrollIntoView({behavior: 'smooth'})
</script>

<style lang="scss" scoped>
.editorial-home { min-height: 100vh; overflow: hidden; color: var(--text-primary); background-color: var(--surface-page); background-image: radial-gradient(circle at 16% 20%, color-mix(in srgb, var(--text-primary) 4%, transparent) 0 .7px, transparent .9px), radial-gradient(circle at 80% 70%, color-mix(in srgb, var(--text-primary) 3%, transparent) 0 .7px, transparent .9px); background-size: 22px 22px, 31px 31px; font-family: var(--font-editorial); }
button { font: inherit; }
.reading-header { position: absolute; z-index: 20; top: 0; right: 0; left: 0; display: grid; grid-template-columns: 1fr auto 1fr; align-items: center; gap: 2rem; padding: 24px clamp(20px, 5vw, 76px); }
.wordmark { display: flex; align-items: center; justify-self: start; gap: 11px; padding: 0; border: 0; color: var(--text-primary); background: transparent; cursor: pointer; text-align: left; }
.wordmark :deep(.brand-logo__mark) { width: 39px; height: 39px; }
.wordmark :deep(.brand-logo__copy strong) { font-size: 17px; }
.wordmark :deep(.brand-logo__copy small) { font-family: var(--font-sans); }
.header-links { display: flex; gap: 28px; }
.header-links button, .text-button, .language-button { padding: 5px 0; border: 0; color: var(--text-secondary); background: transparent; cursor: pointer; font-family: var(--font-sans); font-size: 12px; }
.header-links button:hover, .text-button:hover { color: var(--accent); }
.header-actions { display: flex; align-items: center; justify-self: end; gap: 16px; }
.language-button { width: 34px; height: 34px; padding: 0; border: 1px solid var(--border-color); border-radius: 50%; color: var(--text-primary); }
.solid-button { display: inline-flex; align-items: center; justify-content: center; gap: 18px; min-height: 50px; padding: 0 24px; border: 1px solid var(--text-primary); border-radius: 2px; color: var(--accent-contrast); background: var(--text-primary); box-shadow: var(--control-shadow); cursor: pointer; font-family: var(--font-sans); font-size: 13px; font-weight: 700; transition: transform .2s ease, background .2s ease; }
.solid-button:hover { background: var(--accent); transform: translateY(-2px); }
.solid-button--small { min-height: 42px; padding-inline: 18px; }
.underlined-button { padding: 8px 0; border: 0; border-bottom: 1px solid currentColor; color: var(--text-primary); background: transparent; cursor: pointer; font-family: var(--font-sans); font-size: 13px; }
.opening { min-height: 100vh; padding: 150px clamp(22px, 7vw, 112px) 110px; text-align: center; }
.edition-line { display: flex; max-width: 820px; align-items: center; justify-content: center; gap: 14px; margin: 0 auto 26px; color: var(--text-tertiary); font-family: var(--font-sans); font-size: 10px; letter-spacing: .15em; text-transform: uppercase; }
.edition-line span { display: flex; align-items: center; gap: 14px; }
.edition-line span:first-child::after, .edition-line span:last-child::before { content: ''; width: 42px; height: 1px; background: var(--border-color); }
h1 { max-width: 1060px; margin: 0 auto; font-size: clamp(62px, 8vw, 132px); font-weight: 400; letter-spacing: -.055em; line-height: .86; }
h1 span { display: block; }
h1 em { color: var(--accent); font-weight: 400; }
.opening-deck { max-width: 650px; margin: 35px auto 0; color: var(--text-secondary); font-family: var(--font-sans); font-size: clamp(15px, 1.5vw, 18px); line-height: 1.8; }
.opening-actions { display: flex; align-items: center; justify-content: center; gap: 28px; margin-top: 30px; }
.reading-preview { max-width: 1180px; margin: 88px auto 0; border: 1px solid var(--border-strong); background: var(--surface-muted); box-shadow: 0 34px 75px rgba(70, 50, 36, .16); text-align: left; }
.preview-topline { display: flex; justify-content: space-between; padding: 12px 18px; border-bottom: 1px solid var(--border-strong); color: var(--text-secondary); font-family: var(--font-sans); font-size: 9px; letter-spacing: .14em; }
.book-spread { display: grid; grid-template-columns: 1fr 1fr; padding: clamp(22px, 5vw, 70px); }
.book-page { position: relative; min-height: 430px; padding: clamp(28px, 5vw, 64px); color: #2b241f; background: #fffaf0; box-shadow: 0 20px 45px rgba(70, 50, 36, .13); }
.book-page--left { border-right: 1px solid #ded1bd; }
.book-page--right { background: #fbf2e4; }
.page-folio { position: absolute; top: 22px; right: 28px; margin: 0; color: #9a8d7d; font-size: 11px; }
.page-kicker { margin: 0 0 20px; color: #9b4a3c; font-family: var(--font-sans); font-size: 9px; font-weight: 800; letter-spacing: .16em; }
.book-page h2 { margin: 0; font-size: clamp(38px, 5vw, 70px); font-weight: 400; letter-spacing: -.04em; }
.phonetic { color: #75685c; font-family: var(--font-mono); font-size: 12px; }
.definition { max-width: 33rem; margin-top: 28px; color: #4f463e; font-size: 17px; line-height: 1.8; }
.book-page blockquote { margin: 34px 0 0; padding-left: 20px; border-left: 2px solid #9b4a3c; color: #75685c; font-style: italic; line-height: 1.7; }
.margin-rule { display: grid; gap: 12px; margin-top: 34px; }
.margin-rule span { height: 1px; background: #ded4c5; }
.hand-note { margin: 55px 0 0; color: #9b4a3c; font-size: clamp(28px, 4vw, 52px); font-style: italic; line-height: 1.25; transform: rotate(-2deg); }
.sentence-lines { display: grid; gap: 8px; margin-top: 58px; padding-block: 16px; border-block: 1px solid #d8cbb8; font-size: 17px; }
.editorial-stamp { position: absolute; right: 42px; bottom: 42px; display: grid; width: 92px; height: 92px; place-content: center; border: 2px double #9b4a3c; border-radius: 50%; color: #9b4a3c; text-align: center; transform: rotate(-8deg); }
.editorial-stamp span, .editorial-stamp small { font-family: var(--font-sans); font-size: 7px; letter-spacing: .12em; }
.editorial-stamp strong { font-size: 27px; line-height: 1; }
.reading-preview figcaption { padding: 14px 20px; border-top: 1px solid var(--border-strong); color: var(--text-secondary); font-family: var(--font-sans); font-size: 11px; text-align: center; }
.reading-chapter { display: grid; max-width: 1180px; grid-template-columns: 150px 1fr; gap: clamp(36px, 8vw, 110px); margin: 0 auto; padding: 145px clamp(22px, 4vw, 50px); }
.chapter-aside { display: flex; flex-direction: column; align-items: center; align-self: start; padding-block: 24px; border-block: 1px solid var(--border-strong); }
.chapter-aside span, .chapter-aside small { color: var(--text-tertiary); font-family: var(--font-sans); font-size: 9px; letter-spacing: .15em; text-transform: uppercase; }
.chapter-aside strong { font-size: 66px; font-weight: 400; }
.chapter-kicker { color: var(--accent); font-family: var(--font-sans); font-size: 10px; font-weight: 800; letter-spacing: .15em; text-transform: uppercase; }
.daily-quote { max-width: 900px; margin: 28px 0 0; font-size: clamp(34px, 5vw, 64px); line-height: 1.12; }
.quote-author { color: var(--text-tertiary); font-family: var(--font-sans); font-size: 12px; }
.chapter-body > h2, .feature-copy h2, .closing-page h2 { max-width: 780px; margin: 100px 0 0; font-size: clamp(42px, 6vw, 76px); font-weight: 400; letter-spacing: -.04em; line-height: 1.02; }
.chapter-intro { max-width: 760px; color: var(--text-secondary); font-family: var(--font-sans); font-size: 17px; line-height: 1.9; }
.learning-flow { margin: 70px 0 0; padding: 0; border-top: 1px solid var(--border-strong); list-style: none; }
.learning-flow li { display: grid; grid-template-columns: 50px 1fr auto; align-items: start; gap: 22px; padding: 30px 0; border-bottom: 1px solid var(--border-color); }
.flow-number { color: var(--accent); font-style: italic; }
.learning-flow strong { font-size: 24px; font-weight: 400; }
.learning-flow p { max-width: 620px; margin: 8px 0 0; color: var(--text-secondary); font-family: var(--font-sans); font-size: 14px; line-height: 1.7; }
.learning-flow em { color: var(--text-tertiary); font-size: 24px; }
.editorial-feature { display: grid; grid-template-columns: .85fr 1.15fr; gap: clamp(50px, 9vw, 130px); padding: 140px clamp(24px, 8vw, 130px); color: var(--accent-contrast); background: var(--text-primary); }
.feature-copy { align-self: start; }
.feature-copy .chapter-kicker { color: color-mix(in srgb, var(--accent) 85%, white); }
.feature-copy h2 { margin-top: 20px; }
.feature-copy > p:not(.chapter-kicker) { color: color-mix(in srgb, var(--accent-contrast) 68%, transparent); font-family: var(--font-sans); line-height: 1.8; }
.feature-copy .underlined-button { margin-top: 20px; color: var(--accent-contrast); }
.feature-manuscript { border-top: 1px solid color-mix(in srgb, var(--accent-contrast) 40%, transparent); }
.feature-manuscript article { display: grid; grid-template-columns: 42px 1fr auto; gap: 20px; padding: 30px 0; border-bottom: 1px solid color-mix(in srgb, var(--accent-contrast) 24%, transparent); }
.feature-manuscript > article > span { color: var(--accent); font-family: var(--font-sans); font-size: 11px; }
.feature-manuscript h3 { margin: 0; font-size: 25px; font-weight: 400; }
.feature-manuscript p { margin: 8px 0 0; color: color-mix(in srgb, var(--accent-contrast) 64%, transparent); font-family: var(--font-sans); font-size: 13px; line-height: 1.7; }
.feature-manuscript em { color: var(--accent); font-size: 20px; }
.closing-page { max-width: 900px; margin: 0 auto; padding: 150px 24px; text-align: center; }
.closing-mark { display: grid; width: 54px; height: 54px; place-items: center; margin: 0 auto 25px; border: 1px solid var(--accent); border-radius: 50%; color: var(--accent); font-size: 30px; font-style: italic; }
.closing-page > p:first-of-type { color: var(--accent); font-family: var(--font-sans); font-size: 10px; font-weight: 800; letter-spacing: .16em; }
.closing-page h2 { margin: 25px auto 0; }
.closing-rule { width: 70px; height: 1px; margin: 34px auto; background: var(--border-strong); }
.closing-description { max-width: 600px; margin: 0 auto 28px; color: var(--text-secondary); font-family: var(--font-sans); line-height: 1.8; }
.reading-footer { display: flex; justify-content: space-between; gap: 20px; padding: 28px clamp(20px, 6vw, 96px); border-top: 1px solid var(--border-color); color: var(--text-tertiary); font-family: var(--font-sans); font-size: 10px; letter-spacing: .08em; text-transform: uppercase; }
button:focus-visible { outline: 3px solid var(--focus-ring); outline-offset: 4px; }

@media (max-width: 900px) {
  .reading-header { grid-template-columns: 1fr auto; }
  .header-links { display: none; }
  .book-spread { grid-template-columns: 1fr; }
  .book-page--left { border-right: 0; border-bottom: 1px solid #ded1bd; }
  .reading-chapter { grid-template-columns: 1fr; }
  .chapter-aside { width: 120px; }
  .editorial-feature { grid-template-columns: 1fr; }
}
@media (max-width: 640px) {
  .reading-header { padding: 16px; }
  .wordmark small, .text-button, .header-actions .solid-button { display: none; }
  .header-actions { gap: 8px; }
  .opening { padding: 125px 16px 80px; }
  h1 { font-size: clamp(50px, 17vw, 78px); line-height: .92; }
  .edition-line span:first-child::after, .edition-line span:last-child::before { display: none; }
  .opening-actions { align-items: stretch; flex-direction: column; gap: 12px; }
  .reading-preview { margin-top: 58px; }
  .book-spread { padding: 12px; }
  .book-page { min-height: 390px; padding: 45px 24px 28px; }
  .reading-chapter { padding-block: 90px; }
  .chapter-body > h2 { margin-top: 70px; }
  .learning-flow li { grid-template-columns: 32px 1fr; }
  .learning-flow em { display: none; }
  .feature-manuscript article { grid-template-columns: 30px 1fr; }
  .feature-manuscript em { display: none; }
  .editorial-feature { padding: 90px 22px; }
  .reading-footer { align-items: flex-start; flex-direction: column; }
}
@media (prefers-reduced-motion: reduce) { *, *::before, *::after { scroll-behavior: auto !important; transition-duration: .01ms !important; } }
</style>
