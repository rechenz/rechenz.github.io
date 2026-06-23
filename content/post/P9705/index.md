---
date: '2023-10-16T16:40:00+08:00'
draft: true
title: 'P9705 「TFOI R1」Unknown Graph 题解'
tags:
    -算法
    -题解
---

一道有趣简单的构造题。

阅读题面，发现这似乎很难找到完美的贪心策略，那么我们考虑网络流。

先说建模方式：

考虑拆点，把每个点拆成编号为 $i$ 和 $n+i$ 的两个点。

首先从超级源点 $s$ 向编号为 $1 \sim n$ 的每一个点连流量为这个点出度的边。

然后再从 $1 \sim n$ 的点向每一个编号为 $n+1$ 到 $2n$ 流量为 $1$ 的边，当然因为题中给出了 $m$ 条限制，那么不对这些限制的点进行连边就好了。

最后从编号为 $n+1$ 到 $2n$ 的点向超级汇点 $t$ 连一条流量为这个点入度的边就好了。

大概建了个这么个图：

![11](https://cdn.luogu.com.cn/upload/image_hosting/v1e0uaot.png)

那么边数就是这个网络的最大流（因为保证有解），至于怎么输出构造的边，我们去遍历这个网络的残余流量，如果第二步建的正向边的流量为 $0$ 那么就说明这条边被我们选择了，我们输出这条边的两个节点就好。

然后因为这是个二分图，那么我们的复杂度就是优秀的 $O(n\sqrt n)$。

这道题其实就是直接对着题意进行网络流模拟，是一道很好的板子题。

如果还没理解就看看代码，自认为很好看（

```cpp
#include<bits/stdc++.h>
using namespace std;
const int N=1005;
const int inf=1e9+7;
int n,m,in[N],out[N],head[2*N],cnt=1,s,t,mp[N][N],num=1;
struct Edge{
    int from;
    int to;
    int next;
    int wide;
}e[2*N*N+2*N];

void adding(int u,int v,int w){
    e[++cnt].to=v;
    e[cnt].from=u;
    e[cnt].wide=w;
    e[cnt].next=head[u];
    head[u]=cnt;
}

int dis[2*N],now[2*N];

bool BFS(){
    for(int i=1;i<=2*n+1;i++){
        dis[i]=inf;
    }
    dis[s]=0;
    queue<int>q;
    q.push(s);
    now[s]=head[s];
    while(q.size()){
        int x=q.front();
        q.pop();
        for(int i=head[x];i;i=e[i].next){
            int v=e[i].to;
            // cout<<x<<" "<<v<<endl;
            if(dis[v]==inf&&e[i].wide){
                dis[v]=dis[x]+1;
                now[v]=head[v];
                q.push(v);
            }
        }
    }
    return dis[t]!=inf;
}

int DFS(int x,int sum){
    if(x==t) return sum;
    int k,res=0;
    for(int i=now[x];i&&sum;i=e[i].next){
        int v=e[i].to;
        now[x]=i;
        if(dis[v]==dis[x]+1&&e[i].wide){
            k=DFS(v,min(e[i].wide,sum));
            if(k==0){
                dis[v]=inf;
            }
            e[i].wide-=k;
            e[i^1].wide+=k;
            sum-=k;
            res+=k;
        }
    }
    return res;
}

int Dinic(){
    int res=0;
    while(BFS()){
        res+=DFS(s,inf);
    }
    return res;
}

void Debug(){
    for(int x=0;x<=2*n+1;x++){
        for(int i=head[x];i;i=e[i].next){
            int v=e[i].to;
            cout<<x<<" "<<v<<" "<<e[i].wide<<endl;
        }
    }
}

int main(){
    scanf("%d",&n);
    for(int i=1;i<=n;i++){
        scanf("%d",&in[i]);
    }
    for(int i=1;i<=n;i++){
        scanf("%d",&out[i]);
    }
    scanf("%d",&m);
    for(int i=1;i<=m;i++){
        int s1,s2;
        scanf("%d%d",&s1,&s2);
        mp[s1][s2]=1;
    }
    s=0,t=2*n+1;
    for(int i=1;i<=n;i++){
        for(int j=1;j<=n;j++){
            if(i==j) continue;
            if(mp[i][j]) continue;
            adding(i,j+n,1);
            adding(j+n,i,0);
            num+=2;
        }
    }
    for(int i=1;i<=n;i++){
        adding(s,i,out[i]);
        adding(i,s,0);
    }
    for(int i=n+1;i<=2*n;i++){
        adding(i,t,in[i-n]);
        adding(t,i,0);
    }
    Debug();
    printf("%d\n",Dinic());
    for(int i=2;i<=num;i+=2){
        if(!e[i].wide){
            printf("%d %d\n",e[i].from,e[i].to-n);
        }
    }
    return 0;
}
```
