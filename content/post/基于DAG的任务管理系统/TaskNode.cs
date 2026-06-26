using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;
using System;

[DefaultExecutionOrder(1)]
public class TaskNode : MonoBehaviour
{
    public string taskName;
    public string taskId;
    [Header("这个任务节点是哪些节点的前置节点")]
    public List<string> nextNodesIds = new List<string>();
    [Header("这个任务节点的影响物体和属性")]
    public List<TaskEffect> taskEffects = new List<TaskEffect>();

    [Header("这个任务结束后的效果")]
    public List<TaskEffect> taskEndEffects = new List<TaskEffect>();

    [Header("任务节点目标")]
    public List<TaskGoal> taskGoals = new List<TaskGoal>();

    [HideInInspector]
    public List<TaskNode> nextNodes = new List<TaskNode>();
    [HideInInspector]
    public int Out;
    private int In = 0;
    [HideInInspector]
    public bool isTaskFinished = false;

    // 取消相关
    private CancellationTokenSource _taskCts;
    private bool isTaskRunning = false;
    private bool isCompleting = false;

    public int Inn
    {
        get { return In; }
        set
        {
            // int oldIn = In;
            In = value;

            // 🎯 调试追踪：task21 的入度每次变化都打印调用堆栈
            // if (taskId == "task21")
            // {
            //     string delta = (In > oldIn) ? $"+{In - oldIn}" : $"{In - oldIn}";
            //     Debug.Log($"[DBG] task21 入度变化: {oldIn} → {In} ({delta}) | 调用栈:\n{StackTraceUtility.ExtractStackTrace()}");
            // }

            Debug.Log(taskId + "入度为" + In);
            TaskManager.Instance.SaveTaskNode(taskId);

            if (TaskManager.Instance.IsGraphInitialized && In <= 0 && !isTaskFinished)
            {
                StartTask();
            }
        }
    }

    void Awake()
    {
        TaskManager.Instance.AddTask(taskId, this);
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        CancelTask();
    }

    /// <summary>
    /// 启动任务（外部可调用）
    /// </summary>
    public void StartTask()
    {
        if (isTaskFinished)
        {
            Debug.Log($"任务 {taskId} 已完成，无需启动");
            return;
        }

        if (isTaskRunning)
        {
            Debug.Log($"任务 {taskId} 已在运行中");
            return;
        }

        StartTaskAsync().Forget();
    }

    /// <summary>
    /// 取消当前正在运行的任务
    /// </summary>
    public void CancelTask()
    {
        if (_taskCts != null)
        {
            _taskCts.Cancel();
            _taskCts.Dispose();
            _taskCts = null;
        }
        isTaskRunning = false;
    }

    /// <summary>
    /// 重置任务状态（用于读档后重新启动）
    /// </summary>
    public void ResetForLoad()
    {
        CancelTask();
        isTaskFinished = false;
        isTaskRunning = false;
        // 注意：TaskGoal 的 isDone 状态需要单独处理
    }

    // --- 【重构部分：新增刷新方法，用于替代原本的 while 轮询】 ---

    /// <summary>
    /// 刷新任务状态，检查目标是否全部达成
    /// </summary>
    public void RefreshStatus()
    {
        if (isTaskFinished) return;

        bool allDone = true;

        foreach (var goal in taskGoals)
        {
            // 直接读取重构后的 IsDone 属性（内部已关联 PlayingData 存档）
            if (!goal.IsDone)
            {
                allDone = false;
                break;
            }
        }

        if (allDone)
        {
            OnTaskSuccess();
        }
    }

    /// <summary>
    /// 统一处理任务成功的逻辑（原本在 CheckTaskFinishedAsync 的 if(allDone) 块中）
    /// </summary>
    private void OnTaskSuccess()
    {
        // 防止重复进入（可能通过 CheckGoalAsync 回调链再次触发）
        if (isCompleting || isTaskFinished) return;
        isCompleting = true;

        // 🎯 调试：调用栈追踪，看谁重复调了 OnTaskSuccess
        // Debug.LogError($"[DBG 调用栈] {taskId} 的 OnTaskSuccess 被调用\n{StackTraceUtility.ExtractStackTrace()}");

        isTaskFinished = true;

        // 任务完成时保存
        TaskManager.Instance.SaveTaskNode(taskId);

        // 任务完成后的效果
        // 原逻辑：先还原开始效果，再应用结束效果
        foreach (var effect in taskEffects)
        {
            effect.RevertEffect();
        }
        foreach (var effect in taskEndEffects)
        {
            effect.ApplyEffect();
        }

        // 减少后继节点的入度
        // 🎯 调试追踪：看看哪些后继被减了
        // string successors = string.Join(", ", nextNodes.ConvertAll(n => n.taskId));
        // Debug.Log($"[DBG] 任务 {taskId} 完成，通知后继: [{successors}]");
        foreach (var node in nextNodes)
        {
            node.Inn--;
        }

        Debug.Log($"任务 {taskId} 完成");

        // 无论成功或取消，确保清理状态
        isTaskRunning = false;
        if (TaskManager.Instance != null) TaskManager.Instance.UnregisterActive(this);

        // 停止异步等待
        CancelTask();

        isCompleting = false;
    }

    // --- 【重构部分结束】 ---

    private async UniTaskVoid StartTaskAsync()
    {
        // 取消之前的任务
        CancelTask();

        // 创建新的取消令牌
        _taskCts = new CancellationTokenSource();
        var token = _taskCts.Token;
        isTaskRunning = true;

        // 注册到当前活跃任务（用于外部可视化）
        if (TaskManager.Instance != null) TaskManager.Instance.RegisterActive(this);

        Debug.Log("Start Task: " + taskName + " " + taskId);

        try
        {
            // 引导任务逻辑开始
            // 应用开始效果
            foreach (var effect in taskEffects)
            {
                if (token.IsCancellationRequested) return;
                effect.ApplyEffect();
            }

            // 【关键改动】不再调用 while 循环的 CheckTaskFinishedAsync
            // 我们先执行一次初始检查（应对读档后立刻完成的情况）
            RefreshStatus();

            while (!isTaskFinished)
            {
                RefreshStatus(); // 里面会检查所有 Goal.IsDone
                if (isTaskFinished) break;

                await UniTask.Delay(250, cancellationToken: token); // 每0.25秒扫一次，性能极好
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"任务 {taskId} 被取消");
        }
        finally
        {
            // 确保状态清理（如果是被外部取消而非成功完成）
            if (!isTaskFinished)
            {
                isTaskRunning = false;
                if (TaskManager.Instance != null) TaskManager.Instance.UnregisterActive(this);
            }
        }
    }

    // 原 CheckTaskFinishedAsync 已被 RefreshStatus 和 OnTaskSuccess 逻辑平替

    // 可视化当前任务
    private void OnDrawGizmos()
    {
        if (isTaskRunning)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 1.0f);
        }
    }
}