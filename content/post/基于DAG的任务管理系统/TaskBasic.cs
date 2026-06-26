using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class TaskBasic : MonoBehaviour
{
    [HideInInspector] public string goalID;

    public bool isDone
    {
        get
        {
            return GameFlowManager.Instance.PlayingData.GetState<TaskGoalState>(goalID).isReached;
        }
        set
        {
            var state = GameFlowManager.Instance.PlayingData.GetState<TaskGoalState>(goalID);
            state.isReached = value;
        }
    }

    private UniTaskCompletionSource _taskSignal;
    private bool isTaskRunning = false;
    public async UniTask TaskIEnumerator()
    {
        if (isDone) { return; }   // 提前完成
        if (isTaskRunning) { return; } // 防止重复调用

        isTaskRunning = true;

        _taskSignal = new UniTaskCompletionSource();
        await OnTaskStart(); // 开始演出 
        await _taskSignal.Task; // 卡住，等信号
        await UniTask.WaitUntil(() => !UIManager.Instance.UISys());  // 确保不是菜单或 Start
        OnTaskEnd();

        isTaskRunning = false;
        isDone = true;
    }

    protected virtual UniTask OnTaskStart() { return UniTask.CompletedTask; }
    protected virtual void OnTaskEnd() { }


    /// <summary>
    /// OnTaskStart 最后调用或外部调用，发出完成 Task 的信号
    /// </summary>
    [ContextMenu("Finish Task")]
    public void FinishTask()
    {
        _taskSignal?.TrySetResult();
    }
}