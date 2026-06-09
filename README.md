# GI Engine Project

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)
[![GitHub release](https://img.shields.io/github/v/release/KlYang-dot/GI-Engine)](https://github.com/KlYang-dot/GI-Engine/releases)

[English Version](README_EN.md)

> **GI Engine：不仅仅是桌面宠物。**
> 一款面向中文用户的轻量级桌面虚拟伙伴引擎，支持 VRM 角色、模型商店、原神模型优化渲染及丰富扩展功能。

GI Engine 是基于 [Mate-Engine](https://github.com/shinyflvre/Mate-Engine) 深度爆改的开源桌面伙伴应用。我们解决了原项目中的大量内存泄漏问题，新增了**模型商店**，并全面优化了渲染效果与 UI 交互，旨在为您提供更稳定、更流畅、更具个性化的虚拟伙伴体验。

---

## 目录
- [主要特性](#主要特性)
- [预览展示](#预览展示)
- [快速开始](#快速开始)
- [从源码构建](#从源码构建)
- [改进对比](#改进对比)
- [常见问题解答](#常见问题解答)
- [开发路线图](#开发路线图)
- [贡献与许可证](#贡献与许可证)
- [联系与致谢](#联系与致谢)

---

## 主要特性

| 功能 | 描述 |
| :--- | :--- |
| **VRM 角色支持** | 完整支持 VRM 0.x / 1.x 模型（含表情、眨眼、口型同步及物理骨骼） |
| **内置模型商店** | 一键下载/切换 VRM 角色模型，轻松管理形象库 |
| **原神模型优化** | 针对 hk4e 资源进行渲染调优，兼顾高画质与低资源占用 |
| **AI 对话集成** | 集成 undreamai v1.2.5，支持本地 CUDA 加速推理 |
| **全面中文本地化** | 内置原神字体，界面与文档全中文化 |
| **Mod 扩展支持** | 插件式功能模块，支持 .me 格式模组包，利于二次开发 |
| **互动与反馈** | 响应音乐律动、支持窗口悬挂、拖拽及触摸反馈 |
| **内存优化** | 修复多处资源残留问题，保障长时间运行的稳定性 |

---

## 预览展示

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

## 快速开始

### 系统要求
* **操作系统**：Windows 10 / 11（由于引用 WinForm 等库，不支持 Linux）
* **GPU**：支持 DirectX 11 或更高版本
* **存储**：至少 500 MB 可用空间

### 安装与运行
1. 前往 [Releases](https://github.com/KlYang-dot/GI-Engine/releases) 页面下载最新版本的 `GI_Engine_v1.0.0.zip`。
2. 解压到任意文件夹。
3. 双击 `GI Engine.exe` 即可启动。
4. 首次启动后，通过**模型商店**下载模型，或将本地 `.vrm` 文件直接拖拽至程序窗口。

---

## 从源码构建

1. **克隆仓库**：
  ` git clone [https://github.com/KlYang-dot/GI-Engine.git](https://github.com/KlYang-dot/GI-Engine.git)`
  `cd GI-Engine`


2. **环境要求**：推荐使用 Unity 6000 或更高版本。
3. **打开项目**：在 Unity Hub 中添加项目路径。
4. **预览场景**：打开 `Assets/MATE ENGINE - Scenes/MainMenu.unity` 点击运行。

> **注意**：项目使用 Git LFS 管理大文件（字体/AI库），克隆后请确保已安装 LFS 客户端。

---

## 改进对比

| 改进项 | 说明 |
| --- | --- |
| **内存泄漏修复** | 重构资源加载逻辑，彻底解决纹理与网格残留问题 |
| **模型商店** | 新增在线/离线模型包管理功能 |
| **渲染调优** | 针对原神角色定制渲染参数，视觉表现更佳 |
| **UI/UX 升级** | 调整界面配色，提升视觉舒适度与逻辑连贯性 |
| **稳定性修复** | 解决异步加载异常、COM 兼容性及动画过渡卡顿问题 |

---

## 常见问题解答 (FAQ)
**Q: 是否支持将模型更换为《原神》以外的角色？** A: 完全支持。GI Engine 兼容标准的 VRM 0.x / 1.x 格式。您可以从 VRoid Hub、Sketchfab 或其他 VRM 资源平台下载模型，并直接拖拽至程序窗口使用。
**Q: 为什么运行后占用的内存较高？** A: 考虑到渲染质量与物理模拟的实时性，引擎会预加载部分纹理资源。您可以前往“设置”面板中开启“低内存模式”或降低渲染质量，以适配配置较低的设备。
**Q: 是否兼容MateEngine插件？** A: 他写的插件部分一点没动，理论完全兼容，改的主要是调用、功能、渲染效果类的

---


---

## 贡献与许可证

我们欢迎任何形式的贡献！请参考 [CONTRIBUTING.md](https://www.google.com/search?q=CONTRIBUTING.md) 或直接发起 Pull Request。

**许可证**：GI Engine 基于 **MateEngine Pro License (v2.1)** 开源。

---

## 联系与致谢

* **提交反馈**：[GitHub Issues](https://github.com/KlYang-dot/GI-Engine/issues)
* **原作者**：特别感谢 [shinyflvre](https://github.com/shinyflvre) 开发的 [Mate-Engine](https://github.com/shinyflvre/Mate-Engine)。
* **资源说明**：本项目中使用的《原神》美术资源版权归 ©miHoYo 所有，仅限个人学习与非商业二次创作，请勿用于商业用途或单独分发。

```
