using System;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public class QuestObjectiveData
{
    [Header("Base")]
    [SerializeField] private string objectiveId;
    [SerializeField] private QuestObjectiveType objectiveType;
    [SerializeField] private LocalizedString objectiveDescription;

    [Header("Target Data")]
    [SerializeField] private string targetId;
    [SerializeField] private int requiredAmount = 1;

    public string ObjectiveId => objectiveId;
    public QuestObjectiveType ObjectiveType => objectiveType;
    public LocalizedString ObjectiveDescription => objectiveDescription;
    public string TargetId => targetId;
    public int RequiredAmount => requiredAmount;
}