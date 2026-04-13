using System;

[Serializable]
public class QuestObjectiveRuntimeData
{
    public string ObjectiveId;
    public QuestObjectiveType ObjectiveType;
    public string TargetId;

    public int CurrentAmount;
    public int RequiredAmount;

    public bool IsCompleted;
    public bool IsFailed;

    public QuestObjectiveRuntimeData(
        string objectiveId,
        QuestObjectiveType objectiveType,
        string targetId,
        int requiredAmount)
    {
        ObjectiveId = objectiveId;
        ObjectiveType = objectiveType;
        TargetId = targetId;
        RequiredAmount = Math.Max(1, requiredAmount);

        CurrentAmount = 0;
        IsCompleted = false;
        IsFailed = false;
    }
}