import {onDeactivated, onMounted, onUnmounted, watch} from "vue";
import {emitter, EventKey} from "@/utils/eventBus.ts";
import {useRuntimeStore} from "@/stores/runtime.ts";
import {useSettingStore} from "@/stores/setting.ts";
import {isMobile} from "@/utils";

export function useWindowClick(cb: (e: PointerEvent) => void) {
  onMounted(() => {
    emitter.on(EventKey.closeOther, cb)
    window.addEventListener('click', cb)
  })
  onUnmounted(() => {
    window.removeEventListener('click', cb)
  })
}

export function useEventListener(type: string, listener: EventListenerOrEventListenerObject) {
  const invokeListener = (event: KeyboardEvent) => {
    if (typeof listener === 'function') {
      return (listener as EventListener)(event)
    }
    if (listener && typeof (listener as EventListenerObject).handleEvent === 'function') {
      return (listener as EventListenerObject).handleEvent(event)
    }
  }

  let cleanup: (() => void) | null = null

  onMounted(() => {
    const cleanupFns: Array<() => void> = []
    const registerCleanup = (fn: () => void) => cleanupFns.push(fn)

    const performCleanup = () => {
      while (cleanupFns.length) {
        const fn = cleanupFns.pop()
        try {
          fn()
        } catch (err) {
          console.warn('[useEventListener] cleanup error', err)
        }
      }
    }

    if (isMobile() && type === 'keydown') {
      const ensureMobileInput = () => {
        let input = document.querySelector('#typing-listener') as HTMLInputElement | null
        if (!input) {
          input = document.createElement('input')
          input.id = 'typing-listener'
          input.type = 'text'
          input.autocomplete = 'off'
          input.autocapitalize = 'off'
          input.autocorrect = false
          input.spellcheck = false
          input.tabIndex = -1
          input.setAttribute('aria-hidden', 'true')
          Object.assign(input.style, {
            position: 'fixed',
            opacity: '0',
            pointerEvents: 'none',
            width: '1px',
            height: '1px',
            top: '0',
            left: '-9999px',
            zIndex: '-1',
          })
        }
        if (!input.parentNode) {
          document.body.appendChild(input)
        }
        return input
      }

      const hiddenInput = ensureMobileInput()
      let isComposing = false
      const ignoredKeys = new Set<string>()
      const markIgnore = (key: string) => {
        ignoredKeys.add(key)
        window.setTimeout(() => ignoredKeys.delete(key), 150)
      }

      const createSyntheticEvent = (payload: { key: string; code?: string; keyCode: number }) => {
        const base = {
          key: payload.key,
          code: payload.code ?? '',
          keyCode: payload.keyCode,
          which: payload.keyCode,
          ctrlKey: false,
          altKey: false,
          shiftKey: false,
          metaKey: false,
          repeat: false,
          isComposing: false,
          type,
          preventDefault() {
          },
          stopPropagation() {
          },
          stopImmediatePropagation() {
          },
        }
        return base as unknown as KeyboardEvent
      }

      const dispatchSyntheticKey = (payload: { key: string; code?: string; keyCode: number }) => {
        markIgnore(payload.key)
        invokeListener(createSyntheticEvent(payload))
      }

      const handleCompositionStart = () => {
        isComposing = true
      }

      const handleCompositionEnd = (event: CompositionEvent) => {
        isComposing = false
        if (!event.data) {
          hiddenInput.value = ''
          return
        }
        for (const char of event.data) {
          const keyCode = char === ' ' ? 32 : char.toUpperCase().charCodeAt(0)
          dispatchSyntheticKey({
            key: char,
            code: char === ' ' ? 'Space' : undefined,
            keyCode,
          })
        }
        hiddenInput.value = ''
      }

      const handleInput = (event: InputEvent) => {
        if (isComposing) return
        const target = event.target as HTMLInputElement | null
        const value = target?.value ?? ''

        if (event.inputType === 'deleteContentBackward') {
          dispatchSyntheticKey({key: 'Backspace', code: 'Backspace', keyCode: 8})
          if (target) target.value = ''
          return
        }

        const char = value.slice(-1) || (event as any).data?.slice(-1)
        if (!char) {
          if (target) target.value = ''
          return
        }

        const keyCode = char === ' ' ? 32 : char.toUpperCase().charCodeAt(0)
        dispatchSyntheticKey({
          key: char,
          code: char === ' ' ? 'Space' : undefined,
          keyCode,
        })

        window.setTimeout(() => {
          if (target) target.value = ''
        }, 0)
      }

      const shouldFocusInput = (target: HTMLElement | null) => {
        if (!target) return false
        if (!window.location.pathname.includes('/practice')) return false
        const typingWord = target.closest('.typing-word')
        if (!typingWord) return false
        if (target.closest('.sentence') || target.closest('.phrase')) return false
        if (target.classList?.contains('flex') && target.querySelector('.phrase')) return false
        return true
      }

      const handleFocusRequest = (event: MouseEvent | TouchEvent) => {
        const target = event.target as HTMLElement | null
        if (!shouldFocusInput(target)) return
        window.setTimeout(() => hiddenInput.focus(), 60)
      }

      const windowListener = (event: KeyboardEvent) => {
        if (ignoredKeys.has(event.key)) {
          ignoredKeys.delete(event.key)
          return
        }
        invokeListener(event)
      }

      hiddenInput.addEventListener('compositionstart', handleCompositionStart)
      registerCleanup(() => hiddenInput.removeEventListener('compositionstart', handleCompositionStart))

      hiddenInput.addEventListener('compositionend', handleCompositionEnd)
      registerCleanup(() => hiddenInput.removeEventListener('compositionend', handleCompositionEnd))

      hiddenInput.addEventListener('input', handleInput)
      registerCleanup(() => hiddenInput.removeEventListener('input', handleInput))

      window.addEventListener('click', handleFocusRequest)
      registerCleanup(() => window.removeEventListener('click', handleFocusRequest))

      window.addEventListener('touchstart', handleFocusRequest)
      registerCleanup(() => window.removeEventListener('touchstart', handleFocusRequest))

      window.addEventListener(type, windowListener)
      registerCleanup(() => window.removeEventListener(type, windowListener))

      registerCleanup(() => {
        hiddenInput.value = ''
      })
    } else {
      const windowListener = (event: Event) => invokeListener(event as KeyboardEvent)
      window.addEventListener(type, windowListener)
      registerCleanup(() => window.removeEventListener(type, windowListener))
    }

    cleanup = () => {
      performCleanup()
      cleanup = null
    }
  })

  const remove = () => {
    if (cleanup) cleanup()
  }

  onUnmounted(remove)
  onDeactivated(remove)
}

