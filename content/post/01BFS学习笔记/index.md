---
date: '2023-10-25T10:29:00+08:00'
draft: false
title: '01BFS学习笔记'
tags:
    - 算法
    - 学习笔记
---
本人随机跳题到了一道看起来是最短路的题，但是本人并不想去暴力建图~~懒~~，发现如果抽象成一张图的话边权只有 $0$ 或者 $1$，于是去学了个01BFS。

什么是01BFS呢，其实这是一种最短路的优化，我们都知道BFS要用到queue来维护正确性，但是这在找边权不相等的最短路里就很难受，所以我们需要给他规定一个转移顺序。

我们把queue换成deque，如果这条边边权是1的话就放在队尾，否则放在队首，参考dij里priority_queue的证明，这其实就是找到性质手动模拟了一个优先队列，并且我们的复杂度是优秀的 $O(n)$，以为少了一个堆的log，就很优秀。

当然适用范围有点窄，只能在边权为 $0$ 和一种其他正数中使用。

参考题目：[P1849 [USACO12MAR] Tractor S](https://www.luogu.com.cn/problem/P1849)

```cpp
#include<bits/stdc++.h>
using namespace std;
const int N=1005;
int n,sx,sy,a[N][N],t[N][N];
int stx[6]{-1,0,1,0};
int sty[6]{0,-1,0,1};
deque<pair<int,int>>q;
void BFS(){
    q.push_front(make_pair(sx,sy));
    memset(t,-1,sizeof t);
    t[sx][sy]=0;
    while(q.size()){
        int x=q.front().first;
        int y=q.front().second;
        q.pop_front();
        for(int i=0;i<4;i++){
            int tx,ty;
            tx=x+stx[i];
            ty=y+sty[i];
            if(tx<0||tx>=N) continue;
            if(ty<0||ty>=N) continue;
            if(t[tx][ty]!=-1) continue;
            if(a[tx][ty]){
                t[tx][ty]=t[x][y]+1;
                q.push_back(make_pair(tx,ty));
            }else{
                t[tx][ty]=t[x][y];
                q.push_front(make_pair(tx,ty));
            }
        }
    }
}

int main(){
    scanf("%d%d%d",&n,&sx,&sy);
    for(int i=1;i<=n;i++){
        int s1,s2;
        scanf("%d%d",&s1,&s2);
        a[s1][s2]=1;
    }
    BFS();
    printf("%d",t[0][0]);
    return 0;
}
```
