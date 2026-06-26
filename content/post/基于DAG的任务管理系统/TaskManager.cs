using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }
    public bool isGraphInitialized = false;
    public bool IsGraphInitialized => isGraphInitialized;

    public GameObject mainPlayer; // 主玩家对象
    private bool isLoadingTasks = false;  // 防止加载时重复保存
    private Coroutine _initGraphCoroutine; // 保存 InitTaskGraph 协程引用，以便读档时停止

    private HashSet<TaskNode> activeTasks = new HashSet<TaskNode>();

    public void RegisterActive(TaskNode node) => activeTasks.Add(node);
    public void UnregisterActive(TaskNode node) => activeTasks.Remove(node);

    private void Awake()
    {
        Debug.Log("TaskManager Awake");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _initGraphCoroutine = StartCoroutine(InitTaskGraph());
    }

    private static Dictionary<string, TaskNode> tasks = new Dictionary<string, TaskNode>();

    public void AddTask(string taskId, TaskNode taskNode)
    {
        if (tasks.TryGetValue(taskId, out var existing))
        {
            if (existing == taskNode)
            {
                // 同一实例重新注册，忽略
                return;
            }
            // 旧节点仍然存活 —— 可能是清理不彻底，覆盖并销毁旧节点
            Debug.LogWarning($"[TaskManager] TaskId '{taskId}' 冲突！旧节点: {existing.name}(inst:{existing.GetInstanceID()}), 新节点: {taskNode.name}(inst:{taskNode.GetInstanceID()})");
            Debug.LogWarning($"[TaskManager] 销毁旧节点，使用新节点");
            if (existing != null && existing.gameObject != null)
            {
                existing.CancelTask();
                Destroy(existing.gameObject);
            }
            tasks[taskId] = taskNode;
        }
        else
        {
            tasks.Add(taskId, taskNode);
        }
    }

    public TaskNode GetTask(string taskId)
    {
        if (tasks.ContainsKey(taskId))
        {
            return tasks[taskId];
        }
        else
        {
            Debug.LogError("TaskId does not exist: " + taskId);
            return null;
        }
    }

    IEnumerator InitTaskGraph()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (isGraphInitialized) yield break;

        Debug.Log($"开始初始化任务图，共 {tasks.Count} 个任务");

        // 初始化任务图
        foreach (var task in tasks)
        {
            var taskNode = task.Value;
            SaveTaskNode(task.Key);

            foreach (var id in taskNode.nextNodesIds)
            {
                TaskNode targetTask = TaskManager.Instance.GetTask(id);
                if (targetTask == null)
                {
                    Debug.Log("TaskNode not found: " + id);
                }
                else
                {
                    taskNode.nextNodes.Add(targetTask);
                    targetTask.Inn++;
                    taskNode.Out++;
                }
            }
        }

        // 加载所有任务节点存档（会恢复任务状态）
        LoadAllTaskNodes();

        isGraphInitialized = true;

        // 图初始化完成后，启动所有入度为0且未完成的任务
        StartAllReadyTasks();
    }

    /// <summary>
    /// 启动所有可以开始的任务（入度为0且未完成）
    /// </summary>
    private void StartAllReadyTasks()
    {
        foreach (var task in tasks.Values)
        {
            if (task.Inn <= 0 && !task.isTaskFinished)
            {
                Debug.Log($"启动任务: {task.taskId}");
                task.StartTask();
            }
        }
    }

    /// <summary>
    /// 保存所有任务节点到存档
    /// </summary>
    public void SaveAllTaskNodes()
    {
        if (isLoadingTasks) return;  // 加载期间不保存

        foreach (var task in tasks)
        {
            SaveTaskNode(task.Key);
        }
    }

    /// <summary>
    /// 从存档中加载所有任务节点
    /// </summary>
    public void LoadAllTaskNodes()
    {
        isLoadingTasks = true;

        // 在加载存档前先重置所有任务，确保之前运行中的旧任务不会保留
        foreach (var t in tasks)
        {
            t.Value.ResetForLoad();
        }

        foreach (var task in tasks)
        {
            if (GameFlowManager.Instance.PlayingData.TaskNodesDic.ContainsKey(task.Key))
            {
                LoadTaskNode(task.Key);
            }
            else
            {
                Debug.Log("未找到对应节点存档: " + task.Key);
            }
        }

        isLoadingTasks = false;
    }

    public void SaveTaskNode(string ID)
    {
        if (isLoadingTasks) return;  // 加载期间不保存

        TaskNode taskNode = GetTask(ID);
        if (taskNode == null)
        {
            Debug.LogError("TaskNode not found: " + ID);
            return;
        }

        if (GameFlowManager.Instance?.PlayingData?.TaskNodesDic == null)
        {
            Debug.LogWarning("GameFlowManager.PlayingData 未就绪，跳过保存");
            return;
        }

        if (GameFlowManager.Instance.PlayingData.TaskNodesDic.ContainsKey(ID))
        {
            GameFlowManager.Instance.PlayingData.TaskNodesDic[ID] = (taskNode.Inn, taskNode.isTaskFinished);
            Debug.Log("已更新任务存档: " + ID);
        }
        else
        {
            GameFlowManager.Instance.PlayingData.TaskNodesDic.Add(ID, (taskNode.Inn, taskNode.isTaskFinished));
            Debug.Log("未找到对应节点存档，已自动创建: " + ID);
        }
    }

    public void LoadTaskNode(string ID)
    {
        TaskNode taskNode = GetTask(ID);
        if (taskNode == null) return;

        if (GameFlowManager.Instance?.PlayingData?.TaskNodesDic.ContainsKey(ID) == true)
        {
            var (inn, isFinished) = GameFlowManager.Instance.PlayingData.TaskNodesDic[ID];

            // 修正：入度不应为负数（防御性 clamp）
            if (inn < 0)
            {
                Debug.LogWarning($"[TaskManager] 任务 {ID} 存档入度为 {inn}，已修正为 0");
                inn = 0;
            }

            // 恢复任务核心数值
            taskNode.isTaskFinished = isFinished;
            taskNode.Inn = inn;

            // 执行一次完成表现（比如关闭相关 UI 或触发后续）
            if (isFinished)
            {
            }

            Debug.Log("已加载任务存档: " + ID);
        }
    }

    /// <summary>
    /// 重置并重新启动所有未完成的任务（读档后调用）
    /// </summary>
    public void ReloadAndRestartTasks()
    {
        Debug.Log("重新加载并重启未完成的任务");

        // 1. 取消所有正在运行的任务
        foreach (var task in tasks.Values)
        {
            if (!task.isTaskFinished)
            {
                task.CancelTask();
            }
        }

        // 2. 重新加载任务状态
        LoadAllTaskNodes();

        // 3. 延迟一帧后启动可开始的任务
        StartCoroutine(DelayedStartReadyTasks());
    }

    private IEnumerator DelayedStartReadyTasks()
    {
        yield return null;  // 等待一帧，确保所有状态已恢复

        foreach (var task in tasks.Values)
        {
            if (task.Inn <= 0 && !task.isTaskFinished)
            {
                Debug.Log($"重新启动任务: {task.taskId}");
                task.StartTask();
            }
        }
    }

    // ========== 读档专用流程：重置运行状态 → 加载场景 → 重建图 ==========

    /// <summary>
    /// 重置所有任务节点的运行状态（读档时、加载新场景前调用）。
    /// 不销毁 GameObject（节点在 GlobalManager/T 层级中需要跨场景存活），
    /// 只取消运行中任务、清理 nextNodes 引用、重置图初始化标记。
    /// </summary>
    public void ClearAllTaskNodesForLoad()
    {
        Debug.Log("[TaskManager] ========== 重置所有任务运行状态（读档准备）==========");

        // 1. 取消所有正在运行的任务
        foreach (var kvp in tasks)
        {
            kvp.Value?.CancelTask();
        }

        // 2. 清除所有 nextNodes 引用（连接将在 RebuildGraphFromSave 中重建）
        foreach (var kvp in tasks)
        {
            if (kvp.Value != null)
            {
                kvp.Value.nextNodes.Clear();
                kvp.Value.Out = 0;
            }
        }

        // 3. 停掉 InitTaskGraph 协程（如果还在运行），防止它在 LoadMain 期间重建图并启动任务
        if (_initGraphCoroutine != null)
        {
            StopCoroutine(_initGraphCoroutine);
            _initGraphCoroutine = null;
            Debug.Log("[TaskManager] 已停止 InitTaskGraph 协程");
        }

        // 4. 清空活跃集合、重置图初始化标记、锁定读存档标记
        activeTasks.Clear();
        isGraphInitialized = false;
        isLoadingTasks = false;

        Debug.Log("[TaskManager] ========== 任务运行状态已全部重置 ==========");
    }

    /// <summary>
    /// 场景加载完成后，从存档重建任务图。
    /// 前提：TaskNode 对象在 GlobalManager/T 层级中未销毁，字典中引用仍有效。
    /// </summary>
    public void RebuildGraphFromSave()
    {
        Debug.Log("[TaskManager] ========== 从存档重建任务图 ==========");
        Debug.Log($"[TaskManager] 当前已注册任务数: {tasks.Count}");

        // ------------------------------------------------------------------
        // 阶段 1：构建图连接（nextNodes），同时禁止 Inn setter 覆盖存档
        // ------------------------------------------------------------------
        isLoadingTasks = true; // SaveTaskNode 中会 return，保护存档不被覆盖

        // 1a. 先将所有节点的 Inn 归零（避免旧入度残留）
        foreach (var kvp in tasks)
        {
            if (kvp.Value != null)
                kvp.Value.Inn = 0;
        }

        // 1b. 重建图连接
        foreach (var kvp in tasks)
        {
            var taskNode = kvp.Value;
            taskNode.nextNodes.Clear();

            foreach (var id in taskNode.nextNodesIds)
            {
                TaskNode targetTask = GetTask(id);
                if (targetTask != null)
                {
                    taskNode.nextNodes.Add(targetTask);
                    targetTask.Inn++;       // isLoadingTasks=true，SaveTaskNode 被跳过
                    taskNode.Out++;
                }
            }
        }

        // ------------------------------------------------------------------
        // 阶段 2：恢复存档写入，从存档加载（恢复正确的 Inn / isFinished）
        // ------------------------------------------------------------------
        isLoadingTasks = false;
        LoadAllTaskNodes();

        // ------------------------------------------------------------------
        // 阶段 3：标记图已初始化，启动就绪任务
        // ------------------------------------------------------------------
        isGraphInitialized = true;
        Debug.Log("[TaskManager] isGraphInitialized = true");

        StartAllReadyTasks();

        Debug.Log("[TaskManager] ========== 任务图重建完成 ==========");
    }

    public void OnGoalReached(TaskGoal goal)
    {
        // 找出目前正在运行的任务中，哪一个包含了这个目标
        foreach (var node in activeTasks)
        {
            if (node.taskGoals.Contains(goal))
            {
                // 立即让该节点检查自己是否全目标完成
                node.RefreshStatus();
                break;
            }
        }
    }


    [ContextMenu("Finish All Active Tasks")]
    public void FinishAllActiveTasks()
    {
        foreach (var node in activeTasks)
        {
            foreach (var goal in node.taskGoals)
            {
                if (goal.targetScript != null)
                    goal.targetScript.FinishTask();
                else
                    goal.IsDone = true;
            }
        }
    }

    // 可视化当前任务（仅在编辑器中显示）
#if UNITY_EDITOR
    private void OnGUI()
    {
        if (activeTasks.Count == 0) return;

        GUI.color = Color.red;
        GUILayout.BeginArea(new Rect(15, 15, 300, 500));
        GUILayout.Label("--- ACTIVE TASKS ---", GUI.skin.box);
        foreach (var task in activeTasks)
        {
            GUILayout.Label($"ID: {task.taskId} | Name: {task.taskName}");
        }
        GUILayout.EndArea();
    }
#endif
}