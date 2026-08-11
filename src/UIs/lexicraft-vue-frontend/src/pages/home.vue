<template>
  <div class="public-editorial quiet-home">
    <header class="home-header" aria-label="LexiCraft 首页导航">
      <button class="wordmark" aria-label="返回首页顶部" type="button" @click="scrollToSection('opening')">
        <BrandLogo tagline="A quiet place to learn."/>
      </button>
      <nav class="header-links" aria-label="主导航">
        <button type="button" @click="scrollToSection('method')">{{ copy.nav.method }}</button>
        <button type="button" @click="scrollToSection('experience')">{{ copy.nav.experience }}</button>
      </nav>
      <div class="header-actions">
        <button class="language-button" type="button" @click="toggleLang">{{ lang === 'zh' ? 'EN' : '中' }}</button>
        <button class="text-button" type="button" @click="handleLogin">{{ copy.nav.login }}</button>
        <button class="solid-button solid-button--small" type="button" @click="startLearning">{{ copy.nav.start }}</button>
      </div>
    </header>

    <main>
      <section id="opening" class="opening" aria-labelledby="home-title">
        <div class="opening-copy">
          <p class="eyebrow">{{ copy.hero.eyebrow }}</p>
          <h1 id="home-title"><span>{{ copy.hero.line1 }}</span><span>{{ copy.hero.line2 }}</span></h1>
          <p class="opening-description">{{ copy.hero.description }}</p>
          <div class="opening-actions">
            <button class="solid-button" type="button" @click="startLearning">{{ copy.hero.primary }} <span aria-hidden="true">→</span></button>
            <button class="outline-button" type="button" @click="scrollToSection('method')">{{ copy.hero.secondary }}</button>
          </div>
          <p class="supporting-note">{{ copy.hero.note }}</p>
        </div>

        <aside class="today-card" aria-label="今日学习示例">
          <header class="today-card__header">
            <div><span>{{ copy.preview.label }}</span><strong>{{ copy.preview.title }}</strong></div>
            <small>{{ currentDate }}</small>
          </header>
          <div class="focus-word">
            <span>WORD IN CONTEXT</span><strong>serendipity</strong><small>/ˌserənˈdɪpəti/</small>
            <p>{{ copy.preview.meaning }}</p>
          </div>
          <blockquote>“{{ copy.preview.sentence }}”</blockquote>
          <ol class="preview-steps">
            <li v-for="(item, index) in copy.preview.steps" :key="item"><span>0{{ index + 1 }}</span>{{ item }}</li>
          </ol>
        </aside>
      </section>

      <section id="method" class="section-block" aria-labelledby="method-title">
        <header class="section-heading">
          <p class="eyebrow">{{ copy.method.eyebrow }}</p><h2 id="method-title">{{ copy.method.title }}</h2><p>{{ copy.method.description }}</p>
        </header>
        <ol class="method-grid">
          <li v-for="(item, index) in copy.method.items" :key="item.title">
            <span class="step-number">0{{ index + 1 }}</span><div><h3>{{ item.title }}</h3><p>{{ item.description }}</p></div>
          </li>
        </ol>
      </section>

      <section id="experience" class="section-block feature-section" aria-labelledby="experience-title">
        <header class="section-heading section-heading--compact">
          <p class="eyebrow">{{ copy.experience.eyebrow }}</p><h2 id="experience-title">{{ copy.experience.title }}</h2><p>{{ copy.experience.description }}</p>
        </header>
        <div class="feature-grid">
          <article v-for="item in copy.experience.items" :key="item.title">
            <span class="feature-mark" aria-hidden="true">{{ item.mark }}</span><div><h3>{{ item.title }}</h3><p>{{ item.description }}</p></div>
          </article>
        </div>
      </section>

      <section class="closing" aria-labelledby="closing-title">
        <div><p class="eyebrow">{{ copy.closing.eyebrow }}</p><h2 id="closing-title">{{ copy.closing.title }}</h2><p>{{ copy.closing.description }}</p></div>
        <button class="solid-button" type="button" @click="startLearning">{{ copy.closing.action }} <span aria-hidden="true">→</span></button>
      </section>
    </main>

    <footer><BrandLogo :show-tagline="false"/><span>{{ copy.footer }}</span></footer>
  </div>
</template>

<script lang="ts" setup>
import {computed, ref} from 'vue'
import {useRouter} from 'vue-router'
import BrandLogo from '@/components/BrandLogo.vue'

