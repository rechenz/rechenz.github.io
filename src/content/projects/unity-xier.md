---
title: 西二在线 Unity & UE 技术部 2026 纳新站
published: 2026-08-10
order: 5
description: 为福州大学西二在线工作室 Unity & UE 技术部做的纳新介绍站，Tailwind 单页无构建，部署在 GitHub Pages。
tags: [Tailwind, HTML, 静态站]
status: 已完成
link:
  - label: 线上站点
    icon: material-symbols:link
    value: https://rechenz.github.io/unity-xier/
  - label: GitHub
    icon: fa7-brands:github
    value: https://github.com/rechenz/unity-xier
---

## 背景

2026 学年我接任福州大学西二在线工作室 **Unity & UE 技术部**组长，负责 26 届纳新全流程。这是配套的纳新介绍站。

## 技术选型

用 **Tailwind 单页 + 无构建** 的方案，理由很实际：

- 纳新的同学要能自己改文案、换图，不需要懂构建工具链
- 没有 npm 依赖，改完直接推 GitHub Pages，不用等 CI
- 图片位提前留好占位，同名覆盖 `images/` 就能换图

## 内容结构

- 双引擎介绍（Unity + UE5），各方向（程序 / 策划 / 美术）的考核路径
- 历年作品展示区
- 报名与合作方式

站点原来还兼着组名改版（Unity 组 → **Unity & UE 技术部**）的对外口径同步，飞书文档、海报、官网标题一起改的。
