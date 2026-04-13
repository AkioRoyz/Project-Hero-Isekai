public interface IDialogueActionQuestHandler
{
    bool HandleQuestAction(string questId, QuestActionType actionType);

    // Засчитать конкретную задачу квеста.
    // Обычно используется для ответа в диалоге, который подтверждает разговор с NPC.
    bool AcceptQuestObjective(string questId, string objectiveId, int amount = 1);
}