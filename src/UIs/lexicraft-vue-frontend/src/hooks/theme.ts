import {useSettingStore} from "@/stores/setting.ts";

type Theme = "light" | "dark";

// 获取系统主题
function getSystemTheme(): Theme {
    if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
        return 'dark';
    } else if (window.matchMedia('(prefers-color-scheme: light)').matches) {
        return 'light';
    }
    return 'light'; // 默认浅色模式
}

// 交换主题名称
function swapTheme(theme: Theme): Theme {
    return theme === 'light' ? 'dark' : 'light'
}

// 监听系统主题变化
function listenToSystemThemeChange(call: (theme: Theme) => void) {
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
        if (e.matches) {
            // console.log('系统已切换到深色模式');
            call('dark');
        }
    });
    window.matchMedia('(prefers-color-scheme: light)').addEventListener('change', e => {
        if (e.matches) {
            // console.log('系统已切换到浅色模式');
            call('light');
        }
    });
}

export default function useTheme() {
    const settingStore = useSettingStore()

    // Apply theme to DOM (Side Effect)
    function updateDOM(val: string) {
        const themeName = val === 'auto' ? getSystemTheme() : val;
        document.documentElement.className = themeName;
        // Add 'dark' class explicitly if theme is dark, to support Tailwind's dark mode if configured by class
        if (themeName === 'dark') {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    }

    // 开启监听系统主题变更
    listenToSystemThemeChange((theme: Theme) => {
        // Only update DOM if logic is in auto mode
        if (settingStore.theme === 'auto') {
            updateDOM('auto');
        }
    })

    function toggleTheme() {
        const nextTheme = getThemeSetting() === 'dark' ? 'light' : 'dark';
        setTheme(nextTheme);
    }

    function setTheme(val: string) {
        settingStore.theme = val as Theme | 'auto'; // Update store
        updateDOM(val);
    }

    // 获取当前具体的主题名称 (Resolved theme)
    function getTheme(): Theme {
        return settingStore.theme === 'auto' ? getSystemTheme() : settingStore.theme as Theme;
    }

    // 获取设置的主题值 (including 'auto')
    function getThemeSetting() {
        return settingStore.theme;
    }

    // Initialize: Ensure DOM matches store on hook usage (or rely on App.vue)
    // To avoid repeated setting, we can check if class is not set?
    // But safest is to apply current store value.
    updateDOM(settingStore.theme);

    return {
        toggleTheme,
        setTheme,
        getTheme,
        getThemeSetting
    }
}
