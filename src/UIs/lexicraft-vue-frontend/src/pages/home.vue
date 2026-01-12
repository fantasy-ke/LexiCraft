<template>
  <div class="min-h-screen bg-gradient-to-br from-blue-50 to-white overflow-hidden">
    <!-- 头部导航 -->
    <header class="fixed top-0 left-0 right-0 z-50 bg-white/80 backdrop-blur-md border-b border-blue-100">
      <nav class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between items-center h-16">
          <!-- Logo -->
          <div class="flex items-center space-x-3">
            <div class="w-8 h-8 bg-gradient-to-r from-blue-500 to-blue-600 rounded-lg flex items-center justify-center">
              <span class="text-white font-bold text-sm">LC</span>
            </div>
            <span class="text-xl font-bold text-gray-900">LexionCraft</span>
          </div>
          
          <!-- 中间导航链接 -->
          <div class="hidden lg:flex items-center space-x-8">
            <a href="#hero" class="text-gray-600 hover:text-blue-600 font-medium transition-colors duration-200 cursor-pointer">首页</a>
            <a href="#features" class="text-gray-600 hover:text-blue-600 font-medium transition-colors duration-200 cursor-pointer">功能特色</a>
            <a href="#cta" class="text-gray-600 hover:text-blue-600 font-medium transition-colors duration-200 cursor-pointer">立即使用</a>
          </div>
          
          <!-- 右侧按钮组 -->
          <div class="flex items-center space-x-3">
            <!-- GitHub 按钮 - 优化边框颜色 -->
            <a 
              href="https://github.com/zyronon/LexionCraft" 
              target="_blank"
              class="hidden md:flex items-center space-x-2 bg-gray-900/90 hover:bg-gray-800 text-white px-4 py-2.5 rounded-xl font-medium shadow-lg hover:shadow-xl transition-all duration-200 cursor-pointer border border-gray-600/30"
            >
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
              </svg>
              <span>GitHub</span>
            </a>
            
            <!-- 登录按钮 - 优化边框颜色 -->
            <button 
              @click="handleLogin"
              class="bg-white/90 hover:bg-white text-gray-700 hover:text-blue-600 px-4 py-2.5 rounded-xl font-medium shadow-lg hover:shadow-xl border border-gray-200/60 hover:border-blue-300/60 transition-all duration-200 cursor-pointer"
            >
              登录
            </button>
            <!-- 移动端菜单按钮 -->
            <button 
              @click="toggleMobileMenu"
              class="lg:hidden bg-white/90 hover:bg-white text-gray-700 p-2.5 rounded-xl shadow-lg border border-gray-200 transition-all duration-200 cursor-pointer"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
              </svg>
            </button>
          </div>
        </div>
        
        <!-- 移动端菜单 -->
        <div v-show="showMobileMenu" class="lg:hidden mt-4 pt-4 border-t border-gray-200">
          <div class="flex flex-col space-y-3 pb-4">
            <a href="#hero" @click="closeMobileMenu" class="text-gray-600 hover:text-blue-600 font-medium py-2 transition-colors cursor-pointer">首页</a>
            <a href="#features" @click="closeMobileMenu" class="text-gray-600 hover:text-blue-600 font-medium py-2 transition-colors cursor-pointer">功能特色</a>
            <a href="#cta" @click="closeMobileMenu" class="text-gray-600 hover:text-blue-600 font-medium py-2 transition-colors cursor-pointer">立即使用</a>
            <a href="/words" @click="closeMobileMenu" class="text-gray-600 hover:text-blue-600 font-medium py-2 transition-colors cursor-pointer">单词练习</a>
            <a href="/articles" @click="closeMobileMenu" class="text-gray-600 hover:text-blue-600 font-medium py-2 transition-colors cursor-pointer">文章背诵</a>
            <a href="/setting" @click="closeMobileMenu" class="text-gray-600 hover:text-blue-600 font-medium py-2 transition-colors cursor-pointer">设置</a>
            
            <!-- 移动端 GitHub 按钮 -->
            <a 
              href="https://github.com/zyronon/LexionCraft" 
              target="_blank"
              class="flex items-center justify-center space-x-2 bg-gray-900/90 hover:bg-gray-800 text-white px-4 py-3 rounded-xl font-medium shadow-lg border border-gray-600/30 transition-all duration-200 cursor-pointer mt-2"
            >
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
              </svg>
              <span>GitHub</span>
            </a>
          </div>
        </div>
      </nav>
    </header>

    <!-- 主要内容区域 -->
    <main class="pt-16">
      <!-- Hero 区域 -->
      <section id="hero" class="relative min-h-screen flex items-center justify-center px-4 sm:px-6 lg:px-8">
        <!-- 背景动画元素 -->
        <div class="absolute inset-0 overflow-hidden">
          <div class="floating-shapes">
            <div class="shape shape-1"></div>
            <div class="shape shape-2"></div>
            <div class="shape shape-3"></div>
            <div class="shape shape-4"></div>
            <div class="shape shape-5"></div>
          </div>
        </div>

        <!-- 主要内容 -->
        <div class="relative z-10 text-center max-w-4xl mx-auto">
          <!-- 标题动画 -->
          <div class="mb-8 animate-fade-in-up">
            <h1 class="text-5xl sm:text-6xl lg:text-7xl font-bold text-gray-900 mb-6 leading-tight">
              <span class="bg-gradient-to-r from-blue-600 to-blue-800 bg-clip-text text-transparent">
                LexionCraft
              </span>
            </h1>
            <p class="text-xl sm:text-2xl text-gray-600 mb-4">
              智能英语学习平台
            </p>
            <p class="text-lg text-gray-500 max-w-2xl mx-auto">
              通过科学的记忆曲线算法和个性化练习模式，让英语学习更高效、更有趣
            </p>
          </div>

          <!-- CTA 按钮 -->
          <div class="animate-fade-in-up animation-delay-400 mb-16">
            <button 
              @click="startLearning"
              class="cta-button group"
            >
              <span class="relative z-10">开始学习</span>
              <div class="absolute inset-0 bg-gradient-to-r from-blue-600 to-blue-700 rounded-xl transform group-hover:scale-105 transition-transform duration-200"></div>
            </button>
          </div>
        </div>
      </section>

      <!-- 功能特色区域 -->
      <section id="features" class="py-20 px-4 sm:px-6 lg:px-8 bg-white/50">
        <div class="max-w-7xl mx-auto">
          <div class="text-center mb-16">
            <h2 class="text-4xl font-bold text-gray-900 mb-4">功能特色</h2>
            <p class="text-xl text-gray-600 max-w-2xl mx-auto">
              四大核心功能，全方位提升你的英语学习效率
            </p>
          </div>

          <!-- 功能特色网格 -->
          <div class="grid grid-cols-1 md:grid-cols-3 gap-8 mb-12">
            <div class="feature-card">
              <div class="feature-icon bg-blue-100 text-blue-600">
                <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
                </svg>
              </div>
              <h3 class="text-xl font-semibold text-gray-900 mb-2">单词练习</h3>
              <p class="text-gray-600 mb-4">四种练习模式，智能记忆曲线，让单词记忆更牢固</p>
              <ul class="text-sm text-gray-500 space-y-1">
                <li>• 跟打、辨认、复习、默写四种模式</li>
                <li>• 智能记忆曲线算法</li>
                <li>• 音标发音辅助学习</li>
              </ul>
            </div>

            <div class="feature-card animation-delay-100">
              <div class="feature-icon bg-green-100 text-green-600">
                <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                </svg>
              </div>
              <h3 class="text-xl font-semibold text-gray-900 mb-2">文章背诵</h3>
              <p class="text-gray-600 mb-4">逐句练习，语音朗读，提升英语语感和表达能力</p>
              <ul class="text-sm text-gray-500 space-y-1">
                <li>• 内置经典教材文章</li>
                <li>• 支持自定义文章导入</li>
                <li>• 一键翻译对照功能</li>
              </ul>
            </div>

            <div class="feature-card animation-delay-200">
              <div class="feature-icon bg-purple-100 text-purple-600">
                <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
                </svg>
              </div>
              <h3 class="text-xl font-semibold text-gray-900 mb-2">学习统计</h3>
              <p class="text-gray-600 mb-4">详细的学习数据分析，帮你了解学习进度和效果</p>
              <ul class="text-sm text-gray-500 space-y-1">
                <li>• 收藏夹、错词本管理</li>
                <li>• 学习进度实时跟踪</li>
                <li>• 个性化复习推荐</li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      <!-- CTA 区域 -->
      <section id="cta" class="py-20 px-4 sm:px-6 lg:px-8">
        <div class="max-w-4xl mx-auto text-center">
          <div class="bg-gradient-to-r from-blue-500 to-blue-600 rounded-3xl p-12 shadow-2xl text-white">
            <h2 class="text-4xl font-bold mb-6">准备开始学习了吗？</h2>
            <p class="text-xl mb-8 opacity-90">
              加入数万名学习者，开始你的英语提升之旅
            </p>
            <div class="flex flex-col sm:flex-row gap-4 justify-center">
              <button 
                @click="startLearning"
                class="bg-white text-blue-600 px-8 py-4 rounded-xl font-semibold text-lg shadow-lg hover:shadow-xl transform hover:scale-105 transition-all duration-200 cursor-pointer"
              >
                立即开始学习
              </button>
              <a 
                href="https://github.com/zyronon/LexionCraft" 
                target="_blank"
                class="bg-white/20 backdrop-blur-sm text-white px-8 py-4 rounded-xl font-semibold text-lg border border-white/30 hover:bg-white/30 transition-all duration-200 cursor-pointer inline-flex items-center justify-center space-x-2"
              >
                <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
                </svg>
                <span>查看源码</span>
              </a>
            </div>
          </div>
        </div>
      </section>
    </main>

    <!-- 页脚 -->
    <footer class="py-12 px-4 sm:px-6 lg:px-8 bg-gray-50">
      <div class="max-w-7xl mx-auto text-center">
        <div class="flex items-center justify-center space-x-3 mb-4">
          <div class="w-8 h-8 bg-gradient-to-r from-blue-500 to-blue-600 rounded-lg flex items-center justify-center">
            <span class="text-white font-bold text-sm">LC</span>
          </div>
          <span class="text-xl font-bold text-gray-900">LexionCraft</span>
        </div>
        <p class="text-gray-600 leading-relaxed mb-4">
          开源英语学习工具，让学习变得简单有趣。
        </p>
        <p class="text-gray-500">
          &copy; 2024 LexionCraft. 开源项目，MIT 许可证。
        </p>
      </div>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