const router = useRouter()
const lang = ref<'zh' | 'en'>('zh')
const copybook = {
  zh: {
    nav: {method: '学习方法', experience: '学习内容', login: '登录', start: '开始学习'},
    hero: {eyebrow: '词汇 · 阅读 · 练习', line1: '把词汇和文章，', line2: '放进一条清晰的学习路径。', description: '打开内容、开始练习、查看进度。每个页面都只突出当前最重要的下一步。', primary: '进入学习空间', secondary: '查看学习流程', note: '无需复杂设置，从一本词典或一篇文章开始。'},
    preview: {label: '学习示例', title: '今天只专注一个语境', meaning: '意外发现美好事物的能力或经历。', sentence: 'Reading often creates the serendipity that memorisation cannot.', steps: ['先读语境', '再理解词义', '最后完成练习']},
    method: {eyebrow: '简单的学习流程', title: '先理解，再练习，最后复习。', description: '减少来回寻找功能的时间，把注意力留给真正需要掌握的内容。', items: [
      {title: '选择内容', description: '从一本词典或一本书开始，首页直接显示当前学习对象。'},
      {title: '完成今日任务', description: '主按钮只负责开始或继续，次要操作集中放在旁边。'},
      {title: '查看真实进度', description: '用完成量、连续天数和学习时间判断下一步，而不是被装饰打断。'}
    ]},
    experience: {eyebrow: '一个学习空间', title: '词汇、文章和进度，保持同一套操作逻辑。', description: '布局一致、按钮清楚、颜色克制，让你切换内容时不必重新理解页面。', items: [
      {mark: 'W', title: '词汇学习', description: '当前词典、今日任务和练习入口放在同一视线范围内。'},
      {mark: 'R', title: '文章阅读', description: '书籍、阅读进度和继续阅读按钮形成明确主线。'},
      {mark: 'P', title: '学习进度', description: '用简单数据反馈学习状态，不用渐变、动效或无意义标签抢注意力。'}
    ]},
    closing: {eyebrow: '准备好了', title: '少一点界面噪音，多一点有效学习。', description: '进入学习空间，选择内容并完成今天的第一组练习。', action: '开始今天的学习'},
    footer: 'LexiCraft · 让学习路径更清楚'
  },
  en: {
    nav: {method: 'Method', experience: 'Learning', login: 'Sign in', start: 'Start'},
    hero: {eyebrow: 'Vocabulary · Reading · Practice', line1: 'Put words and reading', line2: 'into one clear learning path.', description: 'Open your material, start a session, and check progress. Every page highlights the next important action.', primary: 'Enter learning space', secondary: 'See the learning flow', note: 'No complex setup. Start with one dictionary or one book.'},
    preview: {label: 'Learning example', title: 'Focus on one context today', meaning: 'The chance discovery of something valuable or delightful.', sentence: 'Reading often creates the serendipity that memorisation cannot.', steps: ['Read the context', 'Understand the word', 'Finish the practice']},
    method: {eyebrow: 'A simple learning flow', title: 'Understand, practise, then review.', description: 'Spend less time finding features and more time mastering the material in front of you.', items: [
      {title: 'Choose material', description: 'Start with one dictionary or book and see it clearly on the dashboard.'},
      {title: 'Finish today’s task', description: 'One primary action starts or resumes learning; secondary actions stay nearby.'},
      {title: 'Review real progress', description: 'Use completion, streaks, and study time to decide what comes next.'}
    ]},
    experience: {eyebrow: 'One learning space', title: 'Vocabulary, reading, and progress share one clear system.', description: 'Consistent layouts, clear actions, and restrained colour reduce the effort of switching contexts.', items: [
      {mark: 'W', title: 'Vocabulary', description: 'Current dictionary, daily task, and practice entry remain in one view.'},
      {mark: 'R', title: 'Reading', description: 'Books, reading progress, and the continue action form one obvious path.'},
      {mark: 'P', title: 'Progress', description: 'Simple data shows your state without decorative motion competing for attention.'}
    ]},
    closing: {eyebrow: 'Ready when you are', title: 'Less interface noise. More useful learning.', description: 'Enter the learning space, choose your material, and finish the first session today.', action: 'Start today’s learning'},
    footer: 'LexiCraft · A clearer path to learning'
  }
} as const
const copy = computed(() => copybook[lang.value])
const currentDate = computed(() => new Intl.DateTimeFormat(lang.value === 'zh' ? 'zh-CN' : 'en-US', {month: 'short', day: 'numeric'}).format(new Date()))
const scrollToSection = (id: string) => document.getElementById(id)?.scrollIntoView({behavior: 'smooth'})
const toggleLang = () => { lang.value = lang.value === 'zh' ? 'en' : 'zh' }
const handleLogin = () => router.push('/login')
const startLearning = () => router.push('/app')
</script>

