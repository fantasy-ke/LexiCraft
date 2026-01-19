<template>
  <div class="min-h-screen bg-slate-50 text-slate-900 overflow-hidden font-sans selection:bg-blue-600 selection:text-white relative">
    
    <!-- Dynamic Background Elements -->
    <!-- 增强了颜色深度和不透明度，移除了混合模式以确保在浅色背景下可见 -->
    <div class="absolute top-0 left-0 w-full h-full overflow-hidden pointer-events-none z-0">
      <div class="absolute top-[-10%] left-[-10%] w-96 h-96 bg-blue-400 rounded-full filter blur-[80px] opacity-40 animate-blob"></div>
      <div class="absolute top-[-10%] right-[-10%] w-96 h-96 bg-indigo-400 rounded-full filter blur-[80px] opacity-40 animate-blob animation-delay-2000"></div>
      <div class="absolute -bottom-32 left-20 w-96 h-96 bg-pink-400 rounded-full filter blur-[80px] opacity-40 animate-blob animation-delay-4000"></div>
    </div>

    <!-- Header -->
    <header class="fixed top-0 left-0 right-0 z-50 bg-white/80 backdrop-blur-md border-b border-slate-200">
      <nav class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <!-- Desktop Layout: 3-column Grid for perfect centering -->
        <div class="hidden md:grid grid-cols-3 items-center h-16">
          <!-- Left: Logo -->
          <div class="flex items-center space-x-3 cursor-pointer justify-self-start" @click="router.push('/')">
            <div class="w-8 h-8 bg-gradient-to-br from-blue-600 to-indigo-600 rounded-lg flex items-center justify-center shadow-lg shadow-blue-500/20">
              <span class="text-white font-bold text-sm">LC</span>
            </div>
            <span class="text-xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-slate-900 to-slate-700">LexionCraft</span>
          </div>
          
          <!-- Center: Nav Links -->
          <div class="flex items-center justify-center space-x-8 justify-self-center w-full">
            <button @click="scrollToSection('hero')" class="bg-transparent border-none outline-none focus:outline-none focus:ring-0 cursor-pointer text-sm font-medium text-slate-500 hover:text-blue-600 transition-colors">{{ t[lang].nav.home }}</button>
            <button @click="scrollToSection('features')" class="bg-transparent border-none outline-none focus:outline-none focus:ring-0 cursor-pointer text-sm font-medium text-slate-500 hover:text-blue-600 transition-colors">{{ t[lang].nav.features }}</button>
            <a href="/articles" class="text-sm font-medium text-slate-500 hover:text-blue-600 transition-colors">{{ t[lang].nav.articles }}</a>
            <a href="/words" class="text-sm font-medium text-slate-500 hover:text-blue-600 transition-colors">{{ t[lang].nav.words }}</a>
          </div>
          
          <!-- Right: Actions -->
          <div class="flex items-center space-x-4 justify-self-end">
             <!-- Lang Switch -->
            <button 
              @click="toggleLang" 
              class="flex items-center justify-center w-9 h-9 rounded-full bg-white text-slate-600 hover:text-blue-600 hover:bg-slate-50 transition-all duration-300 text-xs font-bold shadow-md hover:shadow-lg hover:-translate-y-0.5 border-none outline-none focus:ring-0"
              style="border: none !important;"
              :title="lang === 'en' ? 'Switch to Chinese' : '切换到英文'"
            >
              {{ lang === 'en' ? '中' : 'En' }}
            </button>

            <button 
              @click="handleLogin"
              class="bg-transparent border-none outline-none focus:outline-none focus:ring-0 cursor-pointer text-sm font-medium text-slate-600 hover:text-blue-600 transition-colors"
            >
              {{ t[lang].nav.login }}
            </button>
            <button 
              @click="startLearning"
              class="border-none outline-none focus:ring-0 bg-blue-600 text-white px-6 py-2.5 rounded-full text-sm font-bold hover:bg-blue-700 hover:shadow-xl hover:shadow-blue-500/30 transition-all transform hover:-translate-y-0.5 shadow-lg shadow-blue-500/20"
              style="border: none !important;"
            >
              {{ t[lang].nav.start }}
            </button>
          </div>
        </div>

        <!-- Mobile Layout: Flex -->
        <div class="md:hidden flex justify-between items-center h-16">
          <div class="flex items-center space-x-3 cursor-pointer" @click="router.push('/')">
             <div class="w-8 h-8 bg-gradient-to-br from-blue-600 to-indigo-600 rounded-lg flex items-center justify-center">
              <span class="text-white font-bold text-sm">LC</span>
            </div>
            <span class="text-xl font-bold text-slate-900">LexionCraft</span>
          </div>

          <div class="flex items-center space-x-3">
             <button 
              @click="toggleLang" 
              class="flex items-center justify-center w-9 h-9 rounded-full bg-white text-slate-600 hover:text-blue-600 transition-colors text-xs font-bold shadow-md border-none outline-none focus:ring-0"
              style="border: none !important;"
            >
              {{ lang === 'en' ? '中' : 'En' }}
            </button>
            <button @click="showMobileMenu = !showMobileMenu" class="text-slate-500 hover:text-blue-600 p-2 border-none outline-none focus:ring-0">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path v-if="!showMobileMenu" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
                <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
      </nav>

      <!-- Mobile Menu -->
      <div v-show="showMobileMenu" class="md:hidden fixed inset-x-0 top-16 bottom-0 z-40 bg-white/95 backdrop-blur-xl border-t border-slate-200 p-6 transition-all duration-300 ease-in-out">
        <div class="flex flex-col space-y-6">
          <button @click="handleMobileNavClick('hero')" class="text-lg font-medium text-slate-600 hover:text-blue-600 transition-colors text-left border-none outline-none bg-transparent">{{ t[lang].nav.home }}</button>
          <button @click="handleMobileNavClick('features')" class="text-lg font-medium text-slate-600 hover:text-blue-600 transition-colors text-left border-none outline-none bg-transparent">{{ t[lang].nav.features }}</button>
          <a href="/articles" class="text-lg font-medium text-slate-600 hover:text-blue-600 transition-colors">{{ t[lang].nav.articles }}</a>
          <a href="/words" class="text-lg font-medium text-slate-600 hover:text-blue-600 transition-colors">{{ t[lang].nav.words }}</a>
          <div class="h-px bg-slate-200 my-2"></div>
          <button @click="handleLogin" class="text-lg font-medium text-slate-600 hover:text-blue-600 text-left transition-colors border-none outline-none bg-transparent">{{ t[lang].nav.login }}</button>
          <button @click="startLearning" class="bg-blue-600 hover:bg-blue-700 text-white px-4 py-3 rounded-xl text-lg font-bold text-center shadow-lg shadow-blue-500/20 transition-all border-none outline-none" style="border: none !important;">{{ t[lang].nav.start }}</button>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="pt-24 pb-12 relative z-10">
      <!-- Hero Section -->
      <section id="hero" class="relative px-4 sm:px-6 lg:px-8 flex flex-col items-center justify-center min-h-[70vh]">
        <div class="relative z-10 text-center max-w-5xl mx-auto space-y-8">
          <div class="inline-flex items-center space-x-2 bg-blue-50 border border-blue-100 rounded-full px-4 py-1.5 mb-4 animate-fade-in backdrop-blur-sm bg-opacity-80">
            <span class="w-1.5 h-1.5 rounded-full bg-blue-600"></span>
            <span class="text-xs font-medium text-blue-600 tracking-wide uppercase">{{ t[lang].hero.tag }}</span>
          </div>
          
          <h1 class="text-5xl sm:text-7xl lg:text-8xl font-bold tracking-tight leading-tight animate-fade-in-up">
            <span class="block text-slate-900">{{ t[lang].hero.title1 }}</span>
            <span class="block mt-2 bg-gradient-to-r from-blue-600 to-indigo-600 bg-clip-text text-transparent">
              {{ t[lang].hero.title2 }}
            </span>
          </h1>
          
          <p class="text-lg sm:text-xl text-slate-600 max-w-2xl mx-auto leading-relaxed animate-fade-in-up animation-delay-200">
            {{ t[lang].hero.subtitle }}
          </p>

          <div class="mt-10 flex justify-center gap-x-6 animate-fade-in-up animation-delay-400">
            <button 
              @click="startLearning"
              class="border-none outline-none focus:ring-0 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white px-12 py-5 rounded-2xl text-xl font-bold transition-all duration-300 transform hover:-translate-y-1 hover:scale-105 shadow-[0_20px_50px_-10px_rgba(37,99,235,0.5)] hover:shadow-[0_25px_60px_-10px_rgba(37,99,235,0.7)]"
              style="border: none !important;"
            >
              {{ t[lang].hero.cta }}
            </button>
          </div>
        </div>
      </section>

      <!-- Features Section -->
      <section id="features" class="py-20 px-4 sm:px-6 lg:px-8">
        <div class="max-w-7xl mx-auto">
          <div class="flex items-center justify-between mb-8">
             <h2 class="text-2xl font-bold text-slate-900">{{ t[lang].features.title }}</h2>
             <a href="#" class="text-sm text-slate-500 hover:text-blue-600 flex items-center gap-1 transition-colors">
               {{ t[lang].features.viewAll }}
               <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 8l4 4m0 0l-4 4m4-4H3"></path></svg>
             </a>
          </div>
          
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            <div v-for="(feature, index) in t[lang].features.items" :key="index" 
              class="group relative bg-white/80 backdrop-blur-sm border border-slate-200 rounded-2xl overflow-hidden hover:border-blue-500/50 transition-all duration-300 hover:-translate-y-1 cursor-pointer shadow-sm hover:shadow-md"
            >
              <div class="aspect-video bg-gradient-to-br from-slate-50 to-slate-100 group-hover:from-slate-100 group-hover:to-slate-200 transition-colors p-6 flex items-center justify-center relative">
                 <div class="absolute inset-0 bg-[url('/grid.svg')] opacity-10 invert"></div>
                 <div class="w-16 h-16 rounded-full flex items-center justify-center transition-transform duration-300 group-hover:scale-110"
                    :class="{
                      'bg-blue-100 text-blue-600': index === 0,
                      'bg-green-100 text-green-600': index === 1,
                      'bg-purple-100 text-purple-600': index === 2,
                      'bg-pink-100 text-pink-600': index === 3
                    }"
                 >
                   <!-- Icons based on index -->
                   <svg v-if="index === 0" class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"></path></svg>
                   <svg v-if="index === 1" class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
                   <svg v-if="index === 2" class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
                   <svg v-if="index === 3" class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"></path></svg>
                 </div>
              </div>
              <div class="p-4">
                <h3 class="font-semibold text-slate-900 mb-1">{{ feature.title }}</h3>
                <p class="text-xs text-slate-500">{{ feature.desc }}</p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </main>

    <!-- Footer -->
    <footer class="border-t border-slate-200 bg-white/80 backdrop-blur-sm py-12 px-4 relative z-10">
      <div class="max-w-7xl mx-auto flex flex-col md:flex-row justify-between items-center gap-6">
        <div class="flex items-center space-x-3">
          <div class="w-8 h-8 bg-gradient-to-br from-blue-600 to-indigo-600 rounded-lg flex items-center justify-center">
            <span class="text-white font-bold text-sm">LC</span>
          </div>
          <span class="text-xl font-bold text-slate-900">LexionCraft</span>
        </div>
        <div class="text-slate-500 text-sm">
          {{ t[lang].footer.rights }}
        </div>
        <div class="flex items-center space-x-6">
           <a href="https://github.com/zyronon/LexionCraft" target="_blank" class="text-slate-500 hover:text-blue-600 transition-colors">
             GitHub
           </a>
           <a href="#" class="text-slate-500 hover:text-blue-600 transition-colors">
             {{ t[lang].footer.privacy }}
           </a>
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const showMobileMenu = ref(false)
// 设置默认为中文
const lang = ref<'en' | 'zh'>('zh')

