---
date: '2023-10-31T21:39:00+08:00'
draft: false
title: 'P6413 [COCI2008-2009#3] NAJKRACI 题解'
tags:
    - 算法
    - 题解
---
之前没有接触过最短路图，退役前正好趁这道题学习一下。

其实最短路图没有多少东西，但是考到的时候就很有用 ~~废话~~。

首先对于一张图来说，以一个点来跑单源最短路的话，那么这个起点到所有点的最短路必定构成一张 $\texttt{DAG}$，那么这就是很优秀的，但是如果我们更极端一点，用奇怪的方式规定（当然也可能直接说）两点间的最短路有且只有一条，那么我们跑出来的就变成了一颗最短路树。

来看这道题，题面非常简介，发现这道题中的边如果不被任何最短路经过的话便是毫无作用的，所以我们跑出最短路图，然后便是用拓扑排序在 $\texttt{DAG}$ 上进行递推计数。

因为题中的数据范围很小，所以我们暴力地枚举起点，以枚举的起点跑单源最短路，然后对跑出来的 $\texttt{DAG}$ 进行答案累加就可以了。

但是要注意一点，我们的 $\texttt{DAG}$ 要进行反拓扑序，因为我们记录的是以某一点为起点的 $\texttt{DAG}$，不反过来跑的话记的数是错的。

然后求哪些边是最短路经过的边的时候其实用上了dp回溯（因为最短路算法本质就是动态规划嘛），我们只需要枚举边，然后判断是否 $dis[v]=dis[x]+e[i].wide$ 就可以了。

然后对于跑最短路因为没有负权边所以我用的是堆优化 $\mathrm{Dijkstra}$。

复杂度分析：$\mathrm{O}(n^2logn+n^2)$ 理论实测均可过。

（因为我也是才学所以代码有所借鉴其他题解）

```cpp
#include<bits/stdc++.h>
using namespace std;
#define int long long
const int N=1505;
const int mod=1e9+7;
int n,m,head[N],cnt;

struct Edge{
    int to;
    int next;
    int wide;
}e[5005];

void adding(int u,int v,int w){
    e[++cnt].to=v;
    e[cnt].wide=w;
    e[cnt].next=head[u];
    head[u]=cnt;
}

int cnt1[N],cnt2[N],dis[N],vis[N],ans[5005];
priority_queue<pair<int,int>,vector<pair<int,int>>,greater<pair<int,int>>>q;
void dij(int s){
    memset(cnt1,0,sizeof cnt1);
    memset(dis,127,sizeof dis);
    memset(vis,0,sizeof vis);
    dis[s]=0;
    cnt1[s]=1;
    q.push(make_pair(dis[s],s));
    vector<int>dot;
    while(q.size()){
        int x=q.top().second;
        q.pop();
        if(vis[x]) continue;
        vis[x]=1;
        dot.push_back(x);
        for(int i=head[x];i;i=e[i].next){
            int v=e[i].to;
            if(dis[v]>dis[x]+e[i].wide){
                dis[v]=dis[x]+e[i].wide;
                cnt1[v]=cnt1[x];
                q.push(make_pair(dis[v],v));
            }else if(dis[x]+e[i].wide==dis[v]){
                cnt1[v]+=cnt1[x];
            }
        }
    }
    reverse(dot.begin(),dot.end());
    for(auto x:dot){
        cnt2[x]=1;
        for(int i=head[x];i;i=e[i].next){
            int v=e[i].to;
            if(dis[v]==dis[x]+e[i].wide){
                cnt2[x]=(cnt2[x]+cnt2[v])%mod;
                ans[i]+=1ll*cnt1[x]%mod*cnt2[v]%mod;
                ans[i]%=mod;
            }
        }
    }
}

signed main(){
    scanf("%lld%lld",&n,&m);
    for(int i=1;i<=m;i++){
        int s1,s2,s3;
        scanf("%lld%lld%lld",&s1,&s2,&s3);
        adding(s1,s2,s3);
    }
    for(int i=1;i<=n;i++){
        dij(i);
    }
    for(int i=1;i<=m;i++){
        printf("%lld\n",ans[i]);
    }
    return 0;
}
```
