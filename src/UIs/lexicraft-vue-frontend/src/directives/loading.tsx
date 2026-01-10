// src/directives/loading.js
import {createApp, h} from 'vue'
//@ts-ignore
import IconEosIconsLoading from '~icons/eos-icons/loading'

// 创建一个 Loading 组件
const LoadingComponent = {
  name: 'LoadingComponent',
  render() {
    return (
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          background: 'rgba(255, 255, 255, 0.7)',
          zIndex: 9999
        }}
      >
        <IconEosIconsLoading class="text-3xl"/>
      </div>
    )
  }
}

// 自定义指令
export default {
  mounted(el, binding) {
    // console.log('el',)
    const position = getComputedStyle(el).position
    if (position === 'static' || !position) {
      el.style.position = 'relative' // 保证 loading 居中
    }

    const app = createApp(LoadingComponent)
    const instance = app.mount(document.createElement('div'))
    el.__loadingInstance = instance

    if (binding.value) {
      el.appendChild(instance?.$el)
    }
  },
  updated(el, binding) {
    const instance = el.__loadingInstance
    if (binding.value && !el.contains(instance?.$el)) {
      el.appendChild(instance?.$el)
    } else if (!binding.value && el.contains(instance?.$el)) {
      el.removeChild(instance?.$el)
    }
  },
  unmounted(el) {
    const instance = el.__loadingInstance
    if (instance && instance?.$el.parentNode) {
      instance?.$el.parentNode.removeChild(instance?.$el)
    }
    delete el.__loadingInstance
  }
}
