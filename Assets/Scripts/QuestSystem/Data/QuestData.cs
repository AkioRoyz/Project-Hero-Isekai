using System;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "QuestData_", menuName = "Game/Quests/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Base")]
    [SerializeField] private string questId;
    [SerializeField] private QuestType questType;
    [SerializeField] private QuestStartType startType = QuestStartType.Manual;
    [SerializeField] private QuestRepeatMode repeatMode = QuestRepeatMode.OneTime;

    [Header("Flags")]
    [SerializeField] private bool hiddenUntilStarted = false;
    [SerializeField] private bool canRestartAfterFail = true;
    [SerializeField] private bool notifyOnAccept = true;
    [SerializeField] private bool notifyOnComplete = true;

    [Header("Texts")]
    [SerializeField] private LocalizedString questTitle;
    [SerializeField] private LocalizedString shortDescription;
    [SerializeField] private LocalizedString fullDescription;

    [Header("Availability Conditions")]
    [Tooltip("≈сли больше 0, квест доступен только начина€ с этого уровн€.")]
    [SerializeField] private int requiredPlayerLevel = 0;

    [Tooltip("≈сли заполнено, квест доступен только если у игрока есть предмет с этим ItemId.")]
    [SerializeField] private string requiredItemId;

    [Tooltip("≈сли заполнено, квест доступен только после завершени€ квеста с этим QuestId.")]
    [SerializeField] private string requiredCompletedQuestId;

    [Header("Quest Flow")]
    [SerializeField] private QuestStepData[] steps = Array.Empty<QuestStepData>();

    public string QuestId => questId;
    public QuestType QuestType => questType;
    public QuestStartType StartType => startType;
    public QuestRepeatMode RepeatMode => repeatMode;

    public bool HiddenUntilStarted => hiddenUntilStarted;
    public bool CanRestartAfterFail => canRestartAfterFail;
    public bool NotifyOnAccept => notifyOnAccept;
    public bool NotifyOnComplete => notifyOnComplete;

    public LocalizedString QuestTitle => questTitle;
    public LocalizedString ShortDescription => shortDescription;
    public LocalizedString FullDescription => fullDescription;

    public int RequiredPlayerLevel => requiredPlayerLevel;
    public string RequiredItemId => requiredItemId;
    public string RequiredCompletedQuestId => requiredCompletedQuestId;

    public QuestStepData[] Steps => steps;
}