// 移动端菜单状态
const showMobileMenu = ref(false)

// 开始学习按钮点击事件
const startLearning = () => {
  router.push('/words')
}

// 登录按钮点击事件
const handleLogin = () => {
  // 这里可以添加登录逻辑
  console.log('登录功能')
}

// 切换移动端菜单
const toggleMobileMenu = () => {
  showMobileMenu.value = !showMobileMenu.value
}

// 关闭移动端菜单
const closeMobileMenu = () => {
  showMobileMenu.value = false
}
</script>

<style scoped>
/* 动画定义 */
@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes float {
  0%, 100% {
    transform: translateY(0px) rotate(0deg);
  }
  50% {
    transform: translateY(-20px) rotate(180deg);
  }
}

@keyframes floatReverse {
  0%, 100% {
    transform: translateY(0px) rotate(0deg);
  }
  50% {
    transform: translateY(20px) rotate(-180deg);
  }
}

/* 基础动画类 */
.animate-fade-in-up {
  animation: fadeInUp 0.8s ease-out forwards;
}

.animation-delay-100 {
  animation-delay: 0.1s;
}

.animation-delay-200 {
  animation-delay: 0.2s;
}

.animation-delay-400 {
  animation-delay: 0.4s;
}

/* 浮动形状背景 */
.floating-shapes {
  position: absolute;
  width: 100%;
  height: 100%;
  overflow: hidden;
}

