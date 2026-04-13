using UnityEngine.Localization;

public interface IDialogueSource
{
    // Имя источника диалога.
    // Например, имя NPC.
    LocalizedString GetDialogueSpeakerName();

    // Портрет источника диалога.
    // Например, портрет NPC.
    UnityEngine.Sprite GetDialoguePortrait();
}