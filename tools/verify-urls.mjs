#!/usr/bin/env node
/**
 * verify-urls.mjs — 校验「迁移后旧链接不失效」。
 *
 * 做三件事：
 *   1. 逐条检查旧站 URL 基线（Hugo 版重建 sitemap 导出的 46 条，存在 tools/legacy-urls.txt）
 *      在构建产物里是否有对应页面；
 *   2. 检查文章正文里引用的站内绝对路径（/images/…、/code/…、/post/…）是否真实存在；
 *   3. 报告新站多出来的 URL，并对含字面 "+" / 空格的 URL 告警
 *      （这类字符依赖托管平台的路径解析行为，GitHub Pages 按普通字符处理，
 *        但换个平台就可能翻车，所以显式提示）。
 *
 * 用法：
 *   node tools/verify-urls.mjs                                   # 默认校验 dist/ 对 tools/legacy-urls.txt
 *   node tools/verify-urls.mjs --public dist --baseline tools/legacy-urls.txt
 *
 * 退出码：有缺失链接时为 1（可直接卡 CI）。
 *
 * ⚠️ 修订记录（2026-09-23，来自 docs/blog-migration-audit.md 的严重项 1）：
 *   初版把文章目录写成了 Hexo 的 source/_posts，该目录在 Astro 站点里不存在，
 *   于是 checkStaticRefs() 每次都在 existsSync 处提前 return 空数组——
 *   「文章内引用的站内资源全部存在」那一行是空转刷出来的 ✅，不是真结论。
 *   现改为 src/content/posts；产物目录默认值也从 public/ 改成 dist/。
 */

import { readFileSync, readdirSync, existsSync, statSync } from 'node:fs';
import { join, dirname, resolve, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(HERE, '..');

/** 旧站 URL 基线，随仓库一起版本控制 */
const DEFAULT_BASELINE = join(HERE, 'legacy-urls.txt');
/** 文章正文所在目录（Astro content collection） */
const POSTS_DIR = join(ROOT, 'src', 'content', 'posts');
/** 构建产物目录（Astro 输出到 dist/） */
const DEFAULT_PUBLIC = 'dist';

function argValue(flag, fallback) {
  const i = process.argv.indexOf(flag);
  return i !== -1 && process.argv[i + 1] ? process.argv[i + 1] : fallback;
}

const BASELINE = resolve(argValue('--baseline', DEFAULT_BASELINE));
const PUBLIC_DIR = resolve(ROOT, argValue('--public', DEFAULT_PUBLIC));

/** 递归列出产物目录下所有文件，返回相对路径（正斜杠）。 */
function walk(dir, acc = []) {
  for (const name of readdirSync(dir)) {
    const full = join(dir, name);
    if (statSync(full).isDirectory()) walk(full, acc);
    else acc.push(relative(PUBLIC_DIR, full).split(sep).join('/'));
  }
  return acc;
}

/** 把产物文件路径换算成可访问的 URL 路径集合。 */
function urlsFromFiles(files) {
  const urls = new Set();
  for (const f of files) {
    urls.add('/' + f);
    if (f.endsWith('/index.html')) urls.add('/' + f.slice(0, -'index.html'.length));
    else if (f === 'index.html') urls.add('/');
  }
  return urls;
}

/** 旧站 URL -> 可访问路径（URL 解码后比对，规避编码差异）。 */
function toPath(url) {
  return decodeURIComponent(new URL(url).pathname);
}

/** 扫描文章正文里的站内绝对引用，确认产物里真有对应文件。 */
function checkStaticRefs() {
  const problems = [];
  if (!existsSync(POSTS_DIR)) {
    // 这里绝不能静默跳过：目录不对就意味着整套检查是空转的
    problems.push(`[配置错误] 文章目录不存在：${POSTS_DIR}，站内资源检查无法进行`);
    return problems;
  }
  let checked = 0;
  for (const name of readdirSync(POSTS_DIR)) {
    if (!name.endsWith('.md') && !name.endsWith('.mdx')) continue;
    const body = readFileSync(join(POSTS_DIR, name), 'utf8');
    const refs = [
      ...body.matchAll(/!?\[[^\]]*\]\((\/[^)\s]+)\)/g),
      ...body.matchAll(/(?:src|href)="(\/[^"]+)"/g)
    ].map((m) => m[1]);
    for (const ref of refs) {
      const clean = ref.split('#')[0].split('?')[0];
      if (!clean || clean === '/') continue;
      checked++;
      const target = join(PUBLIC_DIR, decodeURIComponent(clean));
      if (!existsSync(target)) problems.push(`${name} -> ${clean}`);
    }
  }
  if (checked === 0) problems.push('[配置错误] 一篇文章的站内引用都没扫到，检查逻辑可能失效');
  return problems;
}

function main() {
  if (!existsSync(PUBLIC_DIR)) {
    console.error(`产物目录不存在：${PUBLIC_DIR}\n先构建：pnpm exec astro build（或完整的 pnpm build）`);
    process.exit(1);
  }
  if (!existsSync(BASELINE)) {
    console.error(`基线文件不存在：${BASELINE}`);
    process.exit(1);
  }

  const baselineUrls = readFileSync(BASELINE, 'utf8').split(/\r?\n/).map((s) => s.trim()).filter(Boolean);
  const available = urlsFromFiles(walk(PUBLIC_DIR));

  const missing = [];
  for (const url of baselineUrls) {
    const path = toPath(url);
    if (!available.has(path)) missing.push({ url, path });
  }

  const baselinePaths = new Set(baselineUrls.map(toPath));
  const extra = [...available]
    .filter((p) => p.endsWith('/'))
    .filter((p) => !p.includes('/page/') && !p.startsWith('/_astro/') && !p.startsWith('/pagefind/'))
    .filter((p) => !baselinePaths.has(p))
    .sort();

  const staticProblems = checkStaticRefs();

  // 依赖平台路径解析行为的字符：出问题时不报错、只是悄悄 404，所以显式告警
  const risky = baselineUrls.filter((u) => {
    const p = toPath(u);
    return p.includes('+') || p.includes(' ');
  });

  console.log(`基线 URL：${baselineUrls.length} 条   产物 URL：${available.size} 条\n`);

  if (missing.length) {
    console.log(`❌ 失效链接 ${missing.length} 条：`);
    for (const m of missing) console.log(`   ${m.path}   (${m.url})`);
  } else {
    console.log('✅ 旧站 URL 全部可用，无失效。');
  }

  if (staticProblems.length) {
    console.log(`\n❌ 文章站内引用问题 ${staticProblems.length} 处：`);
    for (const p of staticProblems) console.log('   ' + p);
  } else {
    console.log('✅ 文章内引用的站内资源全部存在。');
  }

  if (risky.length) {
    console.log(`\n⚠️  含字面 "+"/空格的旧 URL ${risky.length} 条（依赖平台解析行为，换托管方需回归）：`);
    for (const u of risky) console.log('   ' + toPath(u));
  }

  if (extra.length) {
    console.log(`\nℹ️  新站新增 URL ${extra.length} 条（预期内无害）：`);
    for (const e of extra) console.log('   ' + e);
  }

  process.exit(missing.length || staticProblems.length ? 1 : 0);
}

main();
