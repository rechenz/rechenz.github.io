---
title: TheTravel — 3D RPG 冒险游戏
published: 2026-05-01
order: 5
description: 与合作开发者共同制作的 3D RPG，完整搭建了场景、流程、任务、对话、存档、物品系统全链路，并自研水体模拟着色器。
tags: [Unity, C#, ShaderLab, 3D RPG]
status: published
link:
  - label: GitHub
    icon: fa7-brands:github
    value: https://github.com/rechenz/TheTravel
---

## 做了什么

一个完整的 3D RPG 冒险游戏，与合作开发者共同制作。**搭起了整套 RPG 框架**：场景、流程、任务、对话、存档、物品系统全链路打通。

我负责的部分：

- **场景搭建与流程制作**：关卡布局、剧情推进的流程控制
- **任务系统设计与实现**：任务接取、状态流转、完成条件判定
- **全部测试与调试**
- **自研水体模拟着色器**（ShaderLab / HLSL）
- 交互 UI、物品获得提示等系统

## 技术栈

Unity + C# + ShaderLab（HLSL）。

其中最花时间的是水体着色器——要在实时性能下做出可信的水面波动、折射与反射，最后是按屏幕空间做法压下了开销。

## 相关的博客记录

任务系统的设计思路后来单独整理成了一篇：[基于 DAG 的任务管理系统](/post/基于dag的任务管理系统/)，里面把任务依赖关系抽象成有向无环图来管理，代码也一起放出来了。
