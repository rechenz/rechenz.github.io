#!/usr/bin/env node
/**
 * migrate-to-firefly.mjs — 把本博客从 Hugo（page bundle）迁到 Firefly（Astro）。
 *
 * 映射规则：
 *   content/post/<目录名>/index.md   ->  src/content/posts/<slug>.md
 *   content/post/<目录名>/<图片等>   ->  public/images/<slug>/<文件名>
 *   文章内相对图片引用               ->  /images/<slug>/<文件名>
 *      （放 public/ 下，产物路径与旧站逐字一致，不经过 Astro 图片优化管道）
 *
 * slug 规则（必须与旧站逐字对齐，否则旧链接 404）：
 *   Hugo 默认 disablePathToLower=false，会把路径中的 ASCII 字母转小写、非 ASCII 原样保留。
 *   例：01BFS学习笔记 -> 01bfs学习笔记；TheDemiuge项目UE5客户端框架设计 -> thedemiuge项目ue5客户端框架设计
 *   已用重建后的 sitemap.xml（46 条 URL）逐条核对。
 *
 * Front matter 映射（Hugo -> Firefly/Astro）：
 *   date    -> published
 *   tags    -> tags（行内数组）
 *   tags[0] -> category（见 CATEGORY_RULES）
 *   title   -> title
 *   draft   -> draft
 *
 * 用法：
 *   node tools/migrate-to-firefly.mjs --dry-run
 *   node tools/migrate-to-firefly.mjs
 *
 * 幂等：可重复运行，会覆盖目标文件。
 */

import { readFileSync, writeFileSync, mkdirSync, readdirSync, existsSync, copyFileSync, statSync } from 'node:fs';
import { join, dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(HERE, '..');
const SRC_POSTS = join(ROOT, 'content', 'post');
const OUT_POSTS = join(ROOT, 'src', 'content', 'posts');
const OUT_IMAGES = join(ROOT, 'public', 'images');

const DRY_RUN = process.argv.includes('--dry-run');

/** 与 Hugo 的路径小写化行为对齐：ASCII 转小写，其余原样。 */
function toHugoSlug(name) {
  return name.replace(/[A-Z]/g, (c) => c.toLowerCase());
}

/** 无法从 tags 推断的分类，显式指定。 */
const CATEGORY_OVERRIDES = {
  UE设计哲学: '游戏开发'
};

/** tags -> category 的映射（按 tags 出现顺序取第一个命中）。 */
const CATEGORY_RULES = [
  { tag: '算法', category: '算法竞赛' },
  { tag: '题解', category: '算法竞赛' },
  { tag: '游戏开发', category: '游戏开发' },
  { tag: 'AI', category: '人工智能' },
  { tag: '碎碎念', category: '随笔' }
];

const FALLBACK_CATEGORY = '随笔';

/** 解析 Hugo 的 YAML front matter（本仓库只用到标量与字符串列表，无需完整 YAML 解析器）。 */
function parseFrontMatter(raw) {
  const lines = raw.split(/\r?\n/);
  if (lines[0].trim() !== '---') throw new Error('缺少 front matter 起始分隔符 ---');
  let end = -1;
  for (let i = 1; i < lines.length; i++) {
    if (lines[i].trim() === '---') {
      end = i;
      break;
    }
  }
  if (end === -1) throw new Error('front matter 未闭合');

  const fields = {};
  let currentKey = null;
  for (let i = 1; i < end; i++) {
    const line = lines[i];
    if (!line.trim()) continue;
    const listItem = line.match(/^\s+-\s+(.*)$/);
    if (listItem && currentKey) {
      if (!Array.isArray(fields[currentKey])) fields[currentKey] = [];
      fields[currentKey].push(unquote(listItem[1]));
      continue;
    }
    const kv = line.match(/^([A-Za-z_][\w-]*):\s*(.*)$/);
    if (kv) {
      currentKey = kv[1];
      const value = kv[2].trim();
      fields[currentKey] = value === '' ? [] : unquote(value);
    }
  }
  return { fields, body: lines.slice(end + 1).join('\n') };
}

function unquote(value) {
  const v = value.trim();
  if ((v.startsWith("'") && v.endsWith("'")) || (v.startsWith('"') && v.endsWith('"'))) {
    return v.slice(1, -1);
  }
  return v;
}

/**
 * '2023-10-25T10:29:00+08:00' -> '2023-10-25T10:29:00+08:00'（保留原始时区偏移）
 * js-yaml 会把带偏移的 ISO 串解析成精确时间点，避免构建机时区（CI 是 UTC）导致的日期漂移。
 */
function toAstroDate(iso) {
  const m = String(iso).match(/^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})(?::(\d{2}))?(?:\.\d+)?(Z|[+-]\d{2}:?\d{2})?$/);
  if (!m) throw new Error(`无法解析日期: ${iso}`);
  const [, y, mo, d, h, mi, s = '00', tz] = m;
  return `${y}-${mo}-${d}T${h}:${mi}:${s}${tz ?? '+08:00'}`;
}

