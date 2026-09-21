using System;
using System.Collections.Generic;
using System.Windows;

namespace DeskSeek.Services
{
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public const string Chinese = "zh-CN";
        public const string English = "en-US";

        public string CurrentLanguage { get; private set; } = Chinese;

        public event Action<string>? LanguageChanged;

        private readonly Dictionary<string, string> _zhStrings = new()
        {
            // General & Brand
            ["Lang_AppTitle"] = "DeskSeek",
            ["Lang_TrayTooltipPrefix"] = "DeskSeek - DeepSeek 桌面轻量吸附窗",
            ["Lang_TrayNoHotkey"] = "未设置快捷键",
            ["Lang_SingleInstanceMsg"] = "DeskSeek 已经在运行中！可通过贴边悬浮球、快捷键或右下角托盘图标唤出。",
            ["Lang_SingleInstanceTitle"] = "DeskSeek 已启动",

            // Drawer Window & Toolbar
            ["Lang_BtnZoom"] = "缩放网页 (点击切换 90% / 80% / 100%)",
            ["Lang_BtnRefresh"] = "刷新当前页面",
            ["Lang_BtnBrowser"] = "在默认浏览器中打开当前对话",
            ["Lang_BtnClose"] = "收起窗口 (失焦或按快捷键也可收起)",
            ["Lang_BtnPin"] = "常驻模式 (点击切换)",
            ["Lang_PinTopmostBadge"] = "顶",
            ["Lang_PinNormalBadge"] = "驻",
            ["Lang_PinTopmostHeader"] = "常驻且置顶",
            ["Lang_PinNormalHeader"] = "常驻但不置顶",
            ["Lang_PinAutoHideHeader"] = "失焦自动收起",
            ["Lang_PinTopmostTip"] = "当前模式：常驻且置顶\n• 失焦不收起，窗口始终置顶最前\n• 左键点击切换为：常驻但不置顶\n• 右键点击可直接选择模式",
            ["Lang_PinNormalTip"] = "当前模式：常驻但不置顶\n• 失焦不收起，允许被其他窗口遮挡\n• 左键点击切换为：失焦自动收起\n• 右键点击可直接选择模式",
            ["Lang_PinAutoHideTip"] = "当前模式：失焦自动收起\n• 随叫随到，点击其他应用或桌面时自动收起\n• 左键点击切换为：常驻且置顶\n• 右键点击可直接选择模式",

            // Context Menus
            ["Lang_MenuToggle"] = "显示 / 隐藏",
            ["Lang_MenuAutoStart"] = "开机自启",
            ["Lang_MenuSettings"] = "偏好设置...",
            ["Lang_MenuResetBall"] = "重置悬浮球位置",
            ["Lang_MenuOpenBrowser"] = "在浏览器中打开当前对话",
            ["Lang_MenuExit"] = "退出 DeskSeek",

            // Settings Overlay
            ["Lang_SettingsTitle"] = "偏好设置",
            ["Lang_BackToChat"] = " 返回对话",
            ["Lang_SecLanguage"] = "语言与本地化",
            ["Lang_LanguageTitle"] = "界面显示语言",
            ["Lang_LanguageDesc"] = "即时切换界面显示语言，无需重启程序",
            ["Lang_LanguageZh"] = "简体中文",
            ["Lang_LanguageEn"] = "English",

            // Section Theme
            ["Lang_SecTheme"] = "外观与个性化",
            ["Lang_ThemeTitle"] = "色彩模式",
            ["Lang_ThemeDesc"] = "支持跟随 Windows 系统自动切换，或强制指定浅色/深色",
            ["Lang_ThemeAuto"] = "跟随系统",
            ["Lang_ThemeLight"] = "浅色",
            ["Lang_ThemeDark"] = "深色",
            ["Lang_AccentTitle"] = "主题强调色",
            ["Lang_AccentDesc"] = "可选择跟随 Windows 系统个性化强调色，或使用 DeepSeek 经典蓝",
            ["Lang_AccentSystem"] = "Windows 系统色",
            ["Lang_AccentDeepSeek"] = "DeepSeek 蓝",

            ["Lang_SecGeneral"] = "基础与窗口",
            ["Lang_AutoStart"] = "开机自启",
            ["Lang_AutoStartDesc"] = "系统开机后自动常驻系统托盘",
            ["Lang_AutoCollapse"] = "贴边闲置半隐藏",
            ["Lang_AutoCollapseDesc"] = "悬浮球靠边闲置 2.5 秒后自动缩为露出一半",

            ["Lang_SecPositionMemory"] = "主界面位置记忆",
            ["Lang_RememberX"] = "记住 X 轴位置 (横向)",
            ["Lang_RememberXDesc"] = "唤出时还原上次水平停靠位置（取消则自动靠紧屏幕边缘）",
            ["Lang_RememberY"] = "记住 Y 轴位置 (纵向)",
            ["Lang_RememberYDesc"] = "唤出时还原上次垂直拖动高度（取消则默认靠顶居齐）",

            ["Lang_SecHotkey"] = "快捷键与唤醒",
            ["Lang_GlobalHotkey"] = "唤出快捷键",
            ["Lang_HotkeyDesc"] = "按下组合键以修改唤出 DeskSeek 的快捷键",
            ["Lang_HotkeyPrompt"] = "点击录制快捷键 (或直接选下方预设)",
            ["Lang_HotkeyRecording"] = "正在录制... 请在键盘按下组合键 (如 Ctrl+Shift+D)",
            ["Lang_HotkeySaved"] = "已保存快捷键",
            ["Lang_HotkeyFailed"] = "该快捷键可能已被系统其他程序占用",
            ["Lang_HotkeyDefaultChip"] = "默认",
            ["Lang_HotkeyDisabledChip"] = "无 (禁用)",
            ["Lang_HotkeyNoneText"] = "无 (已禁用)",
            ["Lang_HotkeyRecordingText"] = "请在键盘直接按下快捷键...",
            ["Lang_HotkeySuccessPrefix"] = "✓ 热键已生效：",
            ["Lang_HotkeyDisabledMsg"] = "已禁用快捷键唤醒",
            ["Lang_HotkeyFailedMsg"] = "⚠️ 热键注册失败：可能已被系统或其他应用占用",

            ["Lang_SecPerformance"] = "性能与内存优化",
            ["Lang_Suspend"] = "后台休眠省内存",
            ["Lang_SuspendDesc"] = "抽屉收起后自动挂起内核释放内存与 CPU",

            ["Lang_SecLayout"] = "尺寸与排版",
            ["Lang_ResetSize"] = "重置侧边栏尺寸",
            ["Lang_ResetSizeDesc"] = "恢复默认宽度 460px 及屏幕可用高度",
            ["Lang_BtnReset"] = "重置尺寸",

            ["Lang_SecData"] = "数据与存储",
            ["Lang_LocalData"] = "本地配置与数据目录",
            ["Lang_LocalDataDesc"] = "保存登录 Cookie 及 JSON 配置文件",
            ["Lang_BtnOpenDir"] = "打开目录",
            ["Lang_AboutSlogan"] = "极简轻量 · 官方直连 · 原生体验",

            // Disclaimer Overlay
            ["Lang_DisclaimerTitle"] = "使用前免责声明与服务协议",
            ["Lang_DisclaimerSubtitle"] = "首次使用请仔细阅读并知悉以下规范",
            ["Lang_DisclaimerSec1Title"] = "一、第三方非官方声明",
            ["Lang_DisclaimerSec1Content"] = "DeskSeek 为独立的开源第三方轻量桌面辅助客户端，其核心功能为提供贴边吸附与便捷唤起的 DeepSeek 网页版（chat.deepseek.com）容器。DeskSeek 与 DeepSeek（杭州深度求索人工智能基础技术研究有限公司）官方不存在任何商业合作、投资或运营隶属关系。",
            ["Lang_DisclaimerSec2Title"] = "二、隐私安全与数据传输",
            ["Lang_DisclaimerSec2Content"] = "本软件坚守纯粹轻量原则，完全依托系统原生 WebView2 运行。软件本身绝不拦截、窃取、存储或上传您的任何 DeepSeek 账户、密码、Token、Cookies 或对话记录。所有通信均直接在您的本地设备与 DeepSeek 官方服务器间通过 HTTPS 加密进行。",
            ["Lang_DisclaimerSec3Title"] = "三、使用规范与合规责任",
            ["Lang_DisclaimerSec3Content"] = "用户在使用 DeskSeek 及 DeepSeek 网页版时，应自觉遵守国家法律法规以及 DeepSeek 官方制定的《服务条款》和《用户协议》。使用者不得利用本软件从事任何危害网络安全、侵犯他人隐私或违反相关法律规定的活动。",
            ["Lang_DisclaimerSec4Title"] = "四、免责条款",
            ["Lang_DisclaimerSec4Content"] = "本软件遵循 Apache-2.0 开源许可协议提供，按“现状”（AS-IS）分发，不提供任何形式的明示或暗示担保。因网络环境不稳定、DeepSeek 官方网页端架构调整或不可抗力导致的服务中断或异常，本软件开发者不承担任何附带或连带法律责任。",
            ["Lang_DisclaimerCheck"] = "我已充分阅读、理解并完全同意上述免责声明及使用规范",
            ["Lang_DisclaimerBtn"] = "同意并开始使用",
            ["Lang_DisclaimerCountdown"] = "请先阅读免责声明 ({0}s)",
            ["Lang_DisclaimerPleaseCheck"] = "请勾选已阅读同意"
        };

        private readonly Dictionary<string, string> _enStrings = new()
        {
            // General & Brand
            ["Lang_AppTitle"] = "DeskSeek",
            ["Lang_TrayTooltipPrefix"] = "DeskSeek - DeepSeek Desktop Companion",
            ["Lang_TrayNoHotkey"] = "No Hotkey",
            ["Lang_SingleInstanceMsg"] = "DeskSeek is already running! You can summon it via the floating ball, hotkey, or system tray icon.",
            ["Lang_SingleInstanceTitle"] = "DeskSeek is Running",

            // Drawer Window & Toolbar
            ["Lang_BtnZoom"] = "Web Zoom (Click to cycle 90% / 80% / 100%)",
            ["Lang_BtnRefresh"] = "Reload Current Page",
            ["Lang_BtnBrowser"] = "Open Current Chat in Default Browser",
            ["Lang_BtnClose"] = "Hide Window (Click outside or press hotkey)",
            ["Lang_BtnPin"] = "Pin Mode (Click to cycle)",
            ["Lang_PinTopmostBadge"] = "TOP",
            ["Lang_PinNormalBadge"] = "PIN",
            ["Lang_PinTopmostHeader"] = "Pinned & Topmost",
            ["Lang_PinNormalHeader"] = "Pinned (Normal)",
            ["Lang_PinAutoHideHeader"] = "Auto-hide on Blur",
            ["Lang_PinTopmostTip"] = "Current: Pinned & Topmost\n• Does not hide on blur; always on top\n• Left click to cycle to: Pinned (Normal)\n• Right click to select mode directly",
            ["Lang_PinNormalTip"] = "Current: Pinned (Normal)\n• Does not hide on blur; can be covered\n• Left click to cycle to: Auto-hide on Blur\n• Right click to select mode directly",
            ["Lang_PinAutoHideTip"] = "Current: Auto-hide on Blur\n• Auto-hides when clicking outside or other apps\n• Left click to cycle to: Pinned & Topmost\n• Right click to select mode directly",

            // Context Menus
            ["Lang_MenuToggle"] = "Show / Hide",
            ["Lang_MenuAutoStart"] = "Launch on Startup",
            ["Lang_MenuSettings"] = "Preferences...",
            ["Lang_MenuResetBall"] = "Reset Ball Position",
            ["Lang_MenuOpenBrowser"] = "Open Current Chat in Browser",
            ["Lang_MenuExit"] = "Exit DeskSeek",

            // Settings Overlay
            ["Lang_SettingsTitle"] = "Preferences",
            ["Lang_BackToChat"] = " Back to Chat",
            ["Lang_SecLanguage"] = "Language & Localization",
            ["Lang_LanguageTitle"] = "Display Language",
            ["Lang_LanguageDesc"] = "Instantly switch language without restarting the app",
            ["Lang_LanguageZh"] = "简体中文",
            ["Lang_LanguageEn"] = "English",

            // Section Theme
            ["Lang_SecTheme"] = "Appearance & Personalization",
            ["Lang_ThemeTitle"] = "Color Mode",
            ["Lang_ThemeDesc"] = "Follow Windows system theme, or force Light/Dark mode",
            ["Lang_ThemeAuto"] = "System",
            ["Lang_ThemeLight"] = "Light",
            ["Lang_ThemeDark"] = "Dark",
            ["Lang_AccentTitle"] = "Accent Color",
            ["Lang_AccentDesc"] = "Follow Windows system accent color, or use DeepSeek Blue",
            ["Lang_AccentSystem"] = "Windows Accent",
            ["Lang_AccentDeepSeek"] = "DeepSeek Blue",

            ["Lang_SecGeneral"] = "General & Window",
            ["Lang_AutoStart"] = "Launch on Startup",
            ["Lang_AutoStartDesc"] = "Automatically start and minimize to system tray on boot",
            ["Lang_AutoCollapse"] = "Edge Auto-Dock",
            ["Lang_AutoCollapseDesc"] = "Dock ball to screen edge peeking out half after 2.5s idle",

            ["Lang_SecPositionMemory"] = "Window Position Memory",
            ["Lang_RememberX"] = "Remember Horizontal Position (X)",
            ["Lang_RememberXDesc"] = "Restore last horizontal position when shown (dock to edge if unchecked)",
            ["Lang_RememberY"] = "Remember Vertical Position (Y)",
            ["Lang_RememberYDesc"] = "Restore last vertical position when shown (align to top if unchecked)",

            ["Lang_SecHotkey"] = "Hotkey & Activation",
            ["Lang_GlobalHotkey"] = "Global Hotkey",
            ["Lang_HotkeyDesc"] = "Press a key combination to change DeskSeek shortcut",
            ["Lang_HotkeyPrompt"] = "Click to record shortcut (or choose presets below)",
            ["Lang_HotkeyRecording"] = "Recording... Press key combination (e.g. Ctrl+Shift+D)",
            ["Lang_HotkeySaved"] = "Hotkey Saved",
            ["Lang_HotkeyFailed"] = "Registration failed; may be in use by another app",
            ["Lang_HotkeyDefaultChip"] = "Default",
            ["Lang_HotkeyDisabledChip"] = "None (Disabled)",
            ["Lang_HotkeyNoneText"] = "None (Disabled)",
            ["Lang_HotkeyRecordingText"] = "Press shortcut keys on keyboard...",
            ["Lang_HotkeySuccessPrefix"] = "✓ Hotkey active: ",
            ["Lang_HotkeyDisabledMsg"] = "Shortcut activation disabled",
            ["Lang_HotkeyFailedMsg"] = "⚠️ Hotkey failed: may be occupied by system or another app",

            ["Lang_SecPerformance"] = "Performance & Memory",
            ["Lang_Suspend"] = "Background Suspension",
            ["Lang_SuspendDesc"] = "Suspend WebView2 engine when drawer is hidden to free RAM & CPU",

            ["Lang_SecLayout"] = "Size & Layout",
            ["Lang_ResetSize"] = "Reset Sidebar Size",
            ["Lang_ResetSizeDesc"] = "Restore default 460px width and work area height",
            ["Lang_BtnReset"] = "Reset Size",

            ["Lang_SecData"] = "Data & Storage",
            ["Lang_LocalData"] = "Local Config & Data Directory",
            ["Lang_LocalDataDesc"] = "Stores login cookies and JSON configuration",
            ["Lang_BtnOpenDir"] = "Open Folder",
            ["Lang_AboutSlogan"] = "Minimalist · Direct Connect · Native Experience",

            // Disclaimer Overlay
            ["Lang_DisclaimerTitle"] = "Disclaimer & Terms of Service",
            ["Lang_DisclaimerSubtitle"] = "Please read and agree to the following terms before using DeskSeek",
            ["Lang_DisclaimerSec1Title"] = "1. Third-Party Disclaimer",
            ["Lang_DisclaimerSec1Content"] = "DeskSeek is an independent open-source third-party desktop client providing an edge-dockable container for DeepSeek web (chat.deepseek.com). DeskSeek is not affiliated with, sponsored by, or endorsed by DeepSeek (Hangzhou DeepSeek AI Co., Ltd.).",
            ["Lang_DisclaimerSec2Title"] = "2. Privacy & Data Transmission",
            ["Lang_DisclaimerSec2Content"] = "DeskSeek runs purely on the native WebView2 browser engine. It never intercepts, collects, stores, or uploads your passwords, tokens, cookies, or conversation data. All communications occur directly between your device and DeepSeek official servers over secure HTTPS.",
            ["Lang_DisclaimerSec3Title"] = "3. Terms of Use & Compliance",
            ["Lang_DisclaimerSec3Content"] = "Users must comply with applicable local laws and DeepSeek's official Terms of Service and Privacy Policy. Users must not use this software for any illegal, infringing, or unauthorized activities.",
            ["Lang_DisclaimerSec4Title"] = "4. Limitation of Liability",
            ["Lang_DisclaimerSec4Content"] = "This software is distributed under the Apache-2.0 license on an AS-IS basis without warranties of any kind. Developers assume no liability for service interruptions or issues resulting from network instability or official DeepSeek webpage updates.",
            ["Lang_DisclaimerCheck"] = "I have read, understood, and agree to the Disclaimer and Terms",
            ["Lang_DisclaimerBtn"] = "Agree & Get Started",
            ["Lang_DisclaimerCountdown"] = "Please read disclaimer ({0}s)",
            ["Lang_DisclaimerPleaseCheck"] = "Please check the box to proceed"
        };

        public void Initialize(string language)
        {
            SetLanguage(language, triggerEvent: false);
        }

        public void SetLanguage(string language, bool triggerEvent = true)
        {
            CurrentLanguage = (language == English) ? English : Chinese;

            var dict = (CurrentLanguage == English) ? _enStrings : _zhStrings;
            var appRes = System.Windows.Application.Current?.Resources;

            if (appRes != null)
            {
                foreach (var (k, v) in dict)
                {
                    appRes[k] = v;
                }
            }

            if (triggerEvent)
            {
                LanguageChanged?.Invoke(CurrentLanguage);
            }
        }

        public string GetString(string key)
        {
            var dict = (CurrentLanguage == English) ? _enStrings : _zhStrings;
            if (dict.TryGetValue(key, out var val))
                return val;
            if (_zhStrings.TryGetValue(key, out var fallbackVal))
                return fallbackVal;
            return key;
        }
    }
}
