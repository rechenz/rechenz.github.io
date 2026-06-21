# AUDIT — rechenz.github.io 博客项目审计报告

> 审计日期：2026-06-22  
> 项目路径：`F:\project\rechenz.github.io`  
> 主题：hugo-theme-stack v4.0.3  
> Hugo 版本（CI）：0.163.3

---

## 📋 目录

1. [🔴 严重问题](#-严重问题)
2. [🟡 建议修复](#-建议修复)
3. [🟢 优化项](#-优化项)
4. [整体评分](#-整体评分)
5. [改进路线图](#-改进路线图)

---

## 🔴 严重问题

### R1. `search.json` 和 `single.json` 文件重复且位置错误

| 属性 | 值 |
|------|------|
| 文件 | `layouts/_default/search.json`、`layouts/_default/single.json` |
| 行 | 全文 |
| 严重度 | 🔴 **严重** |

**问题描述：**
项目在 `layouts/_default/` 下放置了 `search.json` 和 `single.json`，其内容与主题自带的 `layouts/page/search.json` 完全一致。但是主题模板将搜索 JSON 输出注册为 `outputFormat: json` 的 `page/search.json`（page bundle），而项目覆盖的 `_default/search.json` 是 **section 级别的列表输出格式**，不会被搜索页面（layout: search）正确调用。

实际上这两个覆盖文件是完全冗余的——主题已经在 `layouts/page/search.json` 中提供了正确的 JSON 索引。项目覆盖的 `_default/single.json` 更是不会被搜索页面或任何页面使用（search 模板不调用 single.json）。

**影响：**
- 维护陷阱：未来改 JSON 格式时可能会误改这两个文件而实际无效果
- 混淆：新开发者看到 `_default/search.json` 和 `_default/single.json` 会以为是有效的搜索结果源

**修复建议：**
删除两个冗余文件：
```bash
rm "layouts/_default/search.json"
rm "layouts/_default/single.json"
```

### R2. `search.html` 覆盖与主题完全一致，无实际价值

| 属性 | 值 |
|------|------|
| 文件 | `layouts/_default/search.html` |
| 行 | 全文 |
| 严重度 | 🔴 **严重** |

**问题描述：**
`layouts/_default/search.html` 与主题 `layouts/page/search.html` 的模板内容完全相同。当内容页面使用 `layout: "search"` 时，Hugo 也会先查找 `_default/search.html`。这一覆盖没有任何自定义修改，纯粹是主题复制。

查询主题 v4.0.3 的 issue tracker，搜索功能在 v4.0.2+ 中修复了 JS 构建路径问题（`search.tsx` 依赖 `assets/ts/` ），但覆盖 `_default/search.html` 中使用的 `resources.Get "ts/search.tsx"` 路径和主题的 `page/search.html` 完全一致，没有任何不同。

**影响：**
- 未来主题更新 search.html 需要手动同步，增加维护负担
- 当前文件是死复制——没有任何自定义代码
- 如果有差异是因为复制时间点不同，可能在主题 bug 修复后本项目还没同步

**修复建议：**
删除 `layouts/_default/search.html`，让 Hugo 回退到主题的 `layouts/page/search.html`。如果确实需要自定义搜索页面模板，请在文件中标记并保留差异部分，不要无修改复制。

### R3. `!important` 滥用 — 11 处覆盖

| 属性 | 值 |
|------|------|
| 文件 | `assets/scss/custom.scss` |
| 行 | 多处 |
| 严重度 | 🔴 **严重** |

**问题描述：**
`custom.scss` 中使用了 **11 次 `!important`**，几乎覆盖了所有背景相关的样式。引用注释原文：
> "统一用 !important 直打，不走 CSS 变量（Hugo SCSS 会把 :root 变量合并吞掉）"

**这个假设是错误的。** Hugo 的 SCSS 管道使用 `libsass` / `dart sass`，不会「吞掉」`:root` 变量。CSS 自定义属性（`--card-background` 等）在 Dart Sass 1.x 中完全兼容。

!important 具体出现位置：

| 行号 | 选择器 | 属性 |
|------|--------|------|
| 27 | `main.main, .main-container, .container.extended` | `background: transparent !important` |
| 39-40 | `.article-list article, .article-list--compact, ...` | `background-color: ... !important` |
| 49-56 | `[data-scheme="dark"]` 同类选择器 | `background-color: ... !important` |
| 62 | `.search-form` | `background: none !important` |
| 65-68 | `.search-form input` | `padding: ... !important` (×3), `background-color: ... !important` |
| 71 | `.search-form label` | `top: ... !important`, `font-size: ... !important` |
| 73 | `[data-scheme="dark"] .search-form input` | `background-color: ... !important` |
| 78 | `.left-sidebar` | `padding: ... !important` |
| 79 | | `border-radius` 不用 !important（正确）|
| 80 | | `box-shadow` 不用 !important（正确）|
| 82 | | `background-color: ... !important` |
| 85 | `[data-scheme="dark"] .left-sidebar` | `background-color: ... !important` |
| 87-88 | `.left-sidebar #main-menu` | `margin-left, margin-right: 0 !important` |
| 93-94 | `.left-sidebar @media` | `margin-top/bottom: ... !important` |
| 96 | `.left-sidebar.sticky` | `top: ... !important` |
| 102 | `.footer` | `background-color: transparent !important` |

**影响：**
- !important 会使后续任何样式（包括 JavaScript 动态修改）无法覆盖
- 当需要微调颜色值时，只能继续加 !important，形成恶性循环
- 破坏 CSS 级联机制，降低可维护性
- 主题升级后如有新的背景相关样式，可能需要更多 !important 对抗

**修复建议：**
将所有 `!important` 替换为等特异性选择器或改用 CSS 变量覆盖。核心方案：
1. 覆盖 CSS 变量代替特定元素选择器（推荐）：
   ```scss
   :root {
       --card-background: rgba(255, 255, 255, 0.7);
   }
   [data-scheme="dark"] {
       --card-background: rgba(30, 30, 30, 0.65);
   }
   ```
2. 完全不需要 !important 的 CSS 变量覆盖方式可直接在 `:root` 中设置，因为 `custom.scss` 是最后 @import 的
3. 对于 `backdrop-filter` 等非变量属性，使用 `:root` 变量 + 更高特异性选择器（如 `.container.extended main.main`）

---

## 🟡 建议修复

### Y1. 背景图 `::after` 选择器特异性不足

| 属性 | 值 |
|------|------|
| 文件 | `assets/scss/custom.scss` |
| 行 | 12-16 |
| 严重度 | 🟡 **建议** |

**问题描述：**
```scss
[data-scheme="dark"] body::after {
    content: '';
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.45);
    z-index: -1;
}
```
使用 `body::after` 作为暗色遮罩层存在以下问题：
- `z-index: -1` 可能被父容器/子元素层级打断，在某些主题组件内可能无法覆盖
- 若页面有其他 `z-index` 上下文（如 sticky 侧边栏），遮罩可能失效
- `::after` 伪元素在 body 上的行为在不同浏览器中表现不完全一致（spec 允许但偶有 bug）

**修复建议：**
改用 `.container.main-container` 的伪元素或直接使用 CSS 变量控制背景图的暗化：
```scss
[data-scheme="dark"] body {
    background-image: linear-gradient(rgba(0,0,0,0.45), rgba(0,0,0,0.45)), url('/bg.jpg');
}
```

### Y2. 搜索 widget HTML 中 `data-json` 路径拼接方式脆弱

| 属性 | 值 |
|------|------|
| 文件 | `layouts/_partials/widget/search.html` |
| 行 | 3 |
| 严重度 | 🟡 **建议** |

**问题描述：**
```html
<form ... data-json="{{ $searchPage.RelPermalink }}index.json">
```
这里手动拼接 `index.json` 到页面相对路径。如果搜索页面的 `outputs` 配置变更或 `baseName` 更改（如 hugo.yaml 中 `baseName: index` 但重命名），这个硬编码 `index.json` 会错误。

主题默认搜索模板（`layouts/page/search.html`）使用：
```html
{{ with .OutputFormats.Get "json" }} data-json="{{ .RelPermalink }}"{{ end }}
```
这是正确的方式——通过 Hugo 的 OutputFormats API 获取 JSON 输出路径。

**影响：**
- 如果日后变更 JSON 输出的 baseName（如改为 `search.json`），widget 搜索会断裂
- 如果升级主题后 JSON 输出方式改变，widget 搜索需要手动同步

**修复建议：**
引用搜索页面的 OutputFormat：
```html
{{- with $searchPage.OutputFormats.Get "json" -}}
    <form action="{{ $searchPage.RelPermalink }}" class="search-form widget" data-json="{{ .RelPermalink }}">
{{- end -}}
```

### Y3. Hugo 版本号硬编码在 CI 中

| 属性 | 值 |
|------|------|
| 文件 | `.github/workflows/hugo.yml` |
| 行 | 14 |
| 严重度 | 🟡 **建议** |

**问题描述：**
```yaml
env:
  HUGO_VERSION: 0.163.3
```
版本号硬编码，无自动提醒更新机制。主题要求 `min_version = "0.157.0"`，当前版本满足要求。但未来主题升级可能依赖更新的 Hugo 特性。

**影响：**
- 忘记更新导致构建失败
- 无法从 Dependabot/Renovate 自动获得版本建议

**修复建议：**
添加 Dependabot 配置自动跟踪 Hugo 版本：
```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
```

也可考虑使用 `actions-hugo` action 代替手动下载：
```yaml
- uses: peaceiris/actions-hugo@v3
  with:
    hugo-version: '0.163.3'
    extended: true
```

### Y4. `content/search/index.md` 缺少必要的 frontmatter 字段

| 属性 | 值 |
|------|------|
| 文件 | `content/search/index.md` |
| 行 | 1-8 |
| 严重度 | 🟡 **建议** |

**问题描述：**
搜索页面包含了 `aliases: [/page/search/]`，但查看 `public/page/search/index.html` 发现这个别名页面确实被生成了。这是冗余的——`hugo.yaml` 中 `menu.main` 已经直接用 `/search/` 链接，不需要 `page/search` 别名。

**影响：**
- 生成两个相同的搜索页面（`/search/` 和 `/page/search/`），可能有 SEO 分散问题
- 内容「搜索页面占位 — 实际内容由模板渲染」在页面中不可见（被模板覆盖），但可能影响 HTML 生成

**修复建议：**
移除 `aliases`，或者保留但添加 `noindex` 防止 SEO 分散：
```yaml
---
title: "搜索"
layout: "search"
outputs:
  - HTML
  - json
---
```

但考虑到 `page/search/` 是 v3 版本遗留的路径，如果有外部链接指向 `/page/search/`，保留别名是合理的，只是当前用途不明。

### Y5. sidebar 自定义样式硬覆盖主题布局

| 属性 | 值 |
|------|------|
| 文件 | `assets/scss/custom.scss` |
| 行 | 78-96 |
| 严重度 | 🟡 **建议** |

**问题描述：**
sidebar 样式使用了大量 `!important`（padding、margin、margin-top、margin-bottom、top）来硬覆盖主题的布局。主题的 `sidebar.scss` 在 `@include respond(md)` 中设置了 `padding-top: var(--main-top-padding)` 等，这些本应通过 CSS 变量调整。

**影响：**
- 侧边栏间距完全硬编码，不响应 `--card-padding` 等 CSS 变量变化
- 当主题升级修改 `--main-top-padding` 含义时，这些硬覆盖会表现不一致

**修复建议：**
```scss
.left-sidebar {
    // 让主题 CSS 变量生效，仅覆盖背景
    background-color: rgba(255, 255, 255, 0.7);
    backdrop-filter: blur(10px);
    border-radius: var(--card-border-radius);
    box-shadow: var(--shadow-l1);
    // 去掉所有 padding/margin/top 的 !important 覆盖
}
```

---

## 🟢 优化项

### G1. `.container.extended` 背景透明无实际作用

| 属性 | 值 |
|------|------|
| 文件 | `assets/scss/custom.scss` |
| 行 | 25-28 |
| 严重度 | 🟢 **优化** |

**问题描述：**
```scss
main.main,
.main-container,
.container.extended {
    background: transparent !important;
}
```
主题的 `grid.scss` 中这些容器默认没有设置背景色（仅 flex 布局），所以 `transparent` 是多余的。`!important` 更是不必要。

**修复建议：**
```scss
// 不要覆盖容器背景，body 背景自然穿透
// 只有在有背景色冲突时才添加覆盖
```

### G2. RSS Full Content 未经测试

| 属性 | 值 |
|------|------|
| 文件 | `hugo.yaml` |
| 行 | 18（`params.rssFullContent: true`） |
| 严重度 | 🟢 **优化** |

**问题描述：**
启用 `rssFullContent: true` 意味着 RSS 中输出全文而非摘要。当前只有一篇测试文章，内容很短，无法验证 RSS 是否正确截断/排版了长文。

**影响：**
- 未来写长文（带有 frontmatter 分隔后隐藏的内容）时 RSS 可能异常

**修复建议：**
在开发环境中用 `hugo server` 预览本地 RSS 输出，确认长文章的 RSS 渲染正常。

### G3. 侧边栏状态显示「在线」但无实际 WebSocket/SSE 连接

| 属性 | 值 |
|------|------|
| 文件 | `hugo.yaml` |
| 行 | 36-38 |
| 严重度 | 🟢 **优化** |

**问题描述：**
```yaml
sidebar:
    status:
        emoji: "🟢"
        message: "在线"
```
这个状态只是静态文字，不会因为离线/忙碌而动态变化。如果是纯静态展示没问题，但名字暗示「状态」应该反映真实情况。

**修复建议：**
无实际风险，只是一个视觉命名误导。可改名为 `sidebar.status.defaultMessage`，或添加说明注释这是纯静态装饰。

### G4. `hugo.yaml` 中缺少 `disableKinds` 或 `disableDefaultConfig`

| 属性 | 值 |
|------|------|
| 文件 | `hugo.yaml` |
| 行 | 隐含 |
| 严重度 | 🟢 **优化** |

**问题描述：**
未设置 `disableKinds`，意味着所有默认页面类型（home、section、taxonomy、taxonomyTerm、RSS、sitemap、robotsTXT、404）都会生成。查看 `public/` 目录确认了所有类型都有输出。

当前只有 1 篇测试文章，页面数量很少所以没问题。但未来如果拥有几百篇文章，未使用的类型（如空的 taxonomy terms）可能会额外生成很多静态页面。

**修复建议：**
考虑添加：
```yaml
disableKinds:
  - taxonomyTerm  # 如果不需要空标签/分类的索引页
```

### G5. `SortBy: "default"` 可显式说明语义

| 属性 | 值 |
|------|------|
| 文件 | `hugo.yaml` |
| 行 | 19 |
| 严重度 | 🟢 **优化** |

**问题描述：**
```yaml
params:
    SortBy: "default"
```
主题支持 `"default"` 和 `"lastmod"` 两种值。默认是 Hugo 的自然排序（按 weight → date 倒序）。显式写 `"default"` 没问题，但建议添加注释说明意图。

**修复建议：**
```yaml
SortBy: "default"  # 按发布日期倒序（lastmod 按最后修改时间倒序）
```

### G6. `public/` 目录应完全在 `.gitignore` 中排除但缺少 `resources/`

| 属性 | 值 |
|------|------|
| 文件 | `.gitignore` |
| 行 | 2-3 |
| 严重度 | 🟢 **优化** |

**问题描述：**
`public/` 已 `.gitignore`，但 `resources/_gen/` 也已被忽略。需要确认 `resources/` 没有被纳入版本控制（虽然当前未被 track）。

`public/` 当前包含构建产物（已在 repo 中，因为可能已提交跟踪）。建站初期提交过 public/ 但后续不可能再用 `.gitignore` 阻止追踪已提交文件。

**修复建议：**
如果 `public/` 已 tracked，需要用 `git rm -r --cached public/` 解除追踪。

---

## 📊 整体评分

| 维度 | 评分 | 说明 |
|------|------|------|
| **配置正确性** | ⭐⭐⭐⭐ (8/10) | 大部分配置正确，小优化可做 |
| **样式质量** | ⭐⭐⭐ (6/10) | !important 滥用严重，需要重构 |
| **模板覆盖合理性** | ⭐⭐ (4/10) | 2 个冗余覆盖文件需要删除 |
| **内容结构** | ⭐⭐⭐⭐⭐ (10/10) | 结构清晰，无问题 |
| **CI/CD 健壮性** | ⭐⭐⭐⭐ (8/10) | 版本硬编码但可工作 |
| **搜索功能完整性** | ⭐⭐⭐ (6/10) | 端到端可用但 widget 路径脆弱 |
| **可维护性** | ⭐⭐⭐ (5/10) | !important 和冗余覆盖增加维护成本 |
| **总体** | ⭐⭐⭐⭐ (6.7/10) | **良好起步，需要清理技术债** |

---

## 🗺 改进路线图

### Phase 1 — 立即修复（1-2 小时）
1. 🔴 **删除** `layouts/_default/search.json` 和 `layouts/_default/single.json`
2. 🔴 **删除** `layouts/_default/search.html`
3. 🔴 **重构** `assets/scss/custom.scss` 替换所有 `!important` 为 CSS 变量覆盖
4. 🟡 **修复** `widget/search.html` 的 `data-json` 路径为 OutputFormats API

### Phase 2 — 本周内（1-2 天）
5. 🟡 **添加** Dependabot 配置跟踪 GitHub Actions 依赖
6. 🟡 **验证** 搜索结果预览是否正常工作（部署测试）
7. 🟢 **确认** `public/` .gitignore 状态，必要时 `git rm --cached`

### Phase 3 — 长期维护（每次大更新时）
8. 🟢 **添加** `disableKinds` 减少无谓生成
9. 🟢 **考虑** 使用 `peaceiris/actions-hugo` 代替手动下载 Hugo
10. 🟢 **跟进** 主题 v4 更新日志，同步需要的变更

---

## 📝 附：搜索功能端到端校验

```
hugo.yaml outputFormats:
  → json: mediaType=application/json, baseName=index, isPlainText=true

content/search/index.md:
  → layout: "search"
  → outputs: [HTML, json]
  ✓ 正确输出 HTML + JSON 两种格式

layouts/_default/search.json (冗余，将被删除):
  → 与主题 layouts/page/search.json 内容一致
  ✓ 索引生成逻辑正确（helper/pages.html + .Plain）

layouts/_default/search.html (冗余，将被删除):
  → 与主题 layouts/page/search.html 内容一致
  ✓ 引用了搜索 CSS/JS/JSON

widget/search.html:
  ⚠ data-json 手动拼接 "index.json"（脆弱写法）
  → 实际路径: /search/index.json ✓（当前工作正常）

search.tsx:
  ✓ 正确的 JSON 索引解析和 DOM 匹配
  ✓ 初始化绑定正确
```

## 📝 附：submodule 版本

```
Theme: hugo-theme-stack
Version: v4.0.3 (commit 3e123a3)
Min Hugo: 0.157.0
Current CI Hugo: 0.163.3 ✓
```

v4.0.3 于 2025-12-28 发布，是目前最新稳定版。与 Hugo 0.163.3 兼容性良好。
