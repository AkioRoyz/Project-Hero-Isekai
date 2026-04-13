using System;
using System.Collections.Generic;

[Serializable]
public class QuestStepRuntimeData
{
    public string StepId;
    public QuestStepState StepState;

    public List<QuestObjectiveRuntimeData> Objectives = new();

    public QuestStepRuntimeData(string stepId)
    {
        StepId = stepId;
        StepState = QuestStepState.Inactive;
    }

    public bool AreAllObjectivesCompleted()
    {
        if (Objectives == null || Objectives.Count == 0)
            return true;

        for (int i = 0; i < Objectives.Count; i++)
        {
            if (!Objectives[i].IsCompleted)
                return false;
        }

        return true;
    }

    public bool HasAnyFailedObjective()
    {
        if (Objectives == null || Objectives.Count == 0)
            return false;

        for (int i = 0; i < Objectives.Count; i++)
        {
            if (Objectives[i].IsFailed)
                return true;
        }

        return false;
    }
}