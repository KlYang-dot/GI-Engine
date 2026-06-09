# GI Engine Project

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)
[![GitHub release](https://img.shields.io/github/v/release/KlYang-dot/GI-Engine)](https://github.com/KlYang-dot/GI-Engine/releases)

> 一款面向中文用户的轻量级桌面虚拟伙伴引擎  
> 支持 VRM 角色、模型商店、原神模型优化渲染及丰富扩展功能。

GI Engine 是基于 [Mate-Engine](https://github.com/shinyflvre/Mate-Engine) 深度爆改的开源桌面伙伴应用。相比原项目，我们解决了大量内存泄漏问题，增加了**模型商店**，优化了渲染效果和用户界面，让虚拟伙伴体验更稳定、更流畅、更个性化。

---

## 支持本项目

如果你喜欢 GI Engine，欢迎给仓库点个 Star，也欢迎通过 Issue 或 PR 参与贡献。你的支持是我们持续更新的动力！

---

## 主要特性

| 功能 | 描述 |
|------|------|
| VRM 角色支持 | 完整支持 VRM 0.x / 1.x 模型，包含表情、眨眼、口型同步及物理骨骼驱动 |
| 内置模型商店 | 一键下载/切换 VRM 角色模型，扩展你的虚拟伙伴形象库 |
| 原神模型优化渲染 | 针对原神角色模型（如 hk4e 资源）进行渲染调优，画质与性能兼得 |
| AI 对话集成 | 集成 undreamai v1.2.5 运行时，支持 Linux/Windows CUDA 加速本地推理 |
| 全面中文本地化 | UI、注释、文档均为中文，内置高清亚洲字体（hk4e_zh-cn SDF.asset） |
| Mod 扩展支持 | 插件式功能模块，支持 .me 格式模组包，便于二次开发 |
| 随音乐起舞 | 角色可响应音乐节奏，播放舞蹈动画 |
| 桌面互动 | 角色可悬挂于窗口边缘、任务栏，支持拖拽、触摸反馈等 |
| 丰富设置项 | FPS 控制、始终置顶、迷你模式、屏幕保护、粒子效果等 |
| 内存优化 | 修复原项目中的多处内存泄漏，长时间运行更稳定 |

---

## 预览
<img width="797" height="823" alt="image" src="https://github.com/user-attachments/assets/f81cdb8c-a321-4ee9-bb5d-cdb424c03631" />

<img width="403" height="626" alt="image" src="https://github.com/user-attachments/assets/0f0c5a2c-5abf-418d-bed4-86c49fce0619" />
<img width="869" height="858" alt="image" src="https://github.com/user-attachments/assets/9fa1ab55-3e3d-47f3-bcaa-3680d28054fb" />
<img width="2559" height="1439" alt="image" src="https://github.com/user-attachments/assets/a8259b95-694c-4630-9d78-b2eb1f7c8ef0" />


---

## 快速开始

### 系统要求

- 操作系统：Windows 10 / 11（推荐），由于引用WinForm等库 Linux 无法支持
- GPU：支持 DirectX 11 或更高版本
- 存储：至少 500 MB 可用空间

### 安装与运行

1. 前往 [Releases](https://github.com/KlYang-dot/GI-Engine/releases) 页面下载最新版本的 `GI_Engine_v1.0.0.zip`
2. 解压到任意文件夹
3. 双击 `GI Engine.exe` 启动
4. 首次启动后，通过**模型商店**下载你的第一个 VRM 模型，或拖拽本地 `.vrm` 文件到程序窗口

---

## 从源码构建

如果你想自行编译或参与开发：

1. **克隆仓库**
   ```bash
   git clone https://github.com/KlYang-dot/GI-Engine.git
   cd GI-Engine
   ```

2. **使用 Unity 打开**
   - 推荐 Unity 2022.3 LTS 或更高版本
   - 在 Unity Hub 中选择“添加项目”，指向本仓库文件夹

3. **打开主场景**
   - 场景路径：`Assets/MATE ENGINE - Scenes/MainMenu.unity`
   - 点击运行按钮预览

> 注意：项目中的大文件（字体、AI 库等）已使用 Git LFS 管理，克隆后请确保已安装 LFS 客户端。

---

## 与原项目 Mate-Engine 的改进对比

| 改进项 | 说明 |
|--------|------|
| 内存泄漏修复 | 重构资源加载/卸载逻辑，彻底解决纹理、网格等残留问题 |
| 模型商店 | 新增内置模型商店，支持在线/离线模型包管理 |
| 渲染优化 | 针对原神角色模型定制渲染器参数 |
| 界面配色优化 | 全面调整 UI 色调，提升视觉舒适度 |
| 大量 Bug 修复 | 解决原项目中异步加载异常、动画过渡卡顿、MTA STA的COM兼容性等问题 |
| 中文优先 | 注释、文档、界面全部中文化，降低使用门槛 |

---

## 参与贡献

我们欢迎任何形式的贡献！

1. Fork 本仓库
2. 创建你的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交你的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

---

## 许可证

GI Engine 基于 **MIT License** 开源。详见 [LICENSE.md](LICENSE.md) 文件。

---

## 致谢

- **Mate-Engine** – 原项目作者 [shinyflvre](https://github.com/shinyflvre) 及所有贡献者  
  原项目地址：[https://github.com/shinyflvre/Mate-Engine](https://github.com/shinyflvre/Mate-Engine)
- **VRM Consortium** – VRM 模型标准
- **Unity 社区** – 提供技术支持
- 所有参与内测和反馈问题的用户

---

## 联系与讨论

- 提交 Issue：[GitHub Issues](https://github.com/KlYang-dot/GI-Engine/issues)

**本项目中使用了来自 《原神》（miHoYo/HoYoverse）的模型、字体等美术资源。这些资源仅用于非商业二次创作，版权归米哈游所有。请勿将上述资源用于商业用途或单独提取分发。**
---

