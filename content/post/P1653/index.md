---
date: '2023-05-11T21:01:00+08:00'
draft: false
title: 'P1653 [USACO04DEC] Cow Ski Area G'
tags:
    - 算法
    - 题解
---

# 题解
一句话：输出缩点后的出度为 $0$ 的点的个数和入度为 $0$ 的点的个数的最大值

首先对这个矩阵进行建模，如果能滑到就建一条单向边。

```cpp
for(int i=1;i<=n;i++){
        for(int j=1;j<=m;j++){
            if(i>1&&a[i-1][j]<=a[i][j]){
                adding(cal(i,j),cal(i-1,j));
            }
            if(i<n&&a[i+1][j]<=a[i][j]){
                adding(cal(i,j),cal(i+1,j));
            }
            if(j>1&&a[i][j-1]<=a[i][j]){
                adding(cal(i,j),cal(i,j-1));
            }
            if(j<m&&a[i][j+1]<=a[i][j]){
                adding(cal(i,j),cal(i,j+1));
            }
        }
    }
```

然后使用 Tarjan 进行缩点，最后记录缩点后的入度和出度就可以了，答案就是出度为 $0$ 的强连通分量的个数和入度为 $0$ 的强连通分量的个数取 $\max$，最后不要忘了特判一下如果只有一个强连通分量的话就不需要建立任何一道缆车。

## code

```cpp
#include<bits/stdc++.h>
using namespace std;
const int N=250005;
int n,m,head[N],cnt,a[505][505];

struct Edge{
    int to;
    int next;
}e[4*N];

void adding(int u,int v){
    e[++cnt].to=v;
    e[cnt].next=head[u];
    head[u]=cnt;
}

int cal(int i,int j){
    return (i-1)*m+j;
}
int dfn[N],low[N],id,vis[N],is[N],tot;
stack<int> s;
void Tarjan(int x){//缩点
    dfn[x]=low[x]=++id;
    s.push(x);
    vis[x]=1;
    for(int i=head[x];i;i=e[i].next){
        int v=e[i].to;
        if(!dfn[v]){
            Tarjan(v);
            low[x]=min(low[v],low[x]);
        }else if(vis[v]){
            low[x]=min(low[x],dfn[v]);
        }
    }
    if(dfn[x]==low[x]){
        ++tot;
        while(s.top()!=x){
            int temp=s.top();
            s.pop();
            is[temp]=tot;
            vis[temp]=0;
        }
        int temp=s.top();
        s.pop();
        is[temp]=tot;
        vis[temp]=0;
    }
}
int in[N],out[N],tt1,tt2;
int main(){
    scanf("%d%d",&m,&n);
    for(int i=1;i<=n;i++){
        for(int j=1;j<=m;j++){
            scanf("%d",&a[i][j]);
        }
    }
    for(int i=1;i<=n;i++){//建边
        for(int j=1;j<=m;j++){
            if(i>1&&a[i-1][j]<=a[i][j]){
                adding(cal(i,j),cal(i-1,j));
            }
            if(i<n&&a[i+1][j]<=a[i][j]){
                adding(cal(i,j),cal(i+1,j));
            }
            if(j>1&&a[i][j-1]<=a[i][j]){
                adding(cal(i,j),cal(i,j-1));
            }
            if(j<m&&a[i][j+1]<=a[i][j]){
                adding(cal(i,j),cal(i,j+1));
            }
        }
    }
    for(int i=1;i<=n*m;i++){
        if(!dfn[i]) Tarjan(i);
    }
    for(int k=1;k<=n*m;k++){//记录入度和出度
        for(int i=head[k];i;i=e[i].next){
            int v=e[i].to;
            if(is[v]!=is[k]){
                in[is[v]]++;
                out[is[k]]++;
            }
        }
    }
    for(int k=1;k<=tot;k++){
        if(!in[k]) tt1++;
        if(!out[k]) tt2++;
    }
    if(tot==1){//输出答案
        printf("0");
    }else{
        printf("%d",max(tt1,tt2));
    }
    return 0;
}
```
