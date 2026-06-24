---
date: '2023-06-17T15:39:00+08:00'
draft: false
title: 'P4209 学习小组 题解'
tags:
    - 算法
    - 题解
---
# $\mathrm{tag}$：网络流，费用流

---

我第一眼看到这道题的平方建边的时候我还以为需要一些数学方面的优化，但是同机房大佬瞅了一眼跟我说暴力建边能过，我心里一群草泥马奔腾而过。。。

这题的基本建模其实就是按照题意模拟：

$1.$ 从超级源点向每一位同学连一条流量为 $\mathrm{k}$，费用为 $0$ 的边。

$2.$ 从每个同学向愿意参加的社团连一条容量为 $1$，费用为手续费的相反数，因为交的手续费算收益（~~赚翻了~~）。

$3.$ 最后便是刚才说的暴力建边，根据公式从每个社团向汇点连 $n$ 条边，每一条边的容量为 $1$,费用为——$C_i \times (2 \times Num-1)$，$Num$ 表示第几条边。

但是我们注意到题目要求的最大流量是参加的人数尽量多而不是学习小组的人数之和，那么我们这里用一个贪心的思想，再从每个同学向汇点连一条容量为 $k-1$，费用为 $0$ 的边，因为我们只要保证这个同学参加了任意一个学习小组就够了。

如果财务处能赚到钱就让学生去参加，赚不到直接让这个学生走费用为 $0$ 的边就好了（~~真黑啊~~）。

复杂度分析：Dinic 复杂度，是肯定能过的啦。

```cpp
#include<bits/stdc++.h>
using namespace std;
const int inf=1e9+7;
const int N=10005;
int n,m,sum,a[N],b[N],head[N],cnt=1,s,t,ans;

struct Edge{
    int to;
    int next;
    int wide;
    int val;
}e[N*N];

void adding(int u,int v,int w,int val){
    e[++cnt].to=v;
    e[cnt].val=val;
    e[cnt].wide=w;
    e[cnt].next=head[u];
    head[u]=cnt;
}
int dis[N],vis[N];
int now[N];
bool SPFA(){
    for(int i=1;i<=t;i++){
        dis[i]=inf;
    }
    dis[s]=0;
    queue<int>q;
    q.push(s);
    vis[s]=1;
    now[s]=head[s];
    while(q.size()){
        int x=q.front();
        q.pop();
        vis[x]=0;
        for(int i=head[x];i;i=e[i].next){
            int v=e[i].to;
            if(e[i].wide&&dis[v]>dis[x]+e[i].val){
                now[v]=head[v];
                dis[v]=dis[x]+e[i].val;
                if(!vis[v]){
                    q.push(v);
                    vis[v]=1;
                }
            }
        }
    }
    return dis[t]!=inf;
}   

int DFS(int x,int sum){
    if(x==t) return ans+=dis[t]*sum,sum;
    int k,res=0;
    vis[x]=1;
    for(int i=now[x];i&∑i=e[i].next){
        int v=e[i].to;
        now[x]=i;
        if(e[i].wide&&(dis[v]==dis[x]+e[i].val)&&!vis[v]){
            k=DFS(v,min(sum,e[i].wide));
            if(k==0){
                dis[v]=inf;
            }
            e[i].wide-=k;
            e[i^1].wide+=k;
            sum-=k;
            res+=k;
        }
    }
    vis[x]=0;
    return res;
}

void Dinic(){
    while(SPFA()){
        DFS(s,inf);
    }
}

int main(){
    scanf("%d%d%d",&n,&m,&sum);
    t=n+m+1;
    for(int i=1;i<=m;i++){
        scanf("%d",&a[i]);
    }
    for(int i=1;i<=m;i++){
        scanf("%d",&b[i]);
    }
    for(int i=1;i<=n;i++){//第一步
        adding(s,i,sum,0);
        adding(i,s,0,0);
    }
    char c;
    for(int i=1;i<=n;i++){//第二步
        for(int j=1;j<=m;j++){
            c=getchar();
            if(c!='1'&&c!='0'){//防止字符串出锅
                j--;
                continue;
            }
            if(c=='1'){
                adding(i,j+n,1,-b[j]);
                adding(j+n,i,0,b[j]);
            }
        }
    }
    for(int i=1;i<=m;i++){//暴力第三步
        for(int j=1;j<=n;j++){
            adding(i+n,t,1,a[i]*(2*j-1));
            adding(t,i+n,0,-a[i]*(2*j-1));
        }
    }
    for(int i=1;i<=n;i++){//贪心优化
        adding(i,t,sum-1,0);
        adding(t,i,0,0);
    }
    Dinic();
    printf("%d",ans);
    return 0;
}
```
