using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public class DialogueNodeData
{
    [Header("Editor")]
    [Tooltip("Техническое удобное название ноды для инспектора. Используется только для удобства настройки.")]
    [SerializeField] private string inspectorNodeTitle = "Node";

    [Header("Node Settings")]
    [SerializeField] private DialogueNodeType nodeType = DialogueNodeType.Line;

    [Tooltip("Если включено, этот узел будет стартовым. В диалоге должен быть только один стартовый узел.")]
    [SerializeField] private bool isStartNode = false;

    [Header("Speaker")]
    [SerializeField] private DialogueSpeakerMode speakerMode = DialogueSpeakerMode.UseSourceSpeakerName;

    [Tooltip("Используется только если Speaker Mode = Use Custom Name.")]
    [SerializeField] private LocalizedString customSpeakerName;

    [SerializeField] private Sprite speakerPortrait;

    [Header("Text")]
    [SerializeField] private LocalizedString dialogueText;

    [Header("Line Transition")]
    [Tooltip("Следующий узел для обычной реплики. -1 = завершить диалог.")]
    [SerializeField] private int nextNodeIndex = -1;

    [Header("Choices")]
    [SerializeField] private List<DialogueChoiceData> choices = new();

    [Header("Conditions")]
    [SerializeField] private List<DialogueConditionData> conditions = new();

    [Header("On Enter Actions")]
    [SerializeField] private List<DialogueActionData> onEnterActions = new();

    public string InspectorNodeTitle => inspectorNodeTitle;

    public DialogueNodeType NodeType => nodeType;
    public bool IsStartNode => isStartNode;

    public DialogueSpeakerMode SpeakerMode => speakerMode;
    public LocalizedString CustomSpeakerName => customSpeakerName;
    public Sprite SpeakerPortrait => speakerPortrait;

    public LocalizedString DialogueText => dialogueText;
    public int NextNodeIndex => nextNodeIndex;

    public IReadOnlyList<DialogueChoiceData> Choices => choices;
    public IReadOnlyList<DialogueConditionData> Conditions => conditions;
    public IReadOnlyList<DialogueActionData> OnEnterActions => onEnterActions;
}