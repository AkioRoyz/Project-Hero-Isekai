using System;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public class QuestStepData
{
    [Header("Base")]
    [SerializeField] private string stepId;

    [Header("UI Text")]
    [SerializeField] private LocalizedString stepDescription;
    [SerializeField] private LocalizedString stepTaskText;

    [Header("Objectives")]
    [SerializeField] private QuestObjectiveData[] objectives = Array.Empty<QuestObjectiveData>();

    public string StepId => stepId;
    public LocalizedString StepDescription => stepDescription;
    public LocalizedString StepTaskText => stepTaskText;
    public QuestObjectiveData[] Objectives => objectives;
}