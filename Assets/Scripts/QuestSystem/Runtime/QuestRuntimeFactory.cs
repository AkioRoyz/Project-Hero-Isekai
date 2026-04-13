using UnityEngine;

public static class QuestRuntimeFactory
{
    public static QuestRuntimeData CreateFromData(QuestData questData)
    {
        if (questData == null)
        {
            Debug.LogWarning("QuestRuntimeFactory.CreateFromData called with null questData.");
            return null;
        }

        QuestRuntimeData runtimeData = new QuestRuntimeData(questData.QuestId);

        if (questData.Steps != null)
        {
            for (int i = 0; i < questData.Steps.Length; i++)
            {
                QuestStepData stepData = questData.Steps[i];
                QuestStepRuntimeData stepRuntime = new QuestStepRuntimeData(stepData.StepId);

                if (stepData.Objectives != null)
                {
                    for (int j = 0; j < stepData.Objectives.Length; j++)
                    {
                        QuestObjectiveData objectiveData = stepData.Objectives[j];

                        QuestObjectiveRuntimeData objectiveRuntime = new QuestObjectiveRuntimeData(
                            objectiveData.ObjectiveId,
                            objectiveData.ObjectiveType,
                            objectiveData.TargetId,
                            objectiveData.RequiredAmount
                        );

                        stepRuntime.Objectives.Add(objectiveRuntime);
                    }
                }

                runtimeData.Steps.Add(stepRuntime);
            }
        }

        if (runtimeData.Steps.Count > 0)
        {
            runtimeData.CurrentStepIndex = 0;
            runtimeData.Steps[0].StepState = QuestStepState.Active;
        }

        runtimeData.QuestState = QuestState.Active;
        return runtimeData;
    }
}