export function getShortcutKey(e: KeyboardEvent) {
  let shortcutKey = ''
  if (e.ctrlKey || e.metaKey) shortcutKey += 'Ctrl+'
  if (e.altKey) shortcutKey += 'Alt+'
  if (e.shiftKey) shortcutKey += 'Shift+'
  if (e.key !== 'Control' && e.key !== 'Alt' && e.key !== 'Shift') {
    if (e.keyCode >= 65 && e.keyCode <= 90) {
      shortcutKey += e.key.toUpperCase()
    } else {
      if (e.key === 'ArrowRight') {
        shortcutKey += '➡'
      } else if (e.key === 'ArrowLeft') {
        shortcutKey += '⬅'
      } else if (e.key === 'ArrowUp') {
        shortcutKey += '⬆'
      } else if (e.key === 'ArrowDown') {
        shortcutKey += '⬇'
      } else {
        shortcutKey += e.key
      }
    }
  }
  shortcutKey = shortcutKey.trim()

  // console.log('key', shortcutKey)
  return shortcutKey
}

export function useStartKeyboardEventListener() {
  const runtimeStore = useRuntimeStore()
  const settingStore = useSettingStore()

  useEventListener('keydown', (e: KeyboardEvent) => {
    //解决无法复制、全选的问题
    if ((e.ctrlKey || e.metaKey) && ['KeyC', 'KeyA'].includes(e.code)) return
    if (!runtimeStore.disableEventListener) {

      // 检查当前单词是否包含空格，如果包含，则空格键应该被视为输入
      if (e.code === 'Space') {
        // 获取当前正在输入的单词信息
        const currentWord = window.__CURRENT_WORD_INFO__;

        // 如果当前单词包含空格，且下一个字符应该是空格，则将空格键视为输入
        // 或者如果当前处于输入锁定状态（等待空格输入），也将空格键视为输入
        if (currentWord &&
          ((currentWord.word &&
              currentWord.word.includes(' ') &&
              currentWord.word[currentWord.input.length] === ' ') ||
            currentWord.inputLock === true)) {
          e.preventDefault();
          return emitter.emit(EventKey.onTyping, e);
        }
      }

      let shortcutKey = getShortcutKey(e)
      // console.log('shortcutKey', shortcutKey)

      let list = Object.entries(settingStore.shortcutKeyMap)
      let shortcutEvent = ''
      for (let i = 0; i < list.length; i++) {
        let [k, v] = list[i]
        if (v === shortcutKey) {
          // console.log('快捷键', k)
          shortcutEvent = k
          break
        }
      }
      if (shortcutEvent) {
        e.preventDefault()
        emitter.emit(shortcutEvent, e)
      } else {
        //非英文模式下，输入区域的 keyCode 均为 229时，
        // 空格键始终应该被转发到onTyping函数，由它来决定是作为输入还是切换单词
        if (e.code === 'Space') {
          e.preventDefault();
          return emitter.emit(EventKey.onTyping, e);
        }

        if (((e.keyCode >= 65 && e.keyCode <= 90)
          || (e.keyCode >= 48 && e.keyCode <= 57)
          // 空格键已经在上面处理过了
          || e.code === 'Slash'
          || e.code === 'Quote'
          || e.code === 'Comma'
          || e.code === 'BracketLeft'
          || e.code === 'BracketRight'
          || e.code === 'Period'
          || e.code === 'Minus'
          || e.code === 'Equal'
          || e.code === 'Semicolon'
          // || e.code === 'Backquote'
          || e.keyCode === 229
          //当按下功能键时，不阻止事件传播
        ) && (!e.ctrlKey && !e.altKey)) {
          if (isMobile() && e.keyCode === 229 && e.key === 'Unidentified') {
            // 安卓软键盘在keydown阶段不会提供字符，等待input/composition事件来派发实际输入
            return
          }
          e.preventDefault()
          emitter.emit(EventKey.onTyping, e)
        } else {
          emitter.emit(EventKey.keydown, e)
        }
      }

    }
  })
  useEventListener('keyup', (e: KeyboardEvent) => {
    if (!runtimeStore.disableEventListener) {
      emitter.emit(EventKey.keyup, e)
    }
  })
}

export function useOnKeyboardEventListener(onKeyDown: (e: KeyboardEvent) => void, onKeyUp: (e: KeyboardEvent) => void) {
  onMounted(() => {
    emitter.on(EventKey.keydown, onKeyDown)
    emitter.on(EventKey.keyup, onKeyUp)
  })
  onUnmounted(() => {
    emitter.off(EventKey.keydown, onKeyDown)
    emitter.off(EventKey.keyup, onKeyUp)
  })
}

//因为如果用useStartKeyboardEventListener局部变量控制，当出现多个hooks时就不行了，所以用全局变量来控制
export function useDisableEventListener(watchVal: any) {
  const runtimeStore = useRuntimeStore()
  watch(watchVal, (n: any) => {
    runtimeStore.disableEventListener = n
  })
}
