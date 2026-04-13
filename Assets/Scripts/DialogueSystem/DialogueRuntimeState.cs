using System.Collections.Generic;
using UnityEngine;

public class DialogueRuntimeState : MonoBehaviour
{
    public static DialogueRuntimeState Instance;

    private readonly HashSet<string> playedKeys = new();
    private readonly HashSet<string> completedDialogues = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

    public void ClearAll()
    {
        playedKeys.Clear();
        completedDialogues.Clear();
    }
}