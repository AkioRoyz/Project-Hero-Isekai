public interface IDialogueQuestProvider
{
    bool IsQuestStateMatched(string questId, QuestState requiredQuestState);
    bool IsQuestStepIdMatched(string questId, string requiredQuestStepId);
}