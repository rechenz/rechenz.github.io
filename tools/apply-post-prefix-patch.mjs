#!/usr/bin/env node
/**
 * apply-post-prefix-patch.mjs — 把 Firefly 的文章路由前缀从 /posts/ 改成 /post/。
 *
 * 为什么需要：本博客迁移前的 Hugo 站点文章地址是 /post/<slug>/，
 * 外部链接、搜索引擎收录、文章内的互相引用都指向这个前缀。
 * Firefly 默认用 /posts/<slug>/，差一个字母就会让所有旧链接 404。
 *
 * 补丁内容（两类）：
 *   1. 目录重命名：src/pages/posts/ -> src/pages/post/（路由前缀由目录名决定）
 *   2. 逐处替换硬编码的 URL 前缀（只匹配 URL 字面量，不动文件系统路径：
 *      remark-wiki-link.js 里的 "../content/posts/" 必须保持原样）
 *
 * 幂等：已经打过补丁时报告「无需修改」，可安全重复运行。
 * 用途：主题升级（git merge upstream）后重放本脚本即可恢复 /post/ 前缀。
 *
 * 用法：
 *   node tools/apply-post-prefix-patch.mjs --check   # 只检查当前状态
 *   node tools/apply-post-prefix-patch.mjs
 */

import { readFileSync, writeFileSync, existsSync, renameSync, readdirSync, statSync } from 'node:fs';
import { join, dirname, resolve, extname } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(HERE, '..');
const SRC = join(ROOT, 'src');

const CHECK_ONLY = process.argv.includes('--check');

/** URL 字面量模式：/posts/ -> /post/。刻意不匹配 "content/posts/" 这类文件系统路径。 */
const PATTERNS = [
  { from: /\/posts\/\$\{/g, to: '/post/${', label: 'template literal `/posts/${…}`' },
  { from: /"\/posts\/"/g, to: '"/post/"', label: 'string "/posts/"' },
  { from: /'\/posts\/'/g, to: "'/post/'", label: "string '/posts/'" },
  { from: /`\/posts\/`/g, to: '`/post/`', label: 'template literal `/posts/`' }
];

const SCAN_EXT = new Set(['.ts', '.tsx', '.astro', '.svelte', '.js', '.mjs']);

function walk(dir, acc = []) {
  for (const name of readdirSync(dir)) {
    const full = join(dir, name);
    if (statSync(full).isDirectory()) walk(full, acc);
    else if (SCAN_EXT.has(extname(full))) acc.push(full);
  }
  return acc;
}

function main() {
  const changes = [];

  // 1) 目录重命名
  const legacyRoutes = join(SRC, 'pages', 'posts');
  const newRoutes = join(SRC, 'pages', 'post');
  if (existsSync(legacyRoutes)) {
    if (CHECK_ONLY) {
      changes.push('DIR   src/pages/posts/ -> src/pages/post/');
    } else {
      if (existsSync(newRoutes)) {
        console.error('同时存在 src/pages/posts/ 与 src/pages/post/，请先人工确认后再跑本脚本。');
        process.exit(1);
      }
      renameSync(legacyRoutes, newRoutes);
      changes.push('DIR   src/pages/posts/ -> src/pages/post/');
    }
  }

  // 2) 逐文件替换 URL 字面量
  for (const file of walk(SRC)) {
    const original = readFileSync(file, 'utf8');
    let text = original;
    const hits = [];
    for (const { from, to, label } of PATTERNS) {
      const matched = text.match(from);
      if (matched) {
        hits.push(`${label} ×${matched.length}`);
        text = text.replace(from, to);
      }
    }
    if (text === original) continue;
    const rel = file.replace(ROOT + '\\', '').replace(ROOT + '/', '');
    changes.push(`FILE  ${rel}  (${hits.join(', ')})`);
    if (!CHECK_ONLY) writeFileSync(file, text, 'utf8');
  }

  if (!changes.length) {
    console.log('✅ 无需修改：文章路由前缀已是 /post/。');
    return;
  }
  console.log(`${CHECK_ONLY ? '[check] 待修改' : '已完成'} ${changes.length} 项：`);
  for (const c of changes) console.log('  ' + c);
  if (CHECK_ONLY) console.log('\n去掉 --check 即实际执行。');
}

main();