.shape {
  position: absolute;
  opacity: 0.1;
  border-radius: 50%;
}

.shape-1 {
  width: 80px;
  height: 80px;
  background: linear-gradient(45deg, #3B82F6, #60A5FA);
  top: 20%;
  left: 10%;
  animation: float 6s ease-in-out infinite;
}

.shape-2 {
  width: 120px;
  height: 120px;
  background: linear-gradient(45deg, #60A5FA, #93C5FD);
  top: 60%;
  right: 15%;
  animation: floatReverse 8s ease-in-out infinite;
}

.shape-3 {
  width: 60px;
  height: 60px;
  background: linear-gradient(45deg, #1E40AF, #3B82F6);
  top: 40%;
  left: 70%;
  animation: float 7s ease-in-out infinite;
}

.shape-4 {
  width: 100px;
  height: 100px;
  background: linear-gradient(45deg, #93C5FD, #DBEAFE);
  top: 80%;
  left: 20%;
  animation: floatReverse 9s ease-in-out infinite;
}

.shape-5 {
  width: 140px;
  height: 140px;
  background: linear-gradient(45deg, #DBEAFE, #EFF6FF);
  top: 10%;
  right: 30%;
  animation: float 10s ease-in-out infinite;
}

/* 功能卡片样式 */
.feature-card {
  background: rgba(255, 255, 255, 0.8);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(59, 130, 246, 0.1);
  border-radius: 1rem;
  padding: 2rem;
  text-align: left;
  transition: all 0.3s ease;
  cursor: pointer;
  animation: fadeInUp 0.8s ease-out forwards;
}

.feature-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 20px 40px rgba(59, 130, 246, 0.1);
  border-color: rgba(59, 130, 246, 0.2);
}

.feature-icon {
  width: 4rem;
  height: 4rem;
  border-radius: 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 1rem;
  transition: transform 0.3s ease;
}

.feature-card:hover .feature-icon {
  transform: scale(1.1);
}

/* CTA 按钮样式 */
.cta-button {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 1rem 2.5rem;
  font-size: 1.125rem;
  font-weight: 600;
  color: white;
  border: none;
  border-radius: 0.75rem;
  cursor: pointer;
  transition: all 0.3s ease;
  overflow: hidden;
}

.cta-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 10px 30px rgba(59, 130, 246, 0.3);
}

/* 响应式设计 */
@media (max-width: 768px) {
  .shape {
    display: none;
  }
  
  .feature-card {
    padding: 1.5rem;
  }
}

/* 平滑滚动 */
html {
  scroll-behavior: smooth;
}

/* 减少动画（无障碍支持） */
@media (prefers-reduced-motion: reduce) {
  .animate-fade-in-up,
  .shape,
  .feature-card,
  .cta-button {
    animation: none;
    transition: none;
  }
}
</style>