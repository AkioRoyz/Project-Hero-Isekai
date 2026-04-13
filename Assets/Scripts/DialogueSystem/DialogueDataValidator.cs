#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DialogueDataValidator
{
    [MenuItem("Tools/Dialogue/Validate All Dialogue Data")]
    public static void ValidateAllDialogueData()
    {
        string[] guids = AssetDatabase.FindAssets("t:DialogueData");

        int validatedCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            DialogueData dialogueData = AssetDatabase.LoadAssetAtPath<DialogueData>(path);

            if (dialogueData == null)
                continue;

            dialogueData.ValidateDialogue();
            validatedCount++;
        }

        Debug.Log($"Dialogue validation complete. Checked {validatedCount} dialogue assets.");
    }
}
#endif