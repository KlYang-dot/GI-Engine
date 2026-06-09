# GI Engine Project

<div align="center">

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)
[![GitHub release](https://img.shields.io/github/v/release/KlYang-dot/GI-Engine)](https://github.com/KlYang-dot/GI-Engine/releases)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078d7)](https://github.com/KlYang-dot/GI-Engine)
[![Unity Version](https://img.shields.io/badge/Unity-6000%2B-black)](https://unity.com/)

</div>

<style>
  .lang-tab {
    overflow: hidden;
    border-bottom: 1px solid #ccc;
    margin-bottom: 20px;
  }
  .lang-tab button {
    background-color: inherit;
    float: left;
    border: none;
    outline: none;
    cursor: pointer;
    padding: 10px 20px;
    transition: 0.2s;
    font-size: 18px;
    font-weight: 600;
    border-radius: 8px 8px 0 0;
  }
  .lang-tab button:hover {
    background-color: #f0f0f0;
  }
  .lang-tab button.active {
    background-color: #0366d6;
    color: white;
  }
  .lang-content {
    display: none;
    padding: 10px 0;
  }
  .lang-content.active {
    display: block;
  }
</style>

<div class="lang-tab">
  <button class="tablink active" onclick="switchLang(event, 'zh')">中文</button>
  <button class="tablink" onclick="switchLang(event, 'en')">English</button>
</div>

<div id="zh" class="lang-content active">

> **GI Engine：不仅仅是桌面宠物。**  
> 一款面向中文用户的轻量级桌面虚拟伙伴引擎，支持 VRM 角色、模型商店、原神模型优化渲染及丰富扩展功能。

GI Engine 是基于 [Mate-Engine](https://github.com/shinyflvre/Mate-Engine) 深度爆改的开源桌面伙伴应用。我们解决了原项目中的大量内存泄漏问题，新增了**模型商店**，并全面优化了渲染效果与 UI 交互，旨在为您提供更稳定、更流畅、更具个性化的虚拟伙伴体验。

---

## 📑 目录

- [✨ 主要特性](#zh-主要特性)
- [🖼️ 预览展示](#zh-预览展示)
- [⚡ 快速开始](#zh-快速开始)
- [🔧 从源码构建](#zh-从源码构建)
- [📈 改进对比](#zh-改进对比)
- [❓ 常见问题解答](#zh-常见问题解答)
- [🧭 开发路线图](#zh-开发路线图)
- [🤝 贡献与许可证](#zh-贡献与许可证)
- [💬 联系与致谢](#zh-联系与致谢)

---

## ✨ 主要特性 {#zh-主要特性}

| 功能 | 描述 |
| :--- | :--- |
| **VRM 角色支持** | 完整支持 VRM 0.x / 1.x 模型（含表情、眨眼、口型同步及物理骨骼） |
| **内置模型商店** | 一键下载/切换 VRM 角色模型，轻松管理形象库 |
| **原神模型优化** | 针对 hk4e 资源进行渲染调优，兼顾高画质与低资源占用 |
| **AI 对话集成** | 集成 undreamai v1.2.5，支持本地 CUDA 加速推理 |
| **全面中文本地化** | 内置原神字体，界面与文档全中文化 |
| **Mod 扩展支持** | 插件式功能模块，支持 `.me` 格式模组包，利于二次开发 |
| **互动与反馈** | 响应音乐律动、支持窗口悬挂、拖拽及触摸反馈 |
| **内存优化** | 修复多处资源残留问题，保障长时间运行的稳定性 |

---

## 🖼️ 预览展示 {#zh-预览展示}

<div align="center">
  <table>
    <tr>
      <td><img width="350" src="https://github.com/user-attachments/assets/f81cdb8c-a321-4ee9-bb5d-cdb424c03631" alt="主界面"></td>
      <td><img width="350" src="https://github.com/user-attachments/assets/0f0c5a2c-5abf-418d-bed4-86c49fce0619" alt="模型交互"></td>
    </tr>
    <tr>
      <td><img width="350" src="https://github.com/user-attachments/assets/9fa1ab55-3e3d-47f3-bcaa-3680d28054fb" alt="设置菜单"></td>
      <td><img width="350" src="https://github.com/user-attachments/assets/a8259b95-694c-4630-9d78-b2eb1f7c8ef0" alt="渲染效果"></td>
    </tr>
  </table>
</div>

---

## ⚡ 快速开始 {#zh-快速开始}

### 系统要求
- **操作系统**：Windows 10 / 11（由于引用 WinForm 等库，不支持 Linux）
- **GPU**：支持 DirectX 11 或更高版本
- **存储**：至少 500 MB 可用空间

### 安装与运行
1. 前往 [Releases](https://github.com/KlYang-dot/GI-Engine/releases) 页面下载最新版本的 `GI_Engine_v1.0.0.zip`。
2. 解压到任意文件夹。
3. 双击 `GI Engine.exe` 即可启动。
4. 首次启动后，通过**模型商店**下载模型，或将本地 `.vrm` 文件直接拖拽至程序窗口。

---

## 🔧 从源码构建 {#zh-从源码构建}

1. **克隆仓库**  
   ```bash
   git clone https://github.com/KlYang-dot/GI-Engine.git
   cd GI-Engine
   ```

2. **环境要求**：推荐使用 Unity 6000 或更高版本。

3. **打开项目**：在 Unity Hub 中添加项目路径。

4. **预览场景**：打开 `Assets/MATE ENGINE - Scenes/MainMenu.unity` 并点击运行。

> **注意**：项目使用 Git LFS 管理大文件（字体/AI库），克隆后请确保已安装 LFS 客户端。

---

## 📈 改进对比 {#zh-改进对比}

| 改进项 | 说明 |
| --- | --- |
| **内存泄漏修复** | 重构资源加载逻辑，彻底解决纹理与网格残留问题 |
| **模型商店** | 新增在线/离线模型包管理功能 |
| **渲染调优** | 针对原神角色定制渲染参数，视觉表现更佳 |
| **UI/UX 升级** | 调整界面配色，提升视觉舒适度与逻辑连贯性 |
| **稳定性修复** | 解决异步加载异常、COM 兼容性及动画过渡卡顿问题 |

---

## ❓ 常见问题解答 {#zh-常见问题解答}

**Q：是否支持将模型更换为《原神》以外的角色？**  
A：完全支持。GI Engine 兼容标准的 VRM 0.x / 1.x 格式。您可以从 VRoid Hub、Sketchfab 或其他 VRM 资源平台下载模型，并直接拖拽至程序窗口使用。

**Q：为什么运行后占用的内存较高？**  
A：考虑到渲染质量与物理模拟的实时性，引擎会预加载部分纹理资源。您可以前往“设置”面板中开启“低内存模式”或降低渲染质量，以适配配置较低的设备。

**Q：是否兼容 MateEngine 插件？**  
A：原插件部分代码未做改动，理论上完全兼容。主要改进集中在调用逻辑、功能扩展和渲染效果方面。

---

## 🧭 开发路线图 {#zh-开发路线图}

计划中的功能与优化方向（欢迎提交 issue 讨论）：
- [ ] 支持 GPT-SoVITS 语音合成
- [ ] 增加多屏互动与远程控制
- [ ] 增加 OpenAI 兼容 API 聊天
- [ ] 自定义模型语音
- [ ] 接入 Claw（只是一个设想）
- [ ] 角色自定义动画编辑器
- [ ] 轻量级 WebSocket 事件总线
- [ ] 支持与系统 UI 互动

---

## 🤝 贡献与许可证 {#zh-贡献与许可证}

我们欢迎任何形式的贡献！请参考 [CONTRIBUTING.md](https://www.google.com/search?q=CONTRIBUTING.md) 或直接发起 Pull Request。

**许可证**：GI Engine 基于 **MateEngine Pro License (v2.1)** 开源。

---

## 💬 联系与致谢 {#zh-联系与致谢}

- **提交反馈**：[GitHub Issues](https://github.com/KlYang-dot/GI-Engine/issues)
- **原作者**：特别感谢 [shinyflvre](https://github.com/shinyflvre) 开发的 [Mate-Engine](https://github.com/shinyflvre/Mate-Engine)
- **资源说明**：本项目中使用的《原神》美术资源版权归 ©miHoYo 所有，仅限个人学习与非商业二次创作，请勿用于商业用途或单独分发。

</div>

<div id="en" class="lang-content">

> **GI Engine: More Than Just a Desktop Pet.**  
> A lightweight desktop virtual companion engine for Chinese users, supporting VRM characters, a model store, Genshin Impact model rendering optimizations, and rich extensible features.

GI Engine is an open-source desktop companion application heavily modified from [Mate-Engine](https://github.com/shinyflvre/Mate-Engine). We fixed numerous memory leaks from the original project, added a **model store**, and comprehensively optimized rendering and UI interactions to provide a more stable, smoother, and more personalized virtual companion experience.

---

## 📑 Table of Contents

- [✨ Key Features](#en-key-features)
- [🖼️ Preview](#en-preview)
- [⚡ Quick Start](#en-quick-start)
- [🔧 Building from Source](#en-building-from-source)
- [📈 Improvements over Mate-Engine](#en-improvements)
- [❓ FAQ](#en-faq)
- [🧭 Roadmap](#en-roadmap)
- [🤝 Contributing & License](#en-contributing)
- [💬 Contact & Acknowledgements](#en-contact)

---

## ✨ Key Features {#en-key-features}

| Feature | Description |
| :--- | :--- |
| **VRM Character Support** | Full support for VRM 0.x / 1.x models (including expressions, blinking, lip-sync, and physics bones) |
| **Built-in Model Store** | One-click download / switch between VRM character models, easy avatar library management |
| **Genshin Impact Model Optimization** | Rendering tweaks for hk4e assets, balancing high visual quality and low resource usage |
| **AI Conversation Integration** | Integrated undreamai v1.2.5 with local CUDA acceleration |
| **Full Chinese Localization** | Built-in Genshin Impact fonts, fully localized UI and documentation |
| **Mod Extension Support** | Plugin-based functional modules, supports `.me` mod packages for easy secondary development |
| **Interaction & Feedback** | Reacts to music beats, window sticking, drag & drop, and touch feedback |
| **Memory Optimization** | Fixed multiple resource retention issues, ensuring long-term stability |

---

## 🖼️ Preview {#en-preview}

<div align="center">
  <tr>
    <tr>
      <td><img width="350" src="https://github.com/user-attachments/assets/f81cdb8c-a321-4ee9-bb5d-cdb424c03631" alt="Main UI"></td>
      <td><img width="350" src="https://github.com/user-attachments/assets/0f0c5a2c-5abf-418d-bed4-86c49fce0619" alt="Model Interaction"></td>
    </tr>
    <tr>
      <td><img width="350" src="https://github.com/user-attachments/assets/9fa1ab55-3e3d-47f3-bcaa-3680d28054fb" alt="Settings Menu"></td>
      <td><img width="350" src="https://github.com/user-attachments/assets/a8259b95-694c-4630-9d78-b2eb1f7c8ef0" alt="Rendering Effect"></td>
    </tr>
  </table>
</div>

---

## ⚡ Quick Start {#en-quick-start}

### System Requirements
- **OS**: Windows 10 / 11 (Linux not supported due to WinForm dependencies)
- **GPU**: DirectX 11 or higher
- **Storage**: At least 500 MB free space

### Installation & Running
1. Go to the [Releases](https://github.com/KlYang-dot/GI-Engine/releases) page and download the latest `GI_Engine_v1.0.0.zip`.
2. Extract the archive to any folder.
3. Double-click `GI Engine.exe` to launch.
4. After first launch, download models from the **Model Store** or simply drag & drop a local `.vrm` file onto the program window.

---

## 🔧 Building from Source {#en-building-from-source}

1. **Clone the repository**  
   ```bash
   git clone https://github.com/KlYang-dot/GI-Engine.git
   cd GI-Engine
   ```

2. **Environment**: Unity 6000 or later is recommended.

3. **Open the project** in Unity Hub.

4. **Preview the scene**: Open `Assets/MATE ENGINE - Scenes/MainMenu.unity` and press Play.

> **Note**: The project uses Git LFS for large files (fonts / AI libraries). Make sure you have the LFS client installed after cloning.

---

## 📈 Improvements over Mate-Engine {#en-improvements}

| Improvement | Description |
| --- | --- |
| **Memory Leak Fixes** | Refactored resource loading logic, completely eliminating texture and mesh leaks |
| **Model Store** | Added online/offline model package management |
| **Rendering Tuning** | Custom rendering parameters for Genshin Impact characters, better visuals |
| **UI/UX Upgrade** | Adjusted color scheme for improved visual comfort and logical flow |
| **Stability Fixes** | Resolved async loading exceptions, COM compatibility issues, and animation transition stutters |

---

## ❓ FAQ {#en-faq}

**Q: Can I replace the model with characters not from Genshin Impact?**  
A: Absolutely. GI Engine is compatible with standard VRM 0.x / 1.x models. You can download models from VRoid Hub, Sketchfab, or any other VRM source and drag them into the program window.

**Q: Why does the memory usage seem high?**  
A: To ensure real-time rendering quality and physics simulation, the engine preloads some texture resources. You can enable "Low Memory Mode" or reduce rendering quality in the Settings panel for lower-end devices.

**Q: Is it compatible with MateEngine plugins?**  
A: The plugin code remains untouched, so it should be fully compatible. Our main improvements are in core logic, features, and rendering effects.

---

## 🧭 Roadmap {#en-roadmap}

Planned features and optimizations (issues and discussions welcome):
- [ ] GPT-SoVITS voice synthesis support
- [ ] Multi-screen interaction and remote control
- [ ] OpenAI-compatible API chat integration
- [ ] Custom model voices
- [ ] Claw integration (just a concept for now)
- [ ] Character custom animation editor
- [ ] Lightweight WebSocket event bus
- [ ] System UI interaction support

---

## 🤝 Contributing & License {#en-contributing}

We welcome any form of contribution! Please refer to [CONTRIBUTING.md](https://www.google.com/search?q=CONTRIBUTING.md) or submit a Pull Request directly.

**License**: GI Engine is open-sourced under the **MateEngine Pro License (v2.1)**.

---

## 💬 Contact & Acknowledgements {#en-contact}

- **Feedback**: [GitHub Issues](https://github.com/KlYang-dot/GI-Engine/issues)
- **Original Author**: Special thanks to [shinyflvre](https://github.com/shinyflvre) for developing [Mate-Engine](https://github.com/shinyflvre/Mate-Engine)
- **Asset Disclaimer**: The Genshin Impact art assets used in this project are the property of ©miHoYo. They are for personal learning and non-commercial fan creation only. Do not use for commercial purposes or redistribute standalone.

</div>

<script>
function switchLang(evt, lang) {
  var i, tabcontent, tablinks;
  tabcontent = document.getElementsByClassName("lang-content");
  for (i = 0; i < tabcontent.length; i++) {
    tabcontent[i].classList.remove("active");
  }
  tablinks = document.getElementsByClassName("tablink");
  for (i = 0; i < tablinks.length; i++) {
    tablinks[i].classList.remove("active");
  }
  document.getElementById(lang).classList.add("active");
  evt.currentTarget.classList.add("active");
}
</script>
