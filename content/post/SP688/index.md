---
date: '2023-06-12T14:10:00+08:00'
draft: false
title: 'SP688 SAM - Toy Cars题解'
tags:
    - 算法
    - 题解
---
# 双倍经验[P3419](https://www.luogu.com.cn/problem/P3419)

其实我第一眼看到这道题的时候以为是一道 $\texttt {DP}$，但是想了想就发现要记录的状态实在太多，于是就去想贪心。

考虑每次拿玩具对总体的贡献，因为能同时放在地上的玩具个数有限，而我们又只计算拿玩具下来的次数，于是便可得出一个贪心结论，那便是对于把玩具放回去这个行为，下一次玩这个玩具的时间距离现在越远一定越优，因为我们不能不玩任何一个玩具。

所以我们在把玩的顺序读入后，倒序枚举预处理出这个玩具下一次玩的时间，那么至于如何选择，只需要用一个优先队列维护即可，因为要存两维，一个是用来排序的 $\texttt {next}$ 数组，一个是这个玩具的 $\texttt{id}$，所以我们用优先队列存一个 $\texttt{pair}$ 类即可。

复杂度为跑不满的 $\rm O(nlogn)$。

另外温馨提示，多测不清空，爆零两行泪。

```cpp
#include<bits/stdc++.h>
using namespace std;
const int N=500005;
int n,k,p,a[N],nex[N],temp[N],ans,vis[N],tot;

priority_queue<int,vector<pair<int,int>>>q;
//也可以这样写
// priority_queue<pair<int,int>>q;
void init(){//预处理函数
    while(!q.empty()){
        q.pop();
    }
    memset(temp,0,sizeof temp);
    memset(nex,0,sizeof nex);
    memset(vis,0,sizeof vis);
    ans=0;
}

int main(){
    int T;
    scanf("%d",&T);
    for(int l=1;l<=T;l++){
        scanf("%d%d%d",&n,&k,&p);
        for(int i=1;i<=p;i++){
            scanf("%d",&a[i]);
        }
        for(int i=p;i>=1;i--){//预处理出nex数组
            if(temp[a[i]]==0) nex[i]=1e6;
            else nex[i]=temp[a[i]];
            temp[a[i]]=i;
        }
        for(int i=1;i<=p;i++){
            if(!vis[a[i]]){
                if(q.size()==k){
                    vis[q.top().second]=0;
                    q.pop();
                }
                q.push(make_pair(nex[i],a[i]));
                ans++;
                vis[a[i]]=1;
            }else{//因为有新的进入队列更新，而优先队列不好维护弹出，所以把范围开大一个，防止错误弹出
                k++;
                q.push(make_pair(nex[i],a[i]));
            }
        }
        printf("%d\n",ans);
        init();
    }
    return 0;
}
```