<style lang="scss" scoped>
.quiet-home { min-height: 100vh; color: var(--text-primary); background: var(--surface-page); }
.home-header { position: sticky; top: 0; z-index: 20; display: grid; grid-template-columns: 1fr auto 1fr; align-items: center; min-height: 72px; padding: 0 clamp(24px, 5vw, 80px); border-bottom: 1px solid var(--border-color); background: color-mix(in srgb, var(--surface-page) 94%, transparent); backdrop-filter: blur(16px); }
.wordmark, .header-links button, .header-actions button { border: 0; color: inherit; background: transparent; cursor: pointer; font: inherit; }
.wordmark { justify-self: start; padding: 4px; }
.wordmark :deep(.brand-logo__mark) { width: 38px; height: 38px; }
.wordmark :deep(.brand-logo__copy strong) { font-size: 17px; }
.wordmark :deep(.brand-logo__copy small) { font-size: 9px; }
.header-links { display: flex; gap: 30px; }
.header-links button, .text-button, .language-button { color: var(--text-secondary); font-family: var(--font-sans); font-size: 13px; }
.header-links button:hover, .text-button:hover, .language-button:hover { color: var(--text-primary); }
.header-actions { display: flex; justify-self: end; align-items: center; gap: 18px; }
.language-button { min-width: 28px; }
.solid-button, .outline-button { display: inline-flex; min-height: 46px; align-items: center; justify-content: center; gap: 12px; padding: 0 22px; border: 1px solid var(--text-primary); border-radius: var(--radius-control); cursor: pointer; font-family: var(--font-sans); font-size: 14px; font-weight: 650; transition: background .18s ease, border-color .18s ease, color .18s ease; }
.solid-button { color: var(--accent-contrast); background: var(--accent); border-color: var(--accent); }
.solid-button:hover { background: var(--accent-hover); border-color: var(--accent-hover); }
.solid-button--small { min-height: 38px; padding: 0 16px; font-size: 12px; }
.outline-button { color: var(--text-primary); background: transparent; }
.outline-button:hover { border-color: var(--accent); color: var(--accent); background: var(--surface-card); }
main { width: min(1180px, calc(100% - 48px)); margin: 0 auto; }
.opening { display: grid; grid-template-columns: minmax(0, 1.05fr) minmax(360px, .75fr); gap: clamp(56px, 8vw, 112px); align-items: center; min-height: calc(100vh - 72px); padding: 88px 0; }
.eyebrow { margin: 0 0 18px; color: var(--accent); font-family: var(--font-sans); font-size: 11px; font-weight: 700; letter-spacing: .16em; text-transform: uppercase; }
.opening h1, .section-heading h2, .closing h2 { margin: 0; font-family: var(--font-display); font-weight: 560; letter-spacing: -.035em; }
.opening h1 { max-width: 760px; font-size: clamp(48px, 6.6vw, 86px); line-height: 1.04; }
.opening h1 span { display: block; }
.opening-description { max-width: 620px; margin: 30px 0 0; color: var(--text-secondary); font-family: var(--font-sans); font-size: 17px; line-height: 1.9; }
.opening-actions { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 34px; }
.supporting-note { margin: 18px 0 0; color: var(--text-tertiary); font-family: var(--font-sans); font-size: 12px; }
.today-card { padding: 28px; border: 1px solid var(--border-color); border-radius: var(--radius-card); background: var(--surface-card); box-shadow: var(--card-shadow); }
.today-card__header { display: flex; justify-content: space-between; gap: 20px; padding-bottom: 22px; border-bottom: 1px solid var(--border-color); }
.today-card__header span, .today-card__header strong { display: block; }
.today-card__header span, .today-card__header small { color: var(--text-tertiary); font-family: var(--font-sans); font-size: 11px; }
.today-card__header strong { margin-top: 4px; font-size: 18px; }
.focus-word { padding: 30px 0 24px; }
.focus-word > span { display: block; color: var(--text-tertiary); font-family: var(--font-mono); font-size: 9px; letter-spacing: .12em; }
.focus-word strong { display: block; margin-top: 12px; font-family: var(--font-editorial); font-size: clamp(34px, 4vw, 50px); font-weight: 560; }
.focus-word small { color: var(--text-tertiary); font-family: var(--font-mono); }
.focus-word p { margin: 18px 0 0; color: var(--text-secondary); font-family: var(--font-sans); line-height: 1.75; }
.today-card blockquote { margin: 0; padding: 18px 0; border-block: 1px solid var(--border-color); color: var(--text-secondary); font-size: 15px; line-height: 1.75; }
.preview-steps { display: grid; gap: 12px; margin: 22px 0 0; padding: 0; list-style: none; }
.preview-steps li { display: flex; align-items: center; gap: 12px; color: var(--text-secondary); font-family: var(--font-sans); font-size: 12px; }
.preview-steps span { display: grid; width: 26px; height: 26px; place-items: center; border: 1px solid var(--border-color); border-radius: 50%; color: var(--accent); font-family: var(--font-mono); font-size: 9px; }
.section-block { padding: 112px 0; border-top: 1px solid var(--border-color); }
.section-heading { max-width: 720px; margin-bottom: 56px; }
.section-heading h2 { font-size: clamp(36px, 4.5vw, 58px); line-height: 1.12; }
.section-heading > p:last-child { margin: 22px 0 0; color: var(--text-secondary); font-family: var(--font-sans); font-size: 15px; line-height: 1.8; }
.method-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; margin: 0; padding: 0; list-style: none; }
.method-grid li { display: grid; grid-template-columns: auto 1fr; gap: 18px; padding: 24px 0; border-top: 1px solid var(--border-strong); }
.step-number { color: var(--accent); font-family: var(--font-mono); font-size: 11px; }
.method-grid h3, .feature-grid h3 { margin: 0; font-family: var(--font-sans); font-size: 16px; font-weight: 700; }
.method-grid p, .feature-grid p { margin: 12px 0 0; color: var(--text-secondary); font-family: var(--font-sans); font-size: 13px; line-height: 1.75; }
.feature-section { display: grid; grid-template-columns: minmax(280px, .8fr) minmax(0, 1.2fr); gap: 72px; }
.section-heading--compact { margin: 0; }
.feature-grid { display: grid; gap: 0; }
.feature-grid article { display: grid; grid-template-columns: 42px 1fr; gap: 18px; padding: 24px 0; border-top: 1px solid var(--border-color); }
.feature-grid article:last-child { border-bottom: 1px solid var(--border-color); }
.feature-mark { display: grid; width: 34px; height: 34px; place-items: center; border: 1px solid var(--border-color); border-radius: 50%; color: var(--accent); font-family: var(--font-mono); font-size: 11px; }
.closing { display: flex; align-items: center; justify-content: space-between; gap: 48px; margin: 0 0 80px; padding: 54px; border: 1px solid var(--border-color); border-radius: var(--radius-card); background: var(--surface-card); }
.closing h2 { max-width: 720px; font-size: clamp(34px, 4vw, 54px); line-height: 1.1; }
.closing p:not(.eyebrow) { margin: 18px 0 0; color: var(--text-secondary); font-family: var(--font-sans); line-height: 1.7; }
footer { display: flex; align-items: center; justify-content: space-between; gap: 24px; padding: 28px clamp(24px, 5vw, 80px); border-top: 1px solid var(--border-color); color: var(--text-tertiary); font-family: var(--font-sans); font-size: 11px; }
footer :deep(.brand-logo__mark) { width: 30px; height: 30px; }
footer :deep(.brand-logo__copy strong) { font-size: 13px; }
button:focus-visible { outline: 3px solid var(--focus-ring); outline-offset: 3px; }
@media (max-width: 900px) { .home-header { grid-template-columns: 1fr auto; } .header-links { display: none; } .opening { grid-template-columns: 1fr; min-height: auto; padding: 88px 0; } .today-card { max-width: 620px; } .method-grid { grid-template-columns: 1fr; } .feature-section { grid-template-columns: 1fr; gap: 42px; } .closing { align-items: flex-start; flex-direction: column; } }
@media (max-width: 640px) { .home-header { min-height: 64px; padding: 0 16px; } .wordmark :deep(.brand-logo__copy small), .text-button { display: none; } .header-actions { gap: 10px; } .solid-button--small { padding: 0 12px; } main { width: min(100% - 32px, 1180px); } .opening { padding: 64px 0 72px; } .opening h1 { font-size: clamp(42px, 13vw, 60px); } .opening-description { font-size: 15px; } .opening-actions { align-items: stretch; flex-direction: column; } .opening-actions button { width: 100%; } .today-card { padding: 22px; } .section-block { padding: 78px 0; } .section-heading { margin-bottom: 38px; } .method-grid li { padding: 20px 0; } .closing { margin-bottom: 48px; padding: 30px 24px; } .closing .solid-button { width: 100%; } footer { align-items: flex-start; flex-direction: column; padding-inline: 16px; } }
</style>
