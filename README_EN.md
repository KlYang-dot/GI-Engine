# GI Engine Project

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)
[![GitHub release](https://img.shields.io/github/v/release/KlYang-dot/GI-Engine)](https://github.com/KlYang-dot/GI-Engine/releases)
[简体中文版本](README.md)

> **GI Engine: More than just a desktop mascot.**
> A lightweight desktop virtual companion engine, featuring support for VRM models, an in-app model store, optimized rendering for Genshin Impact assets, and rich extension capabilities.

GI Engine is a deep-refactoring of [Mate-Engine](https://github.com/shinyflvre/Mate-Engine). We have addressed major memory leaks from the original project, added an **in-app model store**, and fully optimized the rendering and UI interaction to provide a more stable, smooth, and personalized experience.

---

## Table of Contents
- [Key Features](#key-features)
- [Preview](#preview)
- [Quick Start](#quick-start)
- [Build from Source](#build-from-source)
- [Improvements](#improvements)
- [FAQ](#faq)
- [Roadmap](#roadmap)
- [Contribution & License](#contribution--license)

---

## Key Features

| Feature | Description |
| :--- | :--- |
| **VRM Support** | Full support for VRM 0.x / 1.x models (physics, lip-sync). |
| **In-app Model Store** | One-click download/switch for VRM character models. |
| **Genshin Optimization** | Customized rendering for hk4e assets for high performance. |
| **AI Integration** | Integrated with undreamai v1.2.5, supports local CUDA inference. |
| **Localization** | Full Chinese localization with high-quality fonts. |
| **Mod Support** | Plugin-based modules, supporting .me mod packages. |

---

## Preview

<div align="center">
  <table>
    <tr>
      <td><img width="350" src="https://github.com/user-attachments/assets/f81cdb8c-a321-4ee9-bb5d-cdb424c03631" alt="UI"></td>
      <td><img width="350" src="https://github.com/user-attachments/assets/0f0c5a2c-5abf-418d-bed4-86c49fce0619" alt="Interaction"></td>
    </tr>
    <tr>
      <td><img width="350" src="https://github.com/user-attachments/assets/9fa1ab55-3e3d-47f3-bcaa-3680d28054fb" alt="Settings"></td>
      <td><img width="350" src="https://github.com/user-attachments/assets/a8259b95-694c-4630-9d78-b2eb1f7c8ef0" alt="Rendering"></td>
    </tr>
  </table>
</div>

---

## Quick Start

### System Requirements
* **OS**: Windows 10 / 11
* **GPU**: DirectX 11 or higher
* **Storage**: At least 500 MB free space

### Installation
1. Download the latest version from [Releases](https://github.com/KlYang-dot/GI-Engine/releases).
2. Extract the archive.
3. Run `GI Engine.exe`.
4. Use the **Model Store** or drag-and-drop your `.vrm` files.

---

## Build from Source
1. **Clone**: `git clone https://github.com/KlYang-dot/GI-Engine.git`
2. **Environment**: Unity 2022.3 LTS or higher recommended.
3. **Note**: Project uses Git LFS for large assets. Please ensure LFS is installed.

---

## Improvements
| Improvement | Description |
| :--- | :--- |
| **Memory Fixes** | Refactored resource loading to eliminate leaks. |
| **Model Store** | New integrated online/offline model manager. |
| **Rendering** | Customized parameters for Genshin Impact models. |
| **UI/UX** | Refined color schemes and logic. |
| **Stability** | Fixed async loading and animation stuttering. |

---

## FAQ
**Q: Does it support non-Genshin models?** A: Yes, all standard VRM 0.x / 1.x models are supported.
**Q: AI hardware requirements?** A: NVIDIA GPU with 4GB+ VRAM recommended for local inference.

---

## Roadmap
- [ ] **v1.1.0** - AI upgrade (custom personalities).
- [ ] **v1.2.0** - Microphone input & interactive lip-sync.
- [ ] **v1.3.0** - Multi-platform support.
- [ ] **v2.0.0** - Plugin Marketplace.

---

## Contribution & License
We welcome all contributions! Please refer to [CONTRIBUTING.md](CONTRIBUTING.md).
**License**: GI Engine is open-sourced under the **MateEngine Pro License (v2.1)**.

```
