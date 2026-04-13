using UnityEngine;
using UnityEngine.Localization;

public class DialogueContext
{
    public IDialogueSource Source { get; private set; }
    public IDialogueQuestProvider QuestProvider { get; private set; }
    public IDialogueActionQuestHandler QuestActionHandler { get; private set; }

    private int playerLevel;

    public DialogueContext(
        IDialogueSource source,
        int playerLevel,
        IDialogueQuestProvider questProvider = null,
        IDialogueActionQuestHandler questActionHandler = null)
    {
        Source = source;
        this.playerLevel = playerLevel;
        QuestProvider = questProvider;
        QuestActionHandler = questActionHandler;
    }

    public int GetPlayerLevel()
    {
        return playerLevel;
    }

    public LocalizedString GetSourceSpeakerName()
    {
        if (Source == null)
            return null;

        return Source.GetDialogueSpeakerName();
    }

    public Sprite GetSourcePortrait()
    {
        if (Source == null)
            return null;

        return Source.GetDialoguePortrait();
    }
}