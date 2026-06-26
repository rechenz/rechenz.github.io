using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TargetDialogue
{
    public string dialogueguid;
    public int dialogueIndex;
}

[System.Serializable]
public class TaskEffect
{
    public EffectType effectType;
    public string targetGUID;
    [HideInInspector] public BaseState state;
    public InteractionState targetInteractionState;
    public ItemState targetItemState;
    public ActorState targetActorState;

    public bool isChangeActorScene;
    public string currentScene;
    public string targetActorSceneName;

    public List<TargetDialogue> targetDialogueList;


    private BaseState snapShotState;
    public void ApplyEffect()
    {
        switch (effectType)
        {
            case EffectType.INTERACTABLESTATE:
                state = GameFlowManager.Instance.PlayingData.GetState<InteractionState>(targetGUID);

                snapShotState = state.Clone();
                state.Copyfrom(targetInteractionState);
                break;
            case EffectType.ITEMSTATE:
                state = GameFlowManager.Instance.PlayingData.GetState<ItemState>(targetGUID);

                snapShotState = state.Clone();
                state.Copyfrom(targetItemState);
                break;
            case EffectType.ACTORSTATE:
                ActorState actorState = GameFlowManager.Instance.PlayingData.GetState<ActorState>(targetGUID);
                state = actorState;
                if (isChangeActorScene)
                {
                    if (string.IsNullOrEmpty(currentScene))
                    {
                        Debug.Log("currentScene is null");
                    }
                    if (string.IsNullOrEmpty(targetActorSceneName))
                    {
                        Debug.Log("targetActorSceneName is null");
                    }
                    if (!string.IsNullOrEmpty(currentScene) && !string.IsNullOrEmpty(targetActorSceneName))
                    {
                        GameFlowManager.Instance.PlayingData.GetState<EntitySceneState>(currentScene).UnregisterPoolEntity(actorState.guid);
                        GameFlowManager.Instance.PlayingData.GetState<EntitySceneState>(targetActorSceneName).RegisterPoolEntity(actorState.guid);
                    }
                }

                snapShotState = state.Clone();
                state.Copyfrom(targetActorState);
                break;
            case EffectType.DIALOGUESTATE:
                foreach (var dialogue in targetDialogueList)
                {
                    DialogueManager.Instance.SetDialogueIndex(dialogue.dialogueguid, dialogue.dialogueIndex);
                }
                break;
            case EffectType.CALLMETHOD:
                if (targetGUID == "FountainRipple")
                {
                    if (FountainWaterRipple.Instance != null)
                        FountainWaterRipple.Instance.CreateRipple();
                }
                break;

        }

        // 刷新此物体在当前场景状态
        state?.ScenedNotifyChanged();
    }

    public void RevertEffect()
    {
        if (snapShotState == null) return;

        switch (effectType)
        {
            case EffectType.INTERACTABLESTATE:
                state = GameFlowManager.Instance.PlayingData.GetState<InteractionState>(targetGUID);
                state.Copyfrom(snapShotState as InteractionState);
                break;
            case EffectType.ITEMSTATE:
                state = GameFlowManager.Instance.PlayingData.GetState<ItemState>(targetGUID);
                state.Copyfrom(snapShotState as ItemState);
                break;
            case EffectType.ACTORSTATE:
                state = GameFlowManager.Instance.PlayingData.GetState<ActorState>(targetGUID);
                state.Copyfrom(snapShotState as ActorState);
                break;
        }

        // 刷新此物体在当前场景状态
        state?.ScenedNotifyChanged();
    }
}
public enum EffectType
{
    INTERACTABLESTATE,
    ITEMSTATE,
    ACTORSTATE,
    DIALOGUESTATE,
    CALLMETHOD
}