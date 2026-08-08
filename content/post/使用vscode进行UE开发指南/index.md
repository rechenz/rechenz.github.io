---
date: '2026-08-08T12:30:00+08:00'
draft: false
title: '使用vscode进行UE开发指南'
tags:
    - 游戏开发
---

> 适用：UE 5.x 项目 + VS Code 开发，用 clangd 做代码索引/补全/报错。
> 本文是完整配置教学 + 实战踩坑记录，按顺序做就能跑通。

<!--more-->

## 为什么要用 VS Code + clangd

UE 官方标配是 Visual Studio，但用 VS Code 开发完全可行，而且更轻。核心组件是 **clangd** —— 一个基于 LLVM 的 C++ 语言服务器，负责补全、跳转、报错、重构提示。

clangd 有个铁律：**不自己猜编译参数，全靠 `compile_commands.json` 喂**。UE 的 UnrealBuildTool 可以一键生成这份文件，于是整条链路就是：

```
UE 的 UnrealBuildTool
   │  -mode=GenerateClangDatabase
   ▼
compile_commands.json   ← 每个 .cpp 的完整编译命令（include 路径、宏定义、标准库）
   │
   ▼
clangd 读取 → 索引 → 补全 / 跳转 / 报错 / inlay hints
```

所以配置的关键就两件事：**让 compile_commands.json 正确生成**、**让 clangd 正确读到它**。

## 环境要求

- UE 5.x（本文基于 5.8）
- VS Code
- VS Code 扩展：`clangd`（Microsoft 官方）

## 第一步：安装扩展

安装 clangd 扩展后，把 C/C++ 扩展的 IntelliSense 关掉，两个会打架（重复报错、吃性能）：

`.vscode/settings.json`：

```json
{
  "C_Cpp.intelliSenseEngine": "disabled"
}
```

## 第二步：配置 tasks.json（重点！）

