# 关于我

我是 **热尘**（郑佳轩），福州大学数智实验班大二学生，做游戏开发，也折腾 AI。

## 现在在做什么

**TheDemiuge —— 一个 Agent 驱动的 RPG。** 它由两部分组成：

- **TheDemiugeUE5（游戏本体）** —— UE5.8 + C++。按 L0~L10 分层架构设计，依赖严格单向向下，已实现时间系统（固定步长 + 时间刻事件容器），基础层推进中。
- **TheDemiuge-Bridge（AI 通信桥）** —— Go + Hertz。ReAct 对话循环、MCP 工具注册中心、SSE 全链路流式返回，已完成。

出发点是一个判断：游戏大规模接入大模型是迟早的事，而中间那层「引擎 ↔ AI 服务」的通信与记忆管理还没有标准答案。所以先把游戏本体做扎实，再把 Agent 接进来——而不是停在「接一个对话接口」的 demo。

这条路线的下一站，是大三进入米哈游实习。

## 技术栈

| 方向 | 内容 |
|---|---|
| 语言 | C++ / C# / Go / Python |
| 游戏 | UE5（C++）、Unity（C#）、ShaderLab |
| 后端 | Go（Hertz）、HTTP SSE、MCP |
| AI | PyTorch、Transformer、ReAct / RAG / Function Calling |

## 一些经历

- **算法竞赛** —— NOIP 2023 省一等奖，ACM-ICPC 省级银奖。
- **深度学习** —— 自学完成 Stanford CS231n 与 CS224n，从零基础 Python 一路学到 Transformer。
- **游戏项目** —— 3D RPG《TheTravel》（自研水体着色器、DAG 任务系统）；GameJam 作品《DOT 宇宙探索》（5 人团队，程序负责人兼项目负责人）。
- **研究实验** —— weight-masking-experiment：探索训练初期对网络的约束如何影响最终性能，在 CIFAR-10 上相比 baseline 提升 1.65%。
- **工作室** —— 福州大学西二在线工作室 Unity & UE 技术部组长。

## 关于本站

本站用 [Astro](https://astro.build/) 构建，主题 [Firefly](https://github.com/CuteLeaf/Firefly)（基于 [Fuwari](https://github.com/saicaca/fuwari) 二次开发），部署在 GitHub Pages。

2026 年 9 月之前这里是 Hugo + Stack，迁移到 Astro 之后所有旧文章链接保持不变。

博客主要写算法题解、UE5 学习笔记和项目复盘，欢迎随便翻。

## 找我

- GitHub：[rechenz](https://github.com/rechenz)
- Email：13244571368@qq.com

想聊游戏开发或 AI，随时来信。
