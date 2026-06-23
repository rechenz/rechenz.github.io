---
date: '2023-09-25T15:36:00+08:00'
draft: true
title: 'P5032 经验 题解'
tags:
    -算法
    -题解
---

同机房同学找到的题，一看 $\rm tag$，什么？队列？不会怎么办？

小问题，因为我会暴力。

首先，好长的题面，直接看简要题意，可以发现我们只需要求出最大能合成到多少等级，因为数学知识薄弱，打开 $\rm python$，发现
![1](https://cdn.luogu.com.cn/upload/image_hosting/i0wi8bsk.png)

既然只有 $23$ 那么我们便可以直接开一个数组（桶）来记录每个等级的附魔书出现了多少次，然后从下往上扫，便可以 $\rm O(n)$ 地求出最高能合成多大等级的附魔书。

那么下一步我们就要计算它的最小合成费用，因为合成继承的费用是两者之间的 $\rm max$ 加一，那么一个明显的贪心策略便是每次合成取两个当前费用最大的，其实这样暴力维护的话有点麻烦，但是根据递归的优先选择性其实根本不用特意维护。

```cpp
long long cal(int dep){
     if(a[dep]){
          a[dep]--;
          return 1;
     }
     int s1=cal(dep-1);
     int s2=cal(dep-1);
     y+=s1+s2;
     return (max(s1,s2)<<1)+1;
}
```

因为如果这样写的话，对于一个等级的附魔书来说一定是按照从小到大的顺序进行合并，而且因为一定是用偶数个进行合成，所以这依旧是满足我们的贪心策略的。

最后因为不保证模数为质数求逆元还要用扩展欧几里德~~说实话我觉得这很没必要~~

最后进行整体算法复杂度分析，考虑递归形成了一个满二叉树的最坏情况，叶子节点数便是 $\rm n$，那么根据树的基本知识这颗满二叉树的总节点数依旧是 $\rm n$ 量级的，所以总体算法复杂度 $\rm O(n)$。

```cpp
#include<bits/stdc++.h>
using namespace std;
inline void read(int &x){
     char ch=getchar();x=0;
     while(!isdigit(ch))   ch=getchar();
     while(isdigit(ch))   x=x*10+ch-'0',ch=getchar();
}
const int N=10000007;
int n,a[N],maxn,x;
long long y;

long long cal(int dep){
     if(a[dep]){
          a[dep]--;
          return 1;
     }
     int s1=cal(dep-1);
     int s2=cal(dep-1);
     y+=s1+s2;
     return (max(s1,s2)<<1)+1;
}

int ex_gcd(int s1,int s2,int &s3,int &s4){
     if(s2==0){
          s3=1;
          s4=0;
          return s1;
     }
     int temp=ex_gcd(s2,s1%s2,s4,s3);
     s4-=s3*(s1/s2);
     return temp; 
}

int main(){
     read(n);
     for(int i=1;i<=n;i++){
          int temp;
          read(temp);
          maxn=max(temp,maxn);
          a[temp]++;
     }
     int ans,temp=0,judge;
     for(int i=1;i<=maxn||temp;i++){
          temp=a[i]+temp>>1;
          x=max(x,i);
     }
     cal(x);
     judge=ex_gcd(x,y,ans,temp);
     if(judge!=1) return printf("-1"),0;
     else return printf("%lld",(ans%y+y)%y),0;
}
```
