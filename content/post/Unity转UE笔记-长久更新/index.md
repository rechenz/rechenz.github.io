---
date: '2026-08-08T14:40:15+08:00'
draft: false
title: 'Unity转UE笔记 长久更新'
tags:
    - 游戏开发
---

## 环境配置

显然，作为vscode重度用户以及半个完美主义者，我完全忍受不了VS的低扩展性以及丑的要死的界面还有它的重量，但是vscode的C++扩展对于UE的支持就是一坨，C++扩展的intelligense会把整个UE库全部塞进去，先不说内存占用，就是内联建议都没我的copilot快，这纯扯淡，所以我们需要去自己寻找更优秀的方案。

建议参考我的上一篇文章 [vscode进行UE开发配置指南](https://rechenz.github.io/post/%E4%BD%BF%E7%94%A8vscode%E8%BF%9B%E8%A1%8Cue%E5%BC%80%E5%8F%91%E6%8C%87%E5%8D%97/)

## 代码踩坑(长期更新)

你或许可以在我的[UE学习库](https://github.com/rechenz/IWannaBecomeUEMaster)里找到一些代码示例。

### 头文件顺序

UE库在底层使用了超级多诡异的宏定义，并且在编译的时候要一层一层去展开，那么这就导致了C++没有办法去自己找到一个合理的展开顺序，所以我们需要自己对头文件进行排序，当然其实也没有那么麻烦，我们只需要保证“generate.h”头文件要在最下面就好了。

当然我们要注意一点，如果你使用了vscode的自动格式化，那么一定要去改一下规则，因为有些情况格式化会把你的头文件提前，这个上面的配置教程里有写。

### 类引用

在Unity的C#环境中，C#集成了一个非常优秀的定义池，不仅保证了索引速度，还有实时热更新。

但是UE的C++环境中完全没有，我们甚至不能直接去在一个C++类的头文件里包含另一个C++类的头文件，这和头文件顺序是同一个错误类型，这会导致我们的宏定义展开错乱。

所以我们的正确实现变为了先在头文件里进行要引用的类声明，然后再在.cpp文件里引入完成的头文件。

下面是代码示例：

```C++
#pragma once

#include "GameFramework/Actor.h"
#include "Actor2.generated.h"

class AActor1;//提前定义需要使用的类

UCLASS()
class IWANNABECOMEUEMASTER_API AActor2 : public AActor
{
	GENERATED_BODY()

  public:
	AActor2();

  private:
	UPROPERTY(EditAnywhere, Category = "Event Binding")
	AActor1* BoundActor;//定义类的实例
};
```

```C++
#include "level1/Actor2.h"
#include "level1/Actor1.h"//在使用的时候引入完整的头文件

AActor2::AActor2()
{
	PrimaryActorTick.bCanEverTick = false;
}

void AActor2::BeginPlay()
{
	Super::BeginPlay();

	UE_LOG(LogTemp, Warning, TEXT("Actor2 BeginPlay() called!"));

}
```
