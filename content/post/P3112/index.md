---
date: '2023-06-30T10:48:00+08:00'
draft: false
title: 'P3112 [USACO14DEC] Guard Mark G 题解'
tags:
    - 算法
    - 题解
---
有一说一这道题和 [国王游戏](https://www.luogu.com.cn/problem/P1080) 很像，但是多了一个高度的维度，当然起先我只认为用这个判断是否合法然后取最优解就好了，然后写了一发~~爆切57 pts~~。

```cpp
#include<bits/stdc++.h>
using namespace std;
const int N=25;
int n,h,cnt,t[N],ans=INT_MAX;
struct Cow{
    int height;
    int weight;
    int strength;
}e[N];

bool cmp(Cow A,Cow B){
    if(B.strength-A.weight!=A.strength-B.weight){
        return A.strength-B.weight>B.strength-A.weight;
    }else{
        return A.height>B.height;
    }
}

int main(){
    scanf("%d%d",&n,&h);
    for(int i=1;i<=n;i++){
        scanf("%d%d%d",&e[i].height,&e[i].weight,&e[i].strength);
    }
    sort(e+1,e+n+1,cmp);
    int sum=0;
    for(int i=1;i<=n;i++){
        t[i]=t[i-1]+e[i].weight;
        sum+=e[i].height;
        if(sum>=h){
            cnt=i;
            break;
        }
    }
    for(int i=1;i<=cnt;i++){
        ans=min(ans,e[i].strength-(t[cnt]-t[i]));
    }
    if(!cnt||ans<0){
        printf("Mark is too tall\n");
    }else{
        printf("%d",ans);
    }
    return 0;
}
```

那么问题出现在哪呢？注意到对于这道题这个贪心策略是不全面的，有的时候我们可以在排序后的奶牛中间移除一些没有那么优秀的，而且注意到 $n$ 的范围非常小，那么我们便可以使用状态压缩和搜索两种方式枚举使用哪些奶牛，这样就变成了一个完美的贪心策略。

而对于排序方式来说，我们注意到只考虑两个奶牛的话，两个相邻的奶牛位置的交换是对于其他奶牛来说没有任何影响，那么只需要按照两个奶牛的体重和承受能力的差值进行排序就好了。

复杂度：$\rm O(2^nn)$。

这里给出我的搜索代码。

```cpp
#include<bits/stdc++.h>
using namespace std;
const int N=25;
int n,h,ans=-INT_MAX,a[N],t[N];
struct Cow{
    int height;
    int weight;
    int strength;
}e[N],s[N];

bool cmp(Cow A,Cow B){
    if(B.strength-A.weight!=A.strength-B.weight){//贪心策略排序
        return A.strength-B.weight>B.strength-A.weight;
    }else{
        return A.height>B.height;
    }
}

int cal(){
    int temp=INT_MAX;
    int cnt=0;
    t[0]=0;
    for(int i=1;i<=n;i++){
        if(a[i]){
            s[++cnt]=e[i];//提取出选中的奶牛
        }
    }
    sort(s+1,s+1+cnt,cmp);//排序
    int sum=0;
    for(int i=1;i<=cnt;i++){
        sum+=s[i].height;
        t[i]=t[i-1]+s[i].weight;//前缀和节省复杂度
    }
    if(sum<h){//特判
        return -INT_MAX;
    }
    for(int i=1;i<=cnt;i++){
        temp=min(temp,s[i].strength-(t[cnt]-t[i]));//记录答案，这种情况的答案便是对于每一头奶牛的剩余承受重量取最小值
    }
    return temp;
}

void DFS(int dep,int now){
    a[dep]=now;
    if(dep==n){
        ans=max(ans,cal());
        return;
    }
    DFS(dep+1,1);
    DFS(dep+1,0);
    return;
}

int main(){
    scanf("%d%d",&n,&h);
    for(int i=1;i<=n;i++){
        scanf("%d%d%d",&e[i].height,&e[i].weight,&e[i].strength);
    }
    DFS(1,1);//搜索
    DFS(1,0);
    if(ans<0){//小于0说明根本搭不了 
        printf("Mark is too tall\n");
    }else{
        printf("%d",ans);
    }
    return 0;
}
```