任务用来跑 UBT。**这里有一个大坑**（见踩坑 1），先直接抄正确配置：

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "Build Editor (Development)",
      "type": "process",
      "command": "cmd.exe",
      "args": [
        "/c",
        "E:/Epicgames/games/UE_5.8/Engine/Build/BatchFiles/Build.bat",
        "YourProjectEditor",
        "Win64",
        "Development",
        "-Project=${workspaceFolder}/YourProject.uproject",
        "-WaitMutex"
      ],
      "group": { "kind": "build", "isDefault": true },
      "problemMatcher": ["$msCompile"]
    },
    {
      "label": "Generate Clang Database",
      "type": "process",
      "command": "E:/Epicgames/games/UE_5.8/Engine/Binaries/DotNET/UnrealBuildTool/UnrealBuildTool.exe",
      "args": [
        "-mode=GenerateClangDatabase",
        "-project=${workspaceFolder}/YourProject.uproject",
        "YourProjectEditor",
        "Win64",
        "Development",
        "-OutputDir=${workspaceFolder}"
      ],
      "problemMatcher": []
    }
  ]
}
```

只需改三处：引擎路径、项目名（`YourProject`）、`.uproject` 路径。`${workspaceFolder}` 会自动展开，不用动。

## 第三步：生成编译数据库

`Ctrl+Shift+P` → `Tasks: Run Task` → **Generate Clang Database**

跑完项目根目录出现 `compile_commands.json` 就成功。首次大概 1 分钟（要跑 UHT）。

## 第四步：配置 settings.json

```json
{
  "C_Cpp.intelliSenseEngine": "disabled",
  "editor.inlayHints.enabled": "on",
  "clangd.arguments": [
    "--compile-commands-dir=${workspaceFolder}",
    "--background-index",
    "-j=6",
    "--header-insertion=never"
  ]
}
```

关键项：

- `--compile-commands-dir`：指向 compile_commands.json 所在目录（项目根），这是 clangd 找到编译数据库的钥匙
- `editor.inlayHints.enabled`：开启 **inlay hints**（内联提示）——函数调用边上显示参数名、`auto` 显示推断类型的灰字。clangd 默认会发，但 VS Code 默认不显示，必须手动开

如果重启 clangd 后还是没有内联提示，可以往 `clangd.arguments` 加更细的控制：

```json
"--inlay-hints=parameters+types"
```

`parameters` 显示参数名、`types` 显示 auto 推断类型，`+` 组合。

改完配置后：`Ctrl+Shift+P` → `clangd: Restart language server` 生效。

## 日常使用规则（就一条）

| 你做了什么              | 要不要刷                         |
| ----------------------- | -------------------------------- |
| 改 .cpp / .h 内容       | ❌ clangd 自动重新解析            |
| **新增 .cpp / .h 文件** | ✅ 跑一次 Generate Clang Database |
| 改 Build.cs / 模块依赖  | ✅ 跑一次                         |

UE 编辑器里的 `Tools → Generate Visual Studio project files` **不用点**——那是给 Visual Studio 生成 .sln 的，跟 clangd 无关。放着别删，以后万一要用 VS 调试还有用。

## 踩坑记录（全是血泪）

### 坑 1：任务报 `'cmd /c ...' is not recognized`

**现象：**

```
& 'cmd /c E:/.../UnrealBuildTool.exe' is not recognized as a name of a cmdlet
```

**原因：** tasks.json 写成 `type: "shell"` + `command: "cmd /c xxx"`。VS Code 的默认 shell 是 PowerShell，执行 shell 任务时会生成 `& 'cmd /c ...' 参数`，PowerShell 把整串当**命令名**，自然找不到。

**解法：** 任务类型改 `"type": "process"`，绕开 shell 解析，直接 CreateProcess 调 exe。Build.bat 是批处理不能直接被 CreateProcess 调，所以用 `cmd.exe` + `/c` 显式包一层。

### 坑 2：clangd 报 `'xxx.h' file not found`，但文件明明存在

**现象：** clangd 报找不到自己的头文件，后面跟一串"连锁反应"报错（未声明标识符之类）。

**先别信任何人的"根因分析"，自己实测三步：**

① 看 compile_commands.json 里这个文件的记录，确认 rsp 文件存在：

```powershell
Test-Path "项目/Intermediate/Build/Win64/x64/UnrealEditor/Development/模块名/Actor1.cpp.obj.rsp"
```

② 用 clang-cl 按 rsp 实测语法检查：

```powershell
# 关键：工作目录必须切到 compile_commands.json 里的 directory（Engine/Source）！
# rsp 里全是相对路径，目录不对引擎头文件全找不到，会误导排查
cd E:/Epicgames/games/UE_5.8/Engine/Source
clang-cl -fsyntax-only "@...Actor1.cpp.obj.rsp"
```

③ 直接调 clangd 复现（最接近 VS Code 里的真相）：

```powershell
clangd --compile-commands-dir=F:/project/YourProject --check=F:/project/YourProject/Source/.../Actor1.cpp
# 看最后一行：All checks completed, N errors
```

**真实案例：** 曾有人（AI）分析说"clangd 解析不了嵌套 rsp 所以丢了 include 路径"，建议往 `.clangd` 里硬塞 `-I`。实测 clangd 22 **完美展开嵌套 rsp**，Public 路径全在，头文件根本没丢。真正报错是另外两个，见坑 3、坑 4。

> 教训：rsp 嵌套、路径解析这种问题，别猜，跑一遍就知道。往 `.clangd` 加 `-I` 是给健康代码贴创可贴。

### 坑 3：`member access into incomplete type 'class FTimerManager'`

**现象：** `GetWorldTimerManager().SetTimer(...)` 报 FTimerManager 不完整类型。

**原因：** 头文件（Actor.h）对 FTimerManager 只有前向声明，调用它的方法需要完整定义。

**解法：** cpp 里加：

```cpp
#include "TimerManager.h"
```

这是 UE 官方标准用法，所有用 SetTimer 的 Actor 都该带。

### 坑 4：声明了函数却没实现 → 链接期 LNK2019

**现象：** clangd 不报（语法没错），但编译链接时炸。

**原因：** 头文件里 `UFUNCTION() void TriggerEvent();` 声明了，UHT 生成的 `.gen.cpp` 会引用它，cpp 里却没有定义。

**解法：** 补实现，别留空壳：

```cpp
void AActor1::TriggerEvent()
{
    UE_LOG(LogTemp, Warning, TEXT("Actor1 TriggerEvent() called!"));
    OnSomethingHappened.Broadcast();
}
```

## 验证流程速查

```powershell
# 1. 生成/刷新编译数据库（新增文件后必跑）
#    VS Code: Tasks: Run Task → Generate Clang Database

# 2. 让 clangd 全量检查单文件
clangd --compile-commands-dir=F:/project/YourProject --check=F:/project/YourProject/Source/.../你的文件.cpp
```

看到 `All checks completed, 0 errors` 就是全绿。

## 附录：环境备忘（实测值）

| 项                            | 值                                                                |
| ----------------------------- | ----------------------------------------------------------------- |
| UE 引擎                       | `E:/Epicgames/games/UE_5.8`                                       |
| clangd                        | 22.1.3（VS 18 自带 LLVM，比 UE 首选 20.1.8 新，UBT 有警告但能用） |
| compile_commands 的 directory | `E:/Epicgames/games/UE_5.8/Engine/Source`                         |
| 手动验证必切目录              | 就是上面那个，别在项目目录跑                                      |

---
