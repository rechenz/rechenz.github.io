---
date: '2023-08-08T20:34:00+08:00'
draft: true
title: 'P4645 [COCI2006-2007#3] BICIKLI 题解'
tags:
    -算法
    -题解
---

一道很好的图论练手题。

难得题意很简洁，就是让你进行一个路径计数，看到这么简洁的路径计数我第一眼想到的就是通过拓扑排序进行计数 $\texttt{dp}$，并且题中说明了是单向道路，那么这就是一个很简单的 $\texttt{DAG}$ 上 $\texttt{dp}$。

但是仍然有一些地方需要注意，因为题中并没有保证这张图是一张有向无环图，所以我们要去判断有环的情况，我们跑一遍 $\texttt{Tarjan}$ 算法便可以找出这个图中所有的环，但是题中的所需要的特判当且仅当路径数为无穷时，所以我们要找的是图中对从 $\texttt{1}$ 号节点到 $\texttt{2}$ 号节点路径数有贡献的环。

至于如何判断这个环是否对路径数有贡献，这个环对路径数有贡献当且仅当可以从 $\texttt{1}$ 号节点走到这个环上并且可以从这个环走到 $\texttt{2}$ 号节点上。

想要知道能否从 $\texttt{1}$ 号节点到环很简单，搜一遍就好了，但是能否从环到达 $\texttt{2}$ 号节点就是另一个问题了，正所谓正难则反，我们另外建出一份反图从 $\texttt{2}$ 号节点跑一遍和刚才一样的就好了，这也是图论题中常用的技巧。

另外一个注意事项，拓扑排序中我们需要根据下一个的入度来判断是否可以入队，但是有些点和边是根本遍历不到的，那么就会走到半路就寄了，所以我们要在判断是否能从 $\texttt{1}$ 号节点到环的时候顺便记录一下有用的入度，当然记得要用 $\texttt{BFS}$ 不然会多加出一些入度。

时间复杂度:$\rm O(n)$。

（注：本题诈骗数据，记得开 $10^5$）

# code

```cpp
#include<bits/stdc++.h>
using namespace std;
#define int long long
const int N=100004;
const int mod=1e9;
int n,m,head[N],cnt,in[N],dis[N],head1[N],cnt1;

struct Edge{
	int to;
	int next;
}e[N],e1[N];

void adding(int u,int v){
	e[++cnt].to=v;
	e[cnt].next=head[u];
	head[u]=cnt;
}

void adding1(int u,int v){
	e1[++cnt1].to=v;
	e1[cnt1].next=head1[u];
	head1[u]=cnt1;
}

int dfn[N],low[N],tot,vis[N],num,size[N],color[N];
bool check[N],check1[N];
stack<int> s;
void Tarjan(int x){//Tarjan板子不多说了 
	dfn[x]=low[x]=++tot;
	s.push(x);
	vis[x]=1;
	for(int i=head[x];i;i=e[i].next){
		int v=e[i].to;
		if(!dfn[v]){
			Tarjan(v);
			low[x]=min(low[x],low[v]);
		}else if(vis[v]){
			low[x]=min(low[x],low[v]);
		}
	}
	if(dfn[x]==low[x]){
		num++;
		while(s.top()!=x){
			int temp=s.top();
			s.pop();
			vis[temp]=0;
			size[num]++;
			color[temp]=num;
		}
		vis[x]=0;
		size[num]++;
		color[x]=num;
		s.pop();
	}
}

queue<int>q;
void BFS1(int x){
	q.push(1);
	check[1]=1;
	while(q.size()){
		int x=q.front();
		q.pop();
		for(int i=head[x];i;i=e[i].next){
			int v=e[i].to;
			if(!check[v]){
				check[v]=1;
				q.push(v);
			}
			in[v]++;//记录入度 
		}
	}
}

void DFS1(int x){
	for(int i=head1[x];i;i=e1[i].next){
		int v=e1[i].to;
		if(check1[v]) continue;
		check1[v]=1;
		DFS1(v);
	}
}

void tuopu(){
	dis[1]=1;
	q.push(1);
	while(q.size()){
		int x=q.front();
		q.pop();
		for(int i=head[x];i;i=e[i].next){
			int v=e[i].to;
			if(check1[v]){
				dis[v]+=dis[x];
				dis[v]%=mod;
				in[v]--;
				if(in[v]==0){//入度为零才能加 
					q.push(v);
				}
			}
		}
	}
}

signed main(){
	scanf("%lld%lld",&n,&m);
	for(int i=1;i<=m;i++){
		int s1,s2;
		scanf("%lld%lld",&s1,&s2);
		adding(s1,s2);
		adding1(s2,s1);//建反图 
	}
	for(int i=1;i<=n;i++){
		if(!dfn[i]) Tarjan(i);
	}
	check[1]=1;//记录正图的点能否被遍历到 
	check1[2]=1;//同上记录反图 
	BFS1(1);//第一遍要用BFS 
	DFS1(2);
	for(int i=1;i<=n;i++){
		if(check[i]&&size[color[i]]!=1&&check1[i]){
			printf("inf\n");
			return 0;
		}
	}
	tuopu();
	printf("%lld",dis[2]);
	return 0;
}
```
