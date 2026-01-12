import * as VueRouter from 'vue-router'
import { RouteRecordRaw } from 'vue-router'
import Layout from "@/layout/default.vue";
import ModernLayout from "@/layout/modern.vue";
import PracticeLayout from "@/layout/practice.vue";
import words from "@/pages/(words)/words.vue";
import DictDetail from "@/pages/(words)/dict-detail.vue";
import DictList from "@/pages/(words)/dict-list.vue";
import PracticeWords from "@/pages/(words)/practice-words/[id].vue";
import WordTest from "@/pages/(words)/words-test/[id].vue";

import articles from "@/pages/(articles)/articles.vue";
import BookDetail from "@/pages/(articles)/book-detail.vue";
import BookList from "@/pages/(articles)/book-list.vue";
import PracticeArticles from "@/pages/(articles)/practice-articles/[id].vue";

import setting from "@/pages/setting.vue";
import login from "@/pages/(user)/login.vue";
import user from "@/pages/(user)/user.vue";
import vip from "@/pages/(user)/vip.vue";
import feedback from "@/pages/feedback.vue";
import qa from "@/pages/qa.vue";
import doc from "@/pages/doc.vue";
import dashboard from "@/pages/dashboard.vue";
import home from "@/pages/home.vue";
import practiceLoading from "@/pages/practice-loading.vue";

import { LOGIN_PATH, REGISTER_PATH, CALLBACK_PATH, REDIRECT_PATH } from '@/config/logto.config'

export const routes: RouteRecordRaw[] = [
  // 主页路由（营销页面）
  {
    path: '/',
    component: home
  },
  // 应用入口重定向到仪表板
  {
    path: '/dashboard',
    redirect: REDIRECT_PATH
  },
  // 加载页面路由
  {
    path: '/practice-loading/:id',
    component: practiceLoading,
    props: true
  },
  // 练习页面路由（使用专用布局）
  {
    path: '/practice',
    component: PracticeLayout,
    children: [
      { path: 'words/:id', component: PracticeWords },
      { path: 'articles/:id', component: PracticeArticles },
    ]
  },
  // 登录相关路由 - 独立于布局
  {
    path: LOGIN_PATH,
    component: login,
    meta: { public: true, title: '登录' }
  },
  {
    path: REGISTER_PATH,
    component: () => import('@/pages/(user)/register.vue'),
    meta: { public: true, title: '注册' }
  },
  // OAuth 回调路由
  {
    path: CALLBACK_PATH,
    component: () => import('@/pages/(user)/callback.vue'),
    meta: { public: true, title: 'OAuth回调' }
  },
  // 现代化应用路由 - 需要登录
  {
    path: '/app',
    component: ModernLayout,
    meta: { requiresAuth: true },
    children: [
      { path: '', component: dashboard, meta: { title: '仪表板' } },
      { path: 'dashboard', component: dashboard, meta: { title: '仪表板' } },
      { path: 'words', component: words, meta: { title: '单词练习' } },
      { path: 'word', redirect: '/app/words' },
      { path: 'practice-words/:id', redirect: to => `/practice-loading/${to.params.id}?target=/practice/words` },
      { path: 'word-test/:id', component: WordTest, meta: { title: '单词测试' } },
      { path: 'study-word', redirect: '/app/words' },
      { path: 'dict-list', component: DictList, meta: { title: '词典列表' } },
      { path: 'dict-detail', component: DictDetail, meta: { title: '词典详情' } },

      { path: 'articles', component: articles, meta: { title: '文章背诵' } },
      { path: 'article', redirect: '/app/articles' },
      { path: 'practice-articles/:id', redirect: to => `/practice-loading/${to.params.id}?target=/practice/articles` },
      { path: 'study-article', redirect: '/app/articles' },
      { path: 'book-detail', component: BookDetail, meta: { title: '书籍详情' } },
      { path: 'book-list', component: BookList, meta: { title: '书籍列表' } },

      { path: 'user', component: user, meta: { title: '个人中心' } },
      { path: 'vip', component: vip, meta: { title: 'VIP会员' } },

      { path: 'setting', component: setting, meta: { title: '设置' } },
      { path: 'feedback', component: feedback, meta: { title: '反馈' } },
      { path: 'qa', component: qa, meta: { title: '问答' } },
      { path: 'doc', component: doc, meta: { title: '文档' } },
    ]
  },
  // 兼容旧路由 - 重定向到新的现代化布局
  { path: '/words', redirect: '/app/words' },
  { path: '/articles', redirect: '/app/articles' },
  { path: '/setting', redirect: '/app/setting' },

  { path: '/user', redirect: '/app/user' },
  { path: '/feedback', redirect: '/app/feedback' },
  { path: '/doc', redirect: '/app/doc' },
  { path: '/qa', redirect: '/app/qa' },
  { path: '/batch-edit-article', component: () => import("@/pages/(articles)/batch-edit-article.vue") },
  { path: '/:pathMatch(.*)*', redirect: '/' },
]

const router = VueRouter.createRouter({
  history: VueRouter.createWebHistory(import.meta.env.VITE_ROUTE_BASE),
  routes,
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) {
      return savedPosition
    } else {
      return { top: 0 }
    }
  },
})

// 路由守卫
router.beforeEach(async (to: any, from: any) => {
  const { useUserStore } = await import('@/stores/user')
  const userStore = useUserStore()

  const isPublicRoute = to.meta?.public === true

  if (isPublicRoute) {
    return true
  }

  const requiresAuth = to.matched.some((record: any) => record.meta.requiresAuth)

  if (requiresAuth) {
    if (!userStore.isLogin) {
      await userStore.init()
    }

    if (!userStore.isLogin && !userStore.logtoUser) {
      return {
        path: LOGIN_PATH,
        query: { redirect: to.fullPath }
      }
    }
  }

  return true
})

export default router
