using System.Collections.Generic;
using UnityEngine;

public class DialogueRuntimeState : MonoBehaviour
{
    public static DialogueRuntimeState Instance;

    private readonly HashSet<string> playedKeys = new HashSet<string>();
    private readonly HashSet<string> completedDialogues = new HashSet<string>();

    public IReadOnlyCollection<string> PlayedKeys => playedKeys;
    public IReadOnlyCollection<string> CompletedDialogues => completedDialogues;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool HasPlayed(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return playedKeys.Contains(key);
    }

    public void MarkPlayed(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        playedKeys.Add(key);
    }

    public bool IsDialogueCompleted(string dialogueId)
    {
        if (string.IsNullOrWhiteSpace(dialogueId))
            return false;

        return completedDialogues.Contains(dialogueId);
    }

    public void MarkDialogueCompleted(string dialogueId)
    {
        if (string.IsNullOrWhiteSpace(dialogueId))
            return;

        completedDialogues.Add(dialogueId);
    }

    public DialogueStateSaveData CaptureState()
    {
        DialogueStateSaveData data = new DialogueStateSaveData();

        foreach (string key in playedKeys)
            data.playedKeys.Add(key);

        foreach (string dialogueId in completedDialogues)
            data.completedDialogueIds.Add(dialogueId);

        return data;
    }

    public void RestoreState(DialogueStateSaveData saveData)
    {
        ClearAll();

        if (saveData == null)
            return;

        if (saveData.playedKeys != null)
        {
            for (int i = 0; i < saveData.playedKeys.Count; i++)
            {
                string key = saveData.playedKeys[i];
                if (!string.IsNullOrWhiteSpace(key))
                    playedKeys.Add(key);
            }
        }

        if (saveData.completedDialogueIds != null)
        {
            for (int i = 0; i < saveData.completedDialogueIds.Count; i++)
            {
                string dialogueId = saveData.completedDialogueIds[i];
                if (!string.IsNullOrWhiteSpace(dialogueId))
                    completedDialogues.Add(dialogueId);
            }
        }
    }

    public void ClearAll()
    {
        playedKeys.Clear();
        completedDialogues.Clear();
    }
}