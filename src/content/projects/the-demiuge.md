---
title: TheDemiuge — AI Agent 驱动的游戏 NPC 框架
published: 2026-06-05
order: 1
description: 自研「游戏 ↔ 大模型」通信框架：Go 实现 ReAct 对话循环、Agent 工具调用与 MCP 注册中心，把 Agent 工具集抽象成 NPC 的实时控制层。
tags: [Go, AI Agent, MCP, UE5, 开源]
status: 进行中
link:
  - label: Bridge（Go 后端）
    icon: fa7-brands:github
    value: https://github.com/rechenz/TheDemiuge-Bridge
  - label: UE5 前端
    icon: fa7-brands:github
    value: https://github.com/rechenz/TheDemiugeUE5
---

## 想解决什么

游戏大规模接入大模型是迟早的事，但中间那层「引擎 ↔ AI 服务」的通信与记忆管理还没有标准答案。TheDemiuge 做的就是这层基础设施：**接口定义 + 通信协议 + 记忆管理管道**，引擎无关——UE5 插件只是前端之一。

所以这里不做具体游戏，只搭框架。

## 核心思路

**把 Agent 的工具集抽象成 NPC 的实时控制层。**

NPC 通过工具调用感知环境、做出决策并执行行动，行为由大模型 Agent 实时驱动，而不是预编程的固定逻辑树。这也是它和「数字人」路线的区别：更轻量，落地在游戏里。

## 架构

- **后端** `TheDemiuge-Bridge`（Go + Hertz）：7 层模块（server / service / agent / tool / llm / memory / config），ReAct 对话循环 + MCP 工具注册中心
- **前端** `TheDemiugeUE5`（UE5 C++ 插件）：蓝图 / C++ 对接层，NPC 生命周期管理、对话窗口、场景测试框架
- **通信**：HTTP SSE 全链路（chat 流式返回）+ MCP 注册中心放在引擎端，进程内零转发
- **解耦**：`mcp.Registry` 接口反转依赖方向，工具注册与推理逻辑分离，方便对接不同引擎

## 记忆系统的取舍

记忆方案最终定为**服务端全量托管**（推理 + 记忆都在服务端，客户端只收发文本）。

- **为什么不用端侧轻量模型**：玩家机器不可控——卡顿、跑不跑得完不知道、炸了没法回滚
- **为什么把 LoRA 蒸馏降级成离线 / 异步**：实时蒸馏在消费级单卡上吞吐不足，是**判断过之后主动砍掉**的路线；离线蒸馏仍然可用，接口留了可替换的后端

## 进度

- ✅ Go 后端骨架 + ReAct 对话 + SSE 全链路跑通
- ✅ MCP 解耦完成
- ✅ UE5 端：NPC 基类 / 测试管理器 / 对话窗口，对话后端可切（本地假后端 / 远程 HTTP）
- ⏳ UE5 端 MCP Server 实现；记忆系统工程版（摘要 + 检索）