const toggleLang = () => {
  lang.value = lang.value === 'en' ? 'zh' : 'en'
}

const t = {
  en: {
    nav: {
      home: 'Home',
      features: 'Features',
      articles: 'Articles',
      words: 'Words',
      login: 'Sign In',
      start: 'Get Started'
    },
    hero: {
      tag: 'Scientific Learning',
      title1: 'Master English',
      title2: 'with LexionCraft',
      subtitle: 'Contextual learning, spaced repetition, and smart insights. The smartest way to expand your vocabulary.',
      cta: 'Start Learning →'
    },
    features: {
      title: 'Core Features',
      viewAll: 'View All',
      items: [
        { title: 'Contextual Reading', desc: 'Read articles with instant lookup and one-click saving.' },
        { title: 'Spaced Repetition', desc: 'Scientific review scheduling for long-term memory.' },
        { title: 'Smart Dictionary', desc: 'Auto-generated definitions, examples, and etymology.' },
        { title: 'Progress Tracking', desc: 'Visualize your vocabulary growth over time.' }
      ]
    },
    footer: {
      rights: '© 2024 LexionCraft. Open Source Project.',
      privacy: 'Privacy'
    }
  },
  zh: {
    nav: {
      home: '首页',
      features: '功能',
      articles: '文章',
      words: '单词',
      login: '登录',
      start: '开始学习'
    },
    hero: {
      tag: '科学记忆方法',
      title1: '掌握英语',
      title2: '与 LexionCraft',
      subtitle: '语境学习，间隔重复，以及智能化的深度洞察。扩展词汇量的最科学方式。',
      cta: '开始学习 →'
    },
    features: {
      title: '核心功能',
      viewAll: '查看全部',
      items: [
        { title: '语境阅读', desc: '阅读文章，即点即译，一键收藏生词。' },
        { title: '间隔重复', desc: '基于科学的复习计划，确保长期记忆。' },
        { title: '智能词典', desc: '自动生成释义、例句和词源分析。' },
        { title: '进度追踪', desc: '可视化您的词汇增长历程。' }
      ]
    },
    footer: {
      rights: '© 2024 LexionCraft. 开源项目。',
      privacy: '隐私政策'
    }
  }
}

const startLearning = () => {
  router.push('/app/dashboard')
}

const handleLogin = () => {
  router.push('/login')
}

const scrollToSection = (id: string) => {
  const element = document.getElementById(id)
  if (element) {
    element.scrollIntoView({ behavior: 'smooth' })
  }
}

const handleMobileNavClick = (id: string) => {
  showMobileMenu.value = false
  scrollToSection(id)
}
</script>

<style scoped>
.animation-delay-200 {
  animation-delay: 200ms;
}
.animation-delay-400 {
  animation-delay: 400ms;
}
.animation-delay-2000 {
  animation-delay: 2s;
}
.animation-delay-4000 {
  animation-delay: 4s;
}

@keyframes blob {
  0% { transform: translate(0px, 0px) scale(1); }
  33% { transform: translate(30px, -50px) scale(1.1); }
  66% { transform: translate(-20px, 20px) scale(0.9); }
  100% { transform: translate(0px, 0px) scale(1); }
}

.animate-blob {
  animation: blob 7s infinite;
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.animate-fade-in-up {
  animation: fadeInUp 0.8s ease-out forwards;
  opacity: 0;
}

.animate-fade-in {
  animation: fadeIn 0.8s ease-out forwards;
  opacity: 0;
}
</style>
