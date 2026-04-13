using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public class DialogueChoiceData
{
    [Header("Editor")]
    [Tooltip("Техническое удобное название ответа для инспектора. Используется только для удобства настройки.")]
    [SerializeField] private string inspectorChoiceTitle = "Choice";

    [Header("Quest Visuals")]
    [Tooltip("Если включено, ответ считается квестовым для визуального оформления.")]
    [SerializeField] private bool isQuestRelated = false;

    [Tooltip("Если включено, у ответа показывается квестовая иконка в UI. Работает только если Is Quest Related включён.")]
    [SerializeField] private bool showQuestMarker = true;

    [Header("Choice Text")]
    [SerializeField] private LocalizedString choiceText;

    [Header("Next Node")]
    [Tooltip("Индекс узла, в который перейдёт диалог после выбора этого ответа. -1 = завершить диалог.")]
    [SerializeField] private int nextNodeIndex = -1;

    [Header("Conditions")]
    [SerializeField] private List<DialogueConditionData> conditions = new();

    [Header("Unavailable Behaviour")]
    [SerializeField] private DialogueChoiceUnavailableMode unavailableMode = DialogueChoiceUnavailableMode.Hide;

    [Tooltip("Текст, который показывается, если ответ недоступен и выбран режим ShowDisabled. Если не задан, будет показан обычный текст ответа.")]
    [SerializeField] private LocalizedString disabledChoiceText;

    [Header("On Select Actions")]
    [SerializeField] private List<DialogueActionData> onSelectActions = new();

    public string InspectorChoiceTitle => inspectorChoiceTitle;
    public bool IsQuestRelated => isQuestRelated;
    public bool ShowQuestMarker => isQuestRelated && showQuestMarker;

    public LocalizedString ChoiceText => choiceText;
    public int NextNodeIndex => nextNodeIndex;
    public IReadOnlyList<DialogueConditionData> Conditions => conditions;
    public DialogueChoiceUnavailableMode UnavailableMode => unavailableMode;
    public LocalizedString DisabledChoiceText => disabledChoiceText;
    public IReadOnlyList<DialogueActionData> OnSelectActions => onSelectActions;
}