---
title: rechenz.github.io — 博客换栈：Hugo → Astro
published: 2026-09-23
order: 1
description: 把博客从 Hugo + Stack 整体迁到 Astro + Firefly，33 篇文章迁移、46 条旧链接零失效（含带 + 号的 slug 与标签目录页），并留下可重放的补丁脚本与校验脚本。
tags: [Astro, 前端, 工程化, 开源]
status: developing
link:
  - label: GitHub
    icon: fa7-brands:github
    value: https://github.com/rechenz/rechenz.github.io
---

## 干了什么

有一天觉得原来的 Hugo 主题功能太少，于是把整个博客**换栈**了——从 Hugo + hugo-theme-stack 迁到 Astro + Firefly。不是换主题，是换了框架和构建链。

## 硬约束：旧链接一条都不能死

迁移最容易翻车的地方是 URL。旧站有 46 条地址（33 篇文章 + 列表页 + 归档页 + 7 个标签页），里面有中文、有 `+` 号，而新主题的默认规则全都不一样：

| 差异 | 处理 |
|------|------|
| 主题默认文章前缀 `/posts/`，旧站是 `/post/` | 写了一个**可重放的补丁脚本**改回 `/post/`，主题升级后重跑即可 |
| 主题归档页 `/archive/`，旧站 `/archives/` | 补了一个兼容页面 |
| 主题标签用 `?tag=` 客户端筛选，旧站是 `/tags/<标签>/` 目录页 | 补了动态路由页，并按旧站规则把 ASCII 标签名小写化（`AI` → `/tags/ai/`） |
| Astro 的 slug 化会吃掉 `+` 号 | 自定义 `generateId` 保住原始文件名 |

## 留下的工具

- `migrate-to-firefly.mjs` — 页面结构 + front matter + 图片引用的整体迁移
- `apply-post-prefix-patch.mjs` — 上面那个可重放的路由补丁（幂等）
- `verify-urls.mjs` + `legacy-urls.txt` — 拿迁移前的 46 条 URL 基线逐条比对构建产物，**全绿才算没坏**

## 一个值得记的教训

第一版校验脚本把文章目录写成了旧框架的路径，导致「站内引用检查」整段空转——每次都打印 ✅，但**一个引用都没真检查**。是后来做代码审计时抓出来的。

**验收工具的假阳性比没有工具更危险**，因为它会让你以为已经验过了。
