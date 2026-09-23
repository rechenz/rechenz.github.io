---
title: TheDemiuge — Agent 驱动的 RPG 框架
published: 2026-06-05
order: 6
description: 用 UE5.8 + C++ 写一个分层解耦的 RPG 游戏本体，配一个 Go 实现的 AI 通信桥（ReAct + MCP + SSE）驱动 NPC 决策。后端已完成，当前重心在游戏本体的系统架构。
tags: [UE5, C++, Go, AI Agent, MCP, 游戏框架]
status: developing
link:
  - label: Bridge（Go 后端）
    icon: fa7-brands:github
    value: https://github.com/rechenz/TheDemiuge-Bridge
  - label: UE5 前端
    icon: fa7-brands:github
    value: https://github.com/rechenz/TheDemiugeUE5
---

## 它是什么

**一个 Agent 驱动的 RPG。** 由两个独立项目组成：

- **TheDemiugeUE5** —— UE5.8 + C++ 的游戏本体，当前主战场
- **TheDemiuge-Bridge** —— Go 写的 AI 通信桥，负责把大模型接进游戏（**已完成**）

一般的 AI-NPC demo 都停在「接上一个对话接口」。这里想做的是另一件事：让 NPC 的行为由 Agent 实时驱动，而这件事的前提是先有一个**系统之间足够解耦、状态足够可控**的游戏本体。所以重心放在了本体架构上。

## 游戏本体：L0 ~ L10 分层架构

`Docs/ARCHITECTURE.md` 定下的十一层结构，编译期依赖严格单向向下：

| 层 | 内容 |
|---|---|
| L0 | 基础层：时钟 / 句柄 / 事件总线 / 配置 |
| L1 / L2 | 实体 / 属性与修饰器（数值） |
| L3 / L4 | 条件引擎 / 物品（定义与实例分离） |
| L5 | 行为层：Effect —— **唯一的状态变更通道** |
| L6 / L7 | 世界标记 / 叙事任务 |
| L8 | **Agent 层**：NPC 决策接入 |
| L9 / L10 | 表现层 / 横切层（存档等） |

几条贯穿全局的约束：

- **时间是「可推进的累计量」，不是系统时钟**——睡觉跳过 8 小时，冷却要跟着结束
- 时效性数据存**绝对时间戳**，绝不存「剩余时长」
- **Effect 是唯一的状态变更通道**，事件总线只通知、不改状态
- Agent 四条硬约束：只读只提议 / 知识视图（信息隔离）/ 版本戳（乐观并发）/ 仲裁失败要有反馈回路
- 存档策略：全量快照 + Effect 日志增量

## AI 侧：Go 通信桥（已完成）

`TheDemiuge-Bridge`（Go + Hertz）7 层模块（server / service / agent / tool / llm / memory / config）：

- **ReAct 对话循环** + 工具调用，HTTP SSE 全链路流式返回
- **MCP 注册中心**：`mcp.Registry` 接口把依赖方向反转，工具注册与推理逻辑解耦，方便对接不同引擎
- 通信层设计成引擎无关——UE5 只是第一个前端

## 一个主动砍掉的方向

**模型端不做了。** 早期设想过端侧轻量模型 + LoRA 蒸馏做 NPC 记忆，后来判断过之后放弃：

- 玩家机器不可控——卡不卡、跑不跑得完、炸了能不能回滚，全都不可控
- 自研模型的复杂度会持续挤压真正难的部分：**游戏本体的系统架构**

所以 AI 侧收敛成「调外部 LLM API + 工程化的记忆管理」，接口留了可替换后端。取舍的标准很简单：哪些是这游戏非做不可的，哪些只是听着酷。

## 进度

- ✅ **Bridge 后端完成**：ReAct 对话 + MCP 注册中心 + SSE 全链路
- ✅ 游戏本体：架构文档 rev.2 + 进度跟进机制（事实快照脚本 + 人工台账分离）
- 🔄 **游戏本体 L0 基础层**：`GameClockSubsystem` 时间系统已跑通初版（固定步长 + 时间刻 Slot 事件容器 + 时间对齐）
- ⏭ 下一步：事件总线 → 配置数据层 → 句柄/生命周期，然后进 L1 实体与 L2 数值层

## 一点体会

先写清楚「哪些层不许互相依赖」，比先写出能跑的功能重要得多。这一个项目里最花时间的不是写代码，是决定**状态到底归谁管**。
