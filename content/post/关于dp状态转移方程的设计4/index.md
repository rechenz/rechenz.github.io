---
date: '2026-07-18T16:50:28+08:00'
draft: false
title: '关于dp状态转移方程的设计4'
tags:
    - 算法
---

我们有的时候会发现一个问题，题目给的数据范围或者是某些引导让我们的dp状态设计非常显然，但是可能转移的时候会遇到难以维护或者复杂度不对的情况。

那么我们需要考虑优化，在算法中一种常见的优化是预处理，在某些时候我们可以通过将难以放在方程里维护的数据或者难以找到维护顺序的参数进行预处理，但是记住我们需要去找到使我们这个预处理不会跟随dp方程而改变的性质。

[CF1106E](https://codeforces.com/problemset/problem/1106/E)

```cpp
#include<bits/stdc++.h>
using namespace std;
#define int long long
const int N = 100005;
const int M = 205;
int n, m, k, dp[N][M], t[N], s[N];

struct Node {
    int s;
    int t;
    int d;
    int w;
    bool operator <(const Node& other)const {
        if (w != other.w) {
            return w < other.w;
        }
        else return d < other.d;
    }
}a[N];

bool cmp(Node A, Node B) {
    if (A.s != B.s) {
        return A.s < B.s;
    }
    else {
        return A.t < B.t;
    }
}

priority_queue<Node>q;

signed main() {
    cin >> n >> m >> k;
    for (int i = 1;i <= k;i++) {
        cin >> a[i].s >> a[i].t >> a[i].d >> a[i].w;
    }
    sort(a + 1, a + k + 1, cmp);
    int cur = 1;
    t[0] = 1;
    for (int i = 1;i <= n;i++) {
        while (cur <= k && a[cur].s <= i) {
            q.push(a[cur++]);
        }
        if (q.empty()) t[i] = i + 1;
        else {
            Node x = q.top();
            if (x.t < i) {
                i--;
                q.pop();
                continue;
            }
            t[i] = x.d + 1;
            s[i] = x.w;
        }
    }
    memset(dp, 0x3f, sizeof dp);
    dp[0][0] = 0;
    for (int i = 0;i <= n;i++) {
        for (int j = 0;j <= m;j++) {
            dp[i + 1][j + 1] = min(dp[i + 1][j + 1], dp[i][j]);
            dp[t[i]][j] = min(dp[t[i]][j], dp[i][j] + s[i]);
        }
    }
    int ans = LONG_LONG_MAX;
    for (int i = 0;i <= m;i++) {
        ans = min(ans, dp[n + 1][i]);
    }
    cout << ans << endl;
    return 0;
}
```
