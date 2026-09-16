# DeskSeek - DeepSeek 桌面轻量吸附悬浮窗

DeskSeek 是一款为 Windows 用户打造的轻量级 DeepSeek 桌面智能侧边助手。
坚决**摒弃 Electron**，基于 **.NET 9 + WPF + 原生 WebView2** 构建，待机仅占用极低系统资源，启动迅速，体验丝滑。

---

## 🌟 核心特性

- 🪶 **极度轻量，拒绝臃肿**：采用 Windows 内置的 Edge WebView2 内核，拒绝打包动辄数百兆的 Chromium，启动飞快，内存待机开销极小。
- ⚖️ **合规安全，原汁原味**：非逆向、非反代、无需接入第三方 API。直连 DeepSeek 官方网页版 (`https://chat.deepseek.com`)，用户登录态保存在本地隔离 Profile，安全合规，对话数据完全与官方同步。
- 🎯 **磁吸边缘，闲置半隐藏**：
  - 悬浮球可在任意屏幕位置自由拖拽。
  - 松手自动磁吸附在屏幕左侧或右侧边缘。
  - 空闲 2.5 秒后自动向边缘缩入为贴边小拉手，降低透明度，不遮挡日常工作视线；鼠标靠近立即弹回。
- 📑 **侧边竖向抽屉**：
  - 单击悬浮球或按全局快捷键，侧边抽屉平滑滑出。
  - 支持随悬浮球位置自动贴靠左侧或右侧屏幕展开。
- 📌 **随叫随到 / 固定常驻双模式**：
  - **随叫随到模式 (默认)**：查完即走，点击其他窗口失焦自动平滑滑出收起。
  - **图钉固定模式 (Pin)**：点击顶部图钉 📌，窗口锁定常驻屏幕最上层，方便边写代码/文档边参考 DeepSeek。
- 🔍 **竖屏排版优化与缩放**：提供 100% / 90% / 80% 一键缩放切换，完美适应狭长侧边栏宽度。
- ⌨️ **全局快捷键呼出**：默认支持 `Alt + D` 全局唤醒/隐藏。
- 🖲️ **系统托盘驻留与开机自启**：右键系统托盘可一键切换开机自启动、重置位置、固定状态及退出。

---

## 🚀 编译与运行

### 环境要求
- Windows 10 (1809+) 或 Windows 11
- .NET 9 SDK (当前系统已安装)
- Microsoft Edge WebView2 Runtime (Windows 10/11 系统通常已自带)

### 快速启动
在项目根目录下执行：
```powershell
# 编译并直接运行
dotnet run
```

### 发布为独立可执行文件
```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```
编译产物位于 `bin\Release\net9.0-windows\win-x64\publish\DeskSeek.exe`。

---

## 🛠️ 技术架构

```
DeskSeek/
├── Models/
│   └── AppSettings.cs          # 用户配置实体（位置、锁定态、缩放、自启等）
├── Services/
│   ├── SettingsService.cs      # 本地配置持久化 (%AppData%/DeskSeek/settings.json)
│   ├── AutoStartService.cs     # Windows 注册表开机启动管理
│   └── HotkeyService.cs        # Win32 RegisterHotKey 全局快捷键服务 (Alt+D)
├── Helpers/
│   └── ScreenHelper.cs         # 多显示器 DPI 自适应与屏幕可用工作区边界计算
├── Views/
│   ├── FloatingBallWindow.xaml # 边缘磁吸悬浮球（无边框、平滑弹动、自动半隐藏）
│   └── DrawerWindow.xaml       # 竖向抽屉面板（WebView2、顶部工具栏、滑动动画）
├── App.xaml / App.xaml.cs      # 单实例 Mutex、系统托盘 NotifyIcon、全局生命周期
├── app.manifest                # PerMonitorV2 高 DPI 感知配置
└── DeskSeek.csproj             # .NET 9 WPF 项目配置
```

---

## 📄 开源许可与免责声明

- 本项目基于 **[Apache License 2.0](LICENSE)** 协议开源。
- **免责声明**：DeskSeek 是一款独立的开源第三方辅助工具，仅通过系统标准 WebView2 浏览器组件嵌入 DeepSeek 官方公共网页服务。本项目与 DeepSeek 官方无隶属关系，不拥有 DeepSeek 商标权，不对官方网页端服务质量作任何保证。
