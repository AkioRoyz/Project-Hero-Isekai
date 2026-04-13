using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue_", menuName = "Game/Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Dialogue Settings")]
    [SerializeField] private string dialogueId;

    [Tooltip("≈сли true, этот диалог можно запускать много раз.")]
    [SerializeField] private bool repeatable = true;

    [Header("Conditions")]
    [SerializeField] private List<DialogueConditionData> conditions = new();

    [Header("Nodes")]
    [SerializeField] private List<DialogueNodeData> nodes = new();

    public string DialogueId => dialogueId;
    public bool Repeatable => repeatable;
    public IReadOnlyList<DialogueConditionData> Conditions => conditions;
    public IReadOnlyList<DialogueNodeData> Nodes => nodes;

    public DialogueNodeData GetNode(int index)
    {
        if (index < 0 || index >= nodes.Count)
        {
            Debug.LogWarning($"DialogueData: invalid node index {index} in dialogue {name}");
            return null;
        }

        return nodes[index];
    }

    public int GetStartNodeIndex()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].IsStartNode)
                return i;
        }

        Debug.LogWarning($"DialogueData: start node was not found in dialogue {name}");
        return -1;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateDialogue();
    }
#endif

    public void ValidateDialogue()
    {
        if (nodes == null || nodes.Count == 0)
            return;

        int startNodeCount = 0;

        for (int i = 0; i < nodes.Count; i++)
        {
            DialogueNodeData node = nodes[i];
            if (node == null)
                continue;

            if (node.IsStartNode)
                startNodeCount++;

            ValidateNode(i, node);
        }

        if (startNodeCount == 0)
        {
            Debug.LogWarning($"Dialogue '{name}' has no start node.");
        }
        else if (startNodeCount > 1)
        {
            Debug.LogWarning($"Dialogue '{name}' has more than one start node.");
        }

        if (!repeatable && string.IsNullOrWhiteSpace(dialogueId))
        {
            Debug.LogWarning($"Dialogue '{name}' is non-repeatable but DialogueId is empty.");
        }
    }

    private void ValidateNode(int nodeIndex, DialogueNodeData node)
    {
        if (node.NodeType == DialogueNodeType.Line)
        {
            if (node.NextNodeIndex >= nodes.Count)
            {
                Debug.LogWarning($"Dialogue '{name}' line node [{nodeIndex}] has invalid NextNodeIndex = {node.NextNodeIndex}");
            }
        }
        else if (node.NodeType == DialogueNodeType.Choice)
        {
            if (node.Choices == null || node.Choices.Count == 0)
            {
                Debug.LogWarning($"Dialogue '{name}' choice node [{nodeIndex}] has no choices.");
                return;
            }

            for (int i = 0; i < node.Choices.Count; i++)
            {
                DialogueChoiceData choice = node.Choices[i];
                if (choice == null)
                    continue;

                if (choice.NextNodeIndex >= nodes.Count)
                {
                    Debug.LogWarning($"Dialogue '{name}' choice node [{nodeIndex}] choice [{i}] has invalid NextNodeIndex = {choice.NextNodeIndex}");
                }
            }
        }
    }
}