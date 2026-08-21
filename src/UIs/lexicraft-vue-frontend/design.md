# LexiCraft 前端视觉系统

> Hallmark · genre: learning app · design-system: this file · designed-as-app

## 方向

LexiCraft 是一个安静、可持续使用的英语学习空间。视觉应像一张长期使用的书桌：清楚、耐看、少打扰，不用“高级感”装饰替代信息层级。

## 主题与排版

- 公共首页、登录、注册固定使用 Editorial：纸张底色、衬线展示字、无渐变大背景。
- 应用内部保留 Editorial、Zen、Playful Ink 三套主题；主题只改变字体、密度和强调色，不改变业务结构。
- 颜色通过 `src/assets/css/themes.scss` 的语义变量使用：`--surface-*`、`--text-*`、`--accent`、`--border-color`。新页面不得直接引入蓝紫渐变或 slate/indigo 色阶。
- 标题使用 `--font-heading`，正文使用 `--font-sans`，英文词形可使用 `--font-editorial` 或 `--font-mono`。

## 结构

- 页面优先使用标题、说明、列表和边线形成层级；卡片只用于真正独立的内容块。
- 详情页使用“信息头 + 工作区”结构，头部是浅色主题表面，不使用发光圆、玻璃叠层或全屏深色渐变。
- 列表页使用“返回 + 标题 + 搜索”单行工具区，搜索输入以底线或轻边框表达。
- 主操作只有一个实心强调按钮，次操作使用中性边框，辅助操作使用文字按钮。

## 交互

- 悬停只改变颜色、边线或背景，不使用大幅缩放、漂移和持续脉冲。
- 动画必须服务于状态变化，并尊重 `prefers-reduced-motion`。
- 保留现有路由、数据请求、认证、练习和编辑逻辑；视觉重设计不得改变接口契约。

## 禁止回归

- 不新增 `bg-gradient-*`、`blur-3xl`、`font-black`、`shadow-2xl` 作为页面主视觉。
- 不使用 “Premium”“AI”“智能算法”等营销式装饰文案包装普通功能。
- 不在不同页面重复 hero + 三张渐变卡片 + 巨型 CTA 的模板节奏。
