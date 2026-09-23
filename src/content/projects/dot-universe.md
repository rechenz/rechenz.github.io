---
title: DOT 宇宙探索 — GameJam 2026 团队作品
published: 2026-01-15
order: 3
description: 2D 叙事解谜冒险，用康威生命游戏模拟宇宙。5 人团队的程序负责人兼项目负责人，自研状态管理、对话、异步场景加载等核心系统。
tags: [Unity, C#, GameJam, 2D]
status: 已完成
link:
  - label: GitHub
    icon: fa7-brands:github
    value: https://github.com/rechenz/gamejam
---

## 是什么

GameJam 2026 的参赛作品，5 人团队，我担任**程序负责人 + 项目负责人**（架构设计、核心系统实现、测试）。

2D 叙事解谜冒险：用**康威生命游戏**模拟宇宙，一个失忆的主角和一个点阵少女在其中的哲学冒险。

## 核心机制

- **点阵视域 / 点阵化双视角系统**：两套视角切换构成解谜基础
- 密码锁解谜、宇宙漏洞、多分支多结局

## 我写的系统

- `SimpleStateManager` — 全局状态与存档
- `DialogueManager` — 多角色对话
- `SceneLoader` — 异步场景加载
- `PasswordLockManager` — 密码锁

GameJam 的限制反而逼出了架构上的取舍：没有时间做美术，就把预算全压在机制和状态管理上，让程序结构能扛住后期加内容。
