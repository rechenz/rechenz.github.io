---
title: TheDemiuge
published: 2026-06-05
order: 1
description: 引擎无关的「游戏 ↔ AI」通信框架 —— 让任何 UE5 项目引入插件、配个 JSON，就能让 NPC 接上大模型。
tags: [Go, UE5, AI Agent, MCP, 开源]
status: 进行中
link:
  - label: GitHub (UE5 前端)
    icon: fa7-brands:github
    value: https://github.com/rechenz/TheDemiugeUE5
---

## 为什么做这个

游戏大规模接入大模型是迟早的事，但中间那层「引擎 ↔ AI 服务」的通信与记忆管理还没有标准答案。TheDemiuge 想做的就是这层基础设施：**接口定义 + 通信协议 + 记忆管理管道**，引擎无关。

所以这里不做具体游戏，只搭框架。

## 架构

- **后端** `TheDemiuge-Bridge`（Go）：7 层模块（server / service / agent / tool / llm / memory / config），ReAct 对话循环 + MCP 工具注册中心
- **前端** `TheDemiugeUE5`（UE5 C++ 插件）：蓝图/C++ 对接层，NPC 生命周期管理、对话窗口、场景测试框架
- **通信**：HTTP SSE 全链路（chat 流式返回）+ MCP 注册中心（放在引擎端，进程内零转发）
- **记忆**：服务端全量托管（推理 + 记忆都在服务端，客户端只收发文本）
  - 为什么不用端侧轻量模型：玩家机器不可控，卡顿、跑不完、炸了没法回滚
  - 为什么把 LoRA 蒸馏降级成离线异步方案：实时蒸馏在消费级单卡上吞吐不足，是主动砍掉的路线，不是做不了

## 当前进度

- Go 后端骨架 + ReAct 对话 + SSE 全链路跑通
- MCP 解耦完成（依赖方向反转，mcp 包不依赖 backend）
- UE5 端：NPC 基类 / 测试管理器 / 对话窗口，`EDialogueBackend` 可切本地假后端与远程 HTTP
- 待做：UE5 端 MCP Server 实现、记忆系统工程版（摘要 + 检索）
