# rechenz.github.io

热尘的博客源码 —— https://rechenz.github.io

**Astro** + **[Firefly](https://github.com/CuteLeaf/Firefly)** 主题（Firefly 基于 [Fuwari](https://github.com/saicaca/fuwari) 二次开发），部署在 GitHub Pages。

---

## 本地开发

环境要求：**Node ≥ 22.23.0**、**pnpm ≥ 11**（Firefly 的 `preinstall` 脚本会直接拒绝 npm/yarn）。

```bash
pnpm install       # 安装依赖
pnpm dev           # 本地预览 → http://localhost:4321
pnpm build         # 完整构建到 dist/（含 LQIP、字体子集化、Pagefind 索引）
pnpm exec astro build   # 只跑 Astro 构建，快，日常够用
```

## 目录速查

| 路径 | 作用 |
|------|------|
| `src/content/posts/` | 文章（Markdown，**文件名即 URL 的 slug**） |
| `src/content/spec/` | 关于页 / 友链页 / 留言板内容 |
| `src/config/` | 全站配置：站点信息、导航、评论、看板娘、封面、动效… |
| `public/` | 静态资源：`images/`、`code/`、favicon、看板娘模型（`pio/`） |
| `tools/` | 迁移与校验脚本（见下） |
| `.github/workflows/deploy.yml` | push 到 `main` 自动构建并部署到 GitHub Pages |

## 写文章

文章 front matter 用 Astro 的字段名（注意是 `published`，不是 Hugo 的 `date`）：

```yaml
---
title: 文章标题
published: 2026-09-23T20:00:00+08:00
tags: [算法, 学习笔记]
category: 算法竞赛
image: "api"          # 用随机图当封面，见 src/config/coverImageConfig.ts
---
```

正文里引用站内图片写绝对路径，例如 `![](/images/<目录>/foo.png)`。

## 从 Hugo 迁移的记录

2026-09-23，本站从 **Hugo + hugo-theme-stack** 整体迁移到 **Astro + Firefly**。

- 迁移脚本：`tools/migrate-to-firefly.mjs`（Hugo page bundle → Astro content collection，含图片搬运与链接改写）
- **旧链接一条都没失效**：文章地址保持 `/post/<slug>/`，因为 Firefly 默认用 `/posts/`，用 `tools/apply-post-prefix-patch.mjs` 改成了 `/post/`
- 校验脚本：`tools/verify-urls.mjs` 会拿迁移前导出的 URL 基线逐条比对构建产物
- 迁移前的 Hugo 版本保存在 tag `hugo-final` 和分支 `hugo-legacy` 里

```bash
node tools/verify-urls.mjs        # 校验旧链接是否全部可用
```

## 同步上游主题更新

Firefly 是「整站模板」而不是可插拔主题，所以本站的 `src/` 就是它的代码副本。想跟上上游：

```bash
git remote add firefly https://github.com/CuteLeaf/Firefly.git   # 只需一次
git fetch firefly
git merge firefly/master        # 解决冲突后
node tools/apply-post-prefix-patch.mjs   # 重新打上 /post/ 前缀补丁（幂等）
pnpm build && node tools/verify-urls.mjs # 验证
```

升级时要留意的本站本地改动：`/post/` 前缀补丁、`src/config/` 里的站点配置、`src/content/spec/` 里的关于页与友链页。

## 许可

主题代码来自 [CuteLeaf/Firefly](https://github.com/CuteLeaf/Firefly)（MIT，见 `LICENSE`）；文章内容版权归作者所有。
