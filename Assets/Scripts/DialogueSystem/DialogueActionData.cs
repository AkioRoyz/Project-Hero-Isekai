using System;
using UnityEngine;

[Serializable]
public class DialogueActionData
{
    [SerializeField] private DialogueActionType actionType = DialogueActionType.None;

    [Header("Reward Action")]
    [SerializeField] private RewardData rewardData;

    [Header("Remove Item Action")]
    [SerializeField] private ItemData itemToRemove;
    [SerializeField] private int itemRemoveAmount = 1;

    [Header("Mark Played Action")]
    [Tooltip("Уникальный ключ. Например: villager_intro_completed")]
    [SerializeField] private string playedKey;

    [Header("Quest Action")]
    [SerializeField] private string questId;
    [SerializeField] private QuestActionType questActionType = QuestActionType.None;

    [Header("Accept Quest Objective Action")]
    [Tooltip("ID конкретной задачи квеста, которую нужно засчитать. Обычно это задача типа TalkToNpc.")]
    [SerializeField] private string questObjectiveId;

    [Tooltip("На сколько увеличить прогресс задачи. Обычно оставляем 1.")]
    [SerializeField] private int questObjectiveAmount = 1;

    public DialogueActionType ActionType => actionType;
    public RewardData RewardData => rewardData;

    public ItemData ItemToRemove => itemToRemove;
    public int ItemRemoveAmount => itemRemoveAmount;

    public string PlayedKey => playedKey;

    public string QuestId => questId;
    public QuestActionType QuestActionType => questActionType;

    public string QuestObjectiveId => questObjectiveId;
    public int QuestObjectiveAmount => questObjectiveAmount;
}