function pickCategory(dirName, tags) {
  if (CATEGORY_OVERRIDES[dirName]) return CATEGORY_OVERRIDES[dirName];
  for (const { tag, category } of CATEGORY_RULES) {
    if (tags.includes(tag)) return category;
  }
  return FALLBACK_CATEGORY;
}

/** YAML 标量安全化：含特殊字符时加单引号。 */
function yamlScalar(value) {
  const v = String(value);
  if (v === '' || /[:#\-?&*!|>%@`{}[\],"']/.test(v) || /^\s|\s$/.test(v)) {
    return `'${v.replace(/'/g, "''")}'`;
  }
  return v;
}

/** URL 段编码：保留中文等可读字符，只转义会破坏 Markdown 链接的字符。 */
function encodeSegment(s) {
  return s.replace(/[ %?#]/g, (c) => `%${c.charCodeAt(0).toString(16).toUpperCase()}`);
}

/** Hexo/Astro 的摘要分隔符统一为 <!-- more -->；Hugo 侧常写作 <!--more-->。 */
function normalizeBody(body) {
  return body.replace(/<!--\s*more\s*-->/g, '<!-- more -->');
}

/** 把 page bundle 内的相对图片引用改写成 /images/<slug>/<file> 绝对路径。 */
function rewriteImageRefs(body, slug, assetNames, warnings) {
  return body.replace(/(!\[[^\]]*\]\()([^)\s]+)(\s+"[^"]*")?(\))/g, (full, prefix, target, title = '', suffix) => {
    if (/^(https?:)?\/\//.test(target) || target.startsWith('/')) return full;
    const clean = decodeURIComponent(target).replace(/^\.\//, '');
    if (!assetNames.has(clean)) {
      warnings.push(`引用了 bundle 内不存在的资源: ${target}`);
      return full;
    }
    return `${prefix}/images/${encodeSegment(slug)}/${encodeSegment(clean)}${title}${suffix}`;
  });
}

function main() {
  if (!existsSync(SRC_POSTS)) {
    console.error(`找不到源目录: ${SRC_POSTS}`);
    process.exit(1);
  }
  if (!DRY_RUN) {
    mkdirSync(OUT_POSTS, { recursive: true });
    mkdirSync(OUT_IMAGES, { recursive: true });
  }

  const dirs = readdirSync(SRC_POSTS).filter((name) => statSync(join(SRC_POSTS, name)).isDirectory());
  const report = [];
  const warnings = [];

  for (const dirName of dirs) {
    const bundleDir = join(SRC_POSTS, dirName);
    const entry = join(bundleDir, 'index.md');
    if (!existsSync(entry)) {
      warnings.push(`跳过（无 index.md）: ${dirName}`);
      continue;
    }

    const { fields, body } = parseFrontMatter(readFileSync(entry, 'utf8'));
    const slug = toHugoSlug(dirName);
    const tags = Array.isArray(fields.tags) ? fields.tags : fields.tags ? [fields.tags] : [];
    const category = pickCategory(dirName, tags);

    const assets = readdirSync(bundleDir).filter((f) => f !== 'index.md' && statSync(join(bundleDir, f)).isFile());
    const newBody = normalizeBody(rewriteImageRefs(body, slug, new Set(assets), warnings));

    const fmLines = ['---'];
    fmLines.push(`title: ${yamlScalar(fields.title ?? dirName)}`);
    fmLines.push(`published: ${toAstroDate(fields.date)}`);
    if (tags.length) fmLines.push(`tags: [${tags.map(yamlScalar).join(', ')}]`);
    fmLines.push(`category: ${yamlScalar(category)}`);
    if (fields.draft === 'true') fmLines.push('draft: true');
    fmLines.push('---');
    const out = `${fmLines.join('\n')}\n\n${newBody.replace(/^\n+/, '')}`;

    if (!DRY_RUN) {
      writeFileSync(join(OUT_POSTS, `${slug}.md`), out, 'utf8');
      for (const asset of assets) {
        const destDir = join(OUT_IMAGES, slug);
        mkdirSync(destDir, { recursive: true });
        copyFileSync(join(bundleDir, asset), join(destDir, asset));
      }
    }

    report.push({ dir: dirName, slug, category, tags: tags.join(','), assets: assets.length });
  }

  report.sort((a, b) => a.slug.localeCompare(b.slug, 'en'));
  console.log(`${DRY_RUN ? '[dry-run] ' : ''}迁移 ${report.length} 篇文章 -> Firefly(Astro)\n`);
  for (const r of report) {
    console.log(`${r.slug.padEnd(34)}${r.category.padEnd(12)}${String(r.assets).padEnd(4)} ${r.tags}`);
  }
  if (warnings.length) {
    console.log('\n警告:');
    for (const w of warnings) console.log('  - ' + w);
  }
  console.log(`\n文章: ${OUT_POSTS}`);
  console.log(`图片: ${OUT_IMAGES}`);
}

main();
