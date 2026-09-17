<p align="center">
  <img src="Assets/logo.png" width="96" height="96" alt="DeskSeek Logo" />
</p>

<h1 align="center">DeskSeek</h1>

<p align="center">
  <strong>DeepSeek Desktop Companion / DeepSeek 桌面轻量吸附悬浮窗</strong>
</p>

<p align="center">
  <a href="https://github.com/Xiantaidu/DeskSeek/actions/workflows/build-and-release.yml"><img src="https://github.com/Xiantaidu/DeskSeek/actions/workflows/build-and-release.yml/badge.svg" alt="Build Status" /></a>
  <a href="https://github.com/Xiantaidu/DeskSeek/releases"><img src="https://img.shields.io/github/v/release/Xiantaidu/DeskSeek?color=0052D9&label=Release" alt="Latest Release" /></a>
  <a href="https://github.com/Xiantaidu/DeskSeek"><img src="https://img.shields.io/badge/Platform-Windows-0078D6.svg" alt="Platform" /></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/9.0"><img src="https://img.shields.io/badge/.NET-9.0-512BD4.svg" alt=".NET 9.0" /></a>
  <a href="https://github.com/Xiantaidu/DeskSeek/blob/main/LICENSE"><img src="https://img.shields.io/badge/License-Apache%202.0-green.svg" alt="License" /></a>
</p>

<p align="center">
  <a href="#简体中文">简体中文</a> &nbsp;|&nbsp; <a href="#english">English</a>
</p>

---

## 简体中文

DeskSeek 是一款专为 Windows 用户打造的轻量级 DeepSeek 桌面智能侧边栏工具。
基于 **.NET 9 + WPF + 原生 WebView2** 构建，摒弃 Electron 框架，拥有极低系统资源占用、毫秒级启动与流畅贴边交互。

### 核心特性

- **轻量原生体验**：采用系统内置的 Microsoft Edge WebView2 内核，拒绝数百兆 Chromium 打包开销，内存待机占用极小。
- **直连官方服务**：不反代、不逆向、无需配置第三方 API Key。直连 DeepSeek 官方网页版（`chat.deepseek.com`），登录态与对话记录均保存在本地隔离目录。
- **磁吸贴边与闲置折叠**：悬浮球支持自由拖拽，松手自动贴合屏幕左侧或右侧；闲置 2.5 秒后自动缩进边缘并半隐藏，鼠标悬停即刻恢复。
- **双模态侧边抽屉**：
  - **失焦自动收起（默认）**：随叫随到，点击主屏其他区域自动收回侧边栏。
  - **图钉固定常驻（Pin）**：支持固定常驻与窗口置顶，便于边写代码、做文档边实时参考。
- **多显示器与高 DPI 自适应**：基于 PerMonitorV2 渲染，不同缩放比显示器间拖拽无虚边与变形。
- **排版适配与快速缩放**：内置 80% / 90% / 100% 缩放比例切换，适配竖屏侧边栏宽度。
- **中英双语界面**：偏好设置内支持简体中文与 English 即时热切换，无需重启。
- **多架构分发**：原生支持 x64、x86、ARM64 架构，提供安装版（Installer）与免安装便携版（Portable）。

### 系统环境要求

- **操作系统**：Windows 10（版本 1809 及以上）或 Windows 11
- **运行环境**：
  - [.NET Desktop Runtime 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)（若系统未安装，初次启动时 Windows 会自动弹出官方安装引导）
  - Microsoft Edge WebView2 Runtime（现代 Windows 10/11 系统通常已预装）

### 下载与安装

