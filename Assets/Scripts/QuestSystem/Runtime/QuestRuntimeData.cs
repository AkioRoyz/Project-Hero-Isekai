using System;
using System.Collections.Generic;

[Serializable]
public class QuestRuntimeData
{
    public string QuestId;
    public QuestState QuestState;

    public int CurrentStepIndex;
    public bool IsPinned;

    public List<QuestStepRuntimeData> Steps = new();

    public QuestRuntimeData(string questId)
    {
        QuestId = questId;
        QuestState = QuestState.NotStarted;
        CurrentStepIndex = -1;
        IsPinned = false;
    }

    public QuestStepRuntimeData GetCurrentStep()
    {
        if (CurrentStepIndex < 0 || CurrentStepIndex >= Steps.Count)
            return null;

        return Steps[CurrentStepIndex];
    }

    public QuestStepRuntimeData GetStepById(string stepId)
    {
        if (string.IsNullOrEmpty(stepId))
            return null;

        for (int i = 0; i < Steps.Count; i++)
        {
            if (Steps[i].StepId == stepId)
                return Steps[i];
        }

        return null;
    }

    public QuestObjectiveRuntimeData GetObjectiveById(string objectiveId)
    {
        if (string.IsNullOrEmpty(objectiveId))
            return null;

        for (int i = 0; i < Steps.Count; i++)
        {
            QuestStepRuntimeData step = Steps[i];

            if (step.Objectives == null)
                continue;

            for (int j = 0; j < step.Objectives.Count; j++)
            {
                if (step.Objectives[j].ObjectiveId == objectiveId)
                    return step.Objectives[j];
            }
        }

        return null;
    }
}