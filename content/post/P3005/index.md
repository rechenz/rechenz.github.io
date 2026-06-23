---
date: '2023-05-30T13:28:00+08:00'
draft: false
title: 'P3005 [USACO10DEC]The Trough Game S题解'
tags:
    - 算法
    - 题解
---
首先看到这道题的数据范围就可以想到是枚举暴力。

而对于枚举我们有两种方法，一种是 $\texttt {DFS}$，而另一种是通过二进制来进行枚举（~~反正我用的 $\texttt {DFS}$~~）。

而这题有两种情况的翻译并没有给出，一种是无解输出 $\texttt{IMPOSSIBLE}$，另一种是解法不唯一输出 $\texttt {NOT UNIQUE}$。

而对于 $\texttt{IMPOSSIBLE}$ 我们只需要判断 $\texttt {DFS}$ 后是否找到了一种解就好了。

对于 $\texttt {NOT UNIQUE}$ 情况我们也只需要在判断是否找到了不唯一的合法解就可以了。

复杂度 ${\rm O}(2^{n}m)$，不完全估计 ${\rm O}(104857600)$，$\texttt{1s}$ 肯定是能过的啦。

# code

```cpp
#include<bits/stdc++.h>
using namespace std;

int n,m,t[25],ans[25];
bool flag=false,judge;
struct Query{
    int a[25];
    int num;
}q[105];

bool check(){//判断解是否合法的函数
    for(int i=1;i<=m;i++){
        int temp=0;
        for(int j=1;j<=n;j++){
            if(q[i].a[j]&&t[j]){
                temp++;//记录搜到的结果的有草料的个数
            }
        }
        if(temp!=q[i].num){//不符合条件直接返回
            return false;
        }
    }
    for(int i=1;i<=n;i++){
        ans[i]=t[i];
    }
    if(flag==true){//是否有过合法解
        judge=true;
    }
    return true;
}

void DFS(int dep){
    if(dep==n){//搜完n个就检查是否合法
        if(check()){
            flag=true;
        }
        return;
    }
    t[dep+1]=1;//枚举每一种情况
    DFS(dep+1);
    // if(flag) return;
    t[dep+1]=0;
    DFS(dep+1);
    // if(flag) return;
}

int main(){
    scanf("%d%d",&n,&m);
    for(int i=1;i<=m;i++){
        for(int j=1;j<=n;j++){
            char s;
            cin>>s;
            if(s==' '||s=='\n'){//防止玄学字符串出锅
                j--;
                continue;
            }
            q[i].a[j]=s-'0';
        }
        scanf("%d",&q[i].num);
    }
    //搜索
    t[1]=1;
    DFS(1);
    t[1]=0;
    DFS(1);
    if(!flag){
        printf("IMPOSSIBLE");
        return 0;
    }
    if(judge){
        printf("NOT UNIQUE");
        return 0;
    }
    for(int i=1;i<=n;i++){
        printf("%d",ans[i]);
    }
    return 0;
}
```
