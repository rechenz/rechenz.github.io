using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class TaskGoal
{
    [Tooltip("调试用，或者在Demo里占位")]
    public bool isAutoFinish;
    public TaskGoalType taskGoalType;

    [Header("存档唯一标识符(全局唯一)")]
    public string goalId;

    public string targetId;

    public bool isCheckPlayer = false;
    [Header("是否应该触发")]
    public bool GoalTrigger;
    [Header("是否应该获得")]
    public bool GoalItem;
    [Header("角色目标位置")]
    public string sceneName;
    public Vector3 GoalPosition;
    public Vector3 GoalRotation;
    [Header("位置和旋转的容差")]
    public float positionTolerance = 0.1f;
    [Range(0, 360)]
    public float rotationTolerance = 0.1f;

    [Header("查找对话")]
    public string targetDialogueId = null;
    [Header("对话索引")]
    public int index = 0;

    [Header("对应脚本协程，协程函数统一命名为TaskIEnumerator")]
    public TaskBasic targetScript;

    private BaseState targetState;
    private bool isChecking = false;
    private UniTaskCompletionSource<bool> pendingCheck;

    // 递归防护
    [System.NonSerialized] private int _setIsDoneGuard = 0;
    private const int MaxSetIsDoneDepth = 3;

    public bool IsDone
    {
        get
        {
            if (GameFlowManager.Instance?.PlayingData == null) return false;

            // 拿数据
            var goalState = GameFlowManager.Instance.PlayingData.GetState<TaskGoalState>(goalId);

            // 异步检查（仅在未完成且未在检查时启动）
            if (!goalState.isReached && !isChecking)
            {
                CheckGoalAsync().Forget(e =>
                {
                    if (e != null)
                        Debug.LogError($"CheckGoalAsync 异常 in {goalId}: {e}");
                });
            }

            return goalState.isReached;
        }
        set
        {
            // 递归防护
            if (_setIsDoneGuard > MaxSetIsDoneDepth)
            {
                Debug.LogError($"IsDone setter 递归调用过深！goalId: {goalId}");
                return;
            }

            _setIsDoneGuard++;
            try
            {
                if (GameFlowManager.Instance?.PlayingData == null) return;

                var goalState = GameFlowManager.Instance.PlayingData.GetState<TaskGoalState>(goalId);
                if (goalState.isReached == value) return;

                goalState.isReached = value;
                if (goalState.isReached)
                {
                    Debug.Log($"Task Goal {goalId} completed!");
                    // 主动告知完成，驱动任务树跳转
                    TaskManager.Instance.OnGoalReached(this);
                }
            }
            finally
            {
                _setIsDoneGuard--;
            }
        }
    }

    private async UniTask<bool> CheckGoalAsync()
    {
        if (isChecking)
        {
            if (pendingCheck != null)
                return await pendingCheck.Task;
            return IsDone;
        }

        isChecking = true;
        pendingCheck = new UniTaskCompletionSource<bool>();

        try
        {
            // 自动完成：调试/Demo 用，跳过具体检测直接标记完成
            if (isAutoFinish)
            {
                IsDone = true;
                Debug.Log($"{goalId} 自动完成 (isAutoFinish)");
                return true;
            }

            switch (taskGoalType)
            {
                case TaskGoalType.TRIGGER:
                    CheckTrigger();
                    break;
                case TaskGoalType.ITEM:
                    CheckItem();
                    break;
                case TaskGoalType.ACTOR:
                    CheckActor();
                    break;
                case TaskGoalType.DIALOGUE:
                    CheckDialogue();
                    break;
                case TaskGoalType.SCRIPT:
                    await CheckScript();
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"CheckGoalAsync 执行异常 in {goalId}: {e}");
        }
        finally
        {
            isChecking = false;
        }

        // 使用 goalState.isReached 而不是 IsDone 属性，避免递归
        bool finalResult = false;
        if (GameFlowManager.Instance?.PlayingData != null)
        {
            var goalState = GameFlowManager.Instance.PlayingData.GetState<TaskGoalState>(goalId);
            finalResult = goalState.isReached;
        }

        pendingCheck.TrySetResult(finalResult);
        return finalResult;
    }

    private void CheckTrigger()
    {
        targetState = GameFlowManager.Instance?.PlayingData?.GetState<InteractionState>(targetId);
        if (targetState is InteractionState interactionState)
        {
            bool result = interactionState.isTriggered == GoalTrigger;
            Debug.Log($"{targetId} 触发检测: {(result ? "成功" : "失败")}");
            IsDone = result;
        }
        else
        {
            Debug.Log($"Target state not found: {targetId}");
            IsDone = false;
        }
    }

    private void CheckItem()
    {
        targetState = GameFlowManager.Instance?.PlayingData?.GetState<ItemState>(targetId);
        if (targetState is ItemState itemState)
        {
            bool result = itemState.isPicked == GoalItem;
            Debug.Log($"{targetId} 物品检测: {(result ? "成功" : "失败")}");
            IsDone = result;
        }
        else
        {
            Debug.Log($"Target state not found: {targetId}");
            IsDone = false;
        }
    }

    private void CheckActor()
    {
        if (isCheckPlayer)
        {
            GameObject player = TaskManager.Instance.mainPlayer;
            if (player != null)
            {
                bool positionCheck = CheckActorPosition(player.transform.position, player.transform.eulerAngles);
                bool sceneCheck = GameFlowManager.Instance.PlayingData.currentScene == sceneName;
                bool result = positionCheck && sceneCheck;

                Debug.Log($"玩家位置检测: {(result ? "成功" : "失败")}");
                IsDone = result;
            }
            else
            {
                Debug.Log("玩家未找到");
                IsDone = false;
            }
        }
        else
        {
            targetState = GameFlowManager.Instance?.PlayingData?.GetState<ActorState>(targetId);
            if (targetState is ActorState actorState &&
                actorState.position.HasValue &&
                actorState.rotation.HasValue)
            {
                bool positionCheck = CheckActorPosition(actorState.position.Value, actorState.rotation.Value);
                bool sceneCheck = actorState.scene == sceneName;
                bool result = positionCheck && sceneCheck;

                Debug.Log($"{targetId} 角色位置检测: {(result ? "成功" : "失败")}");
                IsDone = result;
            }
            else
            {
                Debug.Log($"Target state not found or missing position/rotation: {targetId}");
                IsDone = false;
            }
        }
    }

    private void CheckDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            bool result = DialogueManager.Instance.IsDialogueIndexCompleted(targetDialogueId, index);
            Debug.Log($"{targetDialogueId} {index} 对话检测: {(result ? "成功" : "失败")}");
            IsDone = result;
        }
        else
        {
            Debug.Log("DialogueManager 未找到");
            IsDone = false;
        }
    }

    private async UniTask CheckScript()
    {
        if (targetScript != null)
        {
            // 给脚本传入 goalId，使其完成时能正确修改存档
            targetScript.goalID = this.goalId;
            await targetScript.TaskIEnumerator();
            IsDone = targetScript.isDone;
        }
        else
        {
            Debug.Log("targetScript 为空");
            IsDone = false;
        }
    }

    private bool CheckActorPosition(Vector3 targetPosition, Vector3 targetRotation)
    {
        float positionDistance = Vector3.Distance(GoalPosition, targetPosition);
        float rotationAngle = Quaternion.Angle(Quaternion.Euler(GoalRotation), Quaternion.Euler(targetRotation));

        return positionDistance < positionTolerance && rotationAngle < rotationTolerance;
    }

    /// <summary>
    /// 异步获取是否完成（会等待检查完成）
    /// </summary>
    public async UniTask<bool> IsDoneAsync()
    {
        if (IsDone) return true;
        return await CheckGoalAsync();
    }
}

public enum TaskGoalType
{
    TRIGGER,
    ITEM,
    ACTOR,
    DIALOGUE,
    SCRIPT
}