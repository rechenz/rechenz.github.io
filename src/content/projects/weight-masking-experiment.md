---
title: weight-masking-experiment — 神经网络训练动态研究
published: 2026-07-22
order: 3
description: 模拟「机能不全 → 健全」的训练过程：前期约束网络、后期逐渐放开，类比大脑发育。CIFAR-10 上 Fixed Mask 达到 82.69%，反超 Baseline 1.65%。
tags: [Python, PyTorch, TinyViT, 实验]
status: published
link:
  - label: GitHub
    icon: fa7-brands:github
    value: https://github.com/rechenz/weight-masking-experiment
  - label: 实验全记录
    icon: material-symbols:article
    value: /post/权重约束训练实验/
---

## 想法从哪来

人类婴儿的大脑并不是一上来就全功能可用的，很多能力是随着发育逐步「解锁」的。那如果反过来——**训练前期刻意让网络机能不全，后期逐渐放开**，会不会得到更好的结果？

这个实验就是在模拟这个过程：给网络施加约束（激活层 / 权重层），然后按计划逐步解除。

## 实验设计

- 框架：TinyViT + PyTorch（AMP 混合精度）
- 数据集：FashionMNIST（先验证）→ CIFAR-10（主战场）
- 对比组：激活层约束（dropout / noise）、随机 mask、**固定 mask**、Baseline

## 结论

| 方案 | FashionMNIST | CIFAR-10 |
|------|--------------|----------|
| Baseline | 88.6% | 81.04% |
| 激活层约束 | ≈ Baseline | ≈ Baseline |
| **Fixed Mask** | 87.79% | **82.69%（+1.65%）** |

几个值得记的观察：

- **Transformer 对激活层扰动极其鲁棒**——dropout / noise 这类约束基本等于没做
- **固定 mask 优于随机 mask**：「固定的机能不全」比「每次随机重新受伤」更有效
- **数据集越难，效果越明显**：CIFAR-10 上直接反超了 baseline，猜测可能更适合具身智能这类复杂场景
- SVD 截断仍然是硬伤（数值崩塌），需要重新设计（warmup / 分层 / 慢 rank 增长）

> 早期一版结论说「权重层面操作太暴力」——那是 bug 导致的误判，修完反而反超了，已在仓库里更正。