前往 [Releases 页面](https://github.com/Xiantaidu/DeskSeek/releases) 下载适合您电脑架构的安装包或便携包：

| 架构 | 安装包（Setup） | 便携免安装包（Portable） |
| :--- | :--- | :--- |
| **x64 (推荐)** | `DeskSeek-vX.Y.Z-win-x64-Setup.exe` | `DeskSeek-vX.Y.Z-win-x64-Portable.zip` |
| **x86** | `DeskSeek-vX.Y.Z-win-x86-Setup.exe` | `DeskSeek-vX.Y.Z-win-x86-Portable.zip` |
| **ARM64** | `DeskSeek-vX.Y.Z-win-arm64-Setup.exe` | `DeskSeek-vX.Y.Z-win-arm64-Portable.zip` |

### 常用快捷键

- `Alt + D`：唤出 / 隐藏侧边抽屉（可在设置界面中自定义）。
- 鼠标左键点击悬浮球：展开 / 收起主抽屉。
- 鼠标右键点击悬浮球 / 系统托盘：呼出快捷设置与功能菜单。

### 本地编译

```powershell
# 克隆仓库
git clone https://github.com/Xiantaidu/DeskSeek.git
cd DeskSeek

# 运行调试
dotnet run

# 本地打包生成便携包与安装包
.\installer\build-installer.ps1 -Arch x64
```

### 开源协议与免责声明

- 本项目采用 [Apache License 2.0](LICENSE) 开源协议。
- **免责声明**：DeskSeek 为独立的第三方开源桌面辅助客户端，仅作为系统原生 WebView2 网页容器呈现 DeepSeek 官方服务。本项目与 DeepSeek 官方不存在任何商业合作、投资或隶属关系，不拥有 DeepSeek 商标权。

---

## English

DeskSeek is a lightweight, edge-docked desktop companion for DeepSeek on Windows.
Built with **.NET 9, WPF, and native WebView2**, it eschews Electron to deliver ultra-low memory standby, sub-second startup, and seamless docking animations.

### Key Features

- **Native & Ultra-Lightweight**: Built on Microsoft Edge WebView2 instead of heavy Chromium bundles, minimizing CPU and memory consumption.
- **Direct Official Connection**: No proxies, no reverse engineering, and no third-party API keys required. Connects directly to the official DeepSeek web service (`chat.deepseek.com`). Credentials and history are securely isolated on your local machine.
- **Smart Edge Docking & Auto-Collapse**: Drag the floating ball anywhere; it magnetically snaps to the nearest screen edge. After 2.5 seconds of inactivity, it shrinks into a subtle handle that expands upon mouse hover.
- **Dual Window Modes**:
  - **Auto-Hide on Blur (Default)**: Pops out on demand and auto-retracts when clicking elsewhere.
  - **Pin Mode**: Keeps the drawer pinned on-screen or always on top for concurrent coding or writing.
- **Per-Monitor DPI & Multi-Display**: Supports PerMonitorV2 high-DPI scaling across multiple monitors without blurring.
- **Webpage Zoom Adaptation**: Quick-toggle zoom between 80%, 90%, and 100% to optimize vertical reading layouts.
- **Bilingual Interface**: Seamlessly hot-switch between Simplified Chinese and English in Preferences without restarting.
- **Multi-Architecture**: Native builds for x64, x86, and ARM64 in both Installer (`.exe`) and Portable (`.zip`) formats.

### System Requirements

- **Operating System**: Windows 10 (Build 1809+) or Windows 11
- **Runtime Dependencies**:
  - [.NET Desktop Runtime 9.0](https://dotnet.microsoft.com/download/dotnet/9.0) (Windows will prompt to install automatically if missing)
  - Microsoft Edge WebView2 Runtime (pre-installed on most modern Windows systems)

### Download & Installation

Download the installer or portable package for your platform from the [Releases Page](https://github.com/Xiantaidu/DeskSeek/releases):

| Architecture | Installer (Setup) | Portable Archive (ZIP) |
| :--- | :--- | :--- |
| **x64 (Recommended)** | `DeskSeek-vX.Y.Z-win-x64-Setup.exe` | `DeskSeek-vX.Y.Z-win-x64-Portable.zip` |
| **x86** | `DeskSeek-vX.Y.Z-win-x86-Setup.exe` | `DeskSeek-vX.Y.Z-win-x86-Portable.zip` |
| **ARM64** | `DeskSeek-vX.Y.Z-win-arm64-Setup.exe` | `DeskSeek-vX.Y.Z-win-arm64-Portable.zip` |

### Shortcuts & Controls

- `Alt + D`: Toggle drawer window visibility (customizable in Preferences).
- Left-click Floating Ball: Expand / collapse drawer.
- Right-click Floating Ball or Tray Icon: Open context menu and settings.

### Building from Source

```powershell
# Clone repository
git clone https://github.com/Xiantaidu/DeskSeek.git
cd DeskSeek

# Run in debug mode
dotnet run

# Build installer and portable packages locally
.\installer\build-installer.ps1 -Arch x64
```

### License & Disclaimer

- Licensed under the [Apache License 2.0](LICENSE).
- **Disclaimer**: DeskSeek is an independent open-source third-party desktop client. It functions solely as a native WebView2 wrapper for the public DeepSeek web interface. DeskSeek is not affiliated with, sponsored by, or endorsed by DeepSeek (Hangzhou DeepSeek AI Co., Ltd.).
