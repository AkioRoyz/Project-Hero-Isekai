using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [System.Serializable]
    public class ChoiceViewData
    {
        public string Text;
        public bool IsSelectable;
        public bool IsQuestRelated;
        public bool ShowQuestMarker;

        public ChoiceViewData(string text, bool isSelectable, bool isQuestRelated, bool showQuestMarker)
        {
            Text = text;
            IsSelectable = isSelectable;
            IsQuestRelated = isQuestRelated;
            ShowQuestMarker = showQuestMarker;
        }
    }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Content Root")]
    [Tooltip("Основной контейнер содержимого диалога.")]
    [SerializeField] private GameObject contentRoot;

    [Header("Optional Loading Object")]
    [Tooltip("Необязательный объект, который можно показывать во время первой загрузки локализованных строк.")]
    [SerializeField] private GameObject loadingObject;

    [Header("Speaker UI")]
    [SerializeField] private GameObject speakerNameRoot;
    [SerializeField] private TMP_Text speakerNameText;

    [Header("Dialogue Text UI")]
    [SerializeField] private TMP_Text dialogueText;

    [Header("Portrait UI")]
    [SerializeField] private GameObject portraitRoot;
    [SerializeField] private Image portraitImage;

    [Header("Choices")]
    [SerializeField] private List<TMP_Text> choiceTexts = new();

    [Tooltip("Необязательные объекты-иконки для тех же строк выбора. Порядок должен совпадать с choiceTexts.")]
    [SerializeField] private List<GameObject> choiceQuestMarkerObjects = new();

    [Header("Selection Prefixes")]
    [SerializeField] private string selectedPrefix = "> ";
    [SerializeField] private string unselectedPrefix = "  ";
    [SerializeField] private string disabledPrefix = "X ";

    [Header("Choice Colors")]
    [SerializeField] private bool tintDisabledChoices = true;
    [SerializeField] private bool tintQuestChoices = true;
    [SerializeField] private Color normalChoiceColor = Color.white;
    [SerializeField] private Color disabledChoiceColor = Color.gray;
    [SerializeField] private Color questChoiceColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Continue Indicator")]
    [Tooltip("Иконка, которая показывается только на обычных репликах без выбора.")]
    [SerializeField] private GameObject continueIndicatorObject;

    [Tooltip("RectTransform иконки. Нужен для плавного движения вверх-вниз.")]
    [SerializeField] private RectTransform continueIndicatorRect;

    [Tooltip("Насколько пикселей иконка поднимается вверх.")]
    [SerializeField] private float continueIndicatorMoveDistance = 10f;

    [Tooltip("Скорость движения иконки.")]
    [SerializeField] private float continueIndicatorMoveSpeed = 2f;

    private bool isContinueIndicatorVisible;
    private Vector2 continueIndicatorStartAnchoredPosition;

    private void Awake()
    {
        if (continueIndicatorRect != null)
        {
            continueIndicatorStartAnchoredPosition = continueIndicatorRect.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        ResetContinueIndicatorPosition();
    }

    private void Update()
    {
        UpdateContinueIndicatorAnimation();
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        HideContinueIndicator();
        HideLoadingState();
        ClearAllVisuals();
    }

    public void ShowLoadingState()
    {
        if (contentRoot != null)
        {
            contentRoot.SetActive(false);
        }

        if (loadingObject != null)
        {
            loadingObject.SetActive(true);
        }

        HideContinueIndicator();
        ClearAllVisuals();
    }

    public void HideLoadingState()
    {
        if (loadingObject != null)
        {
            loadingObject.SetActive(false);
        }

        if (contentRoot != null)
        {
            contentRoot.SetActive(true);
        }
    }

    public void ClearAllVisuals()
    {
        SetSpeakerName(string.Empty);
        SetDialogueText(string.Empty);
        SetPortrait(null);
        ClearChoices();
    }

    public void SetSpeakerName(string speakerName)
    {
        string finalName = speakerName ?? string.Empty;

        if (speakerNameText != null)
            speakerNameText.text = finalName;

        if (speakerNameRoot != null)
            speakerNameRoot.SetActive(!string.IsNullOrWhiteSpace(finalName));
    }

    public void SetDialogueText(string text)
    {
        if (dialogueText != null)
            dialogueText.text = text ?? string.Empty;
    }

    public void SetPortrait(Sprite portrait)
    {
        bool hasPortrait = portrait != null;

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = hasPortrait;
        }

        if (portraitRoot != null)
        {
            portraitRoot.SetActive(hasPortrait);
        }
    }

    public void ClearChoices()
    {
        for (int i = 0; i < choiceTexts.Count; i++)
        {
            if (choiceTexts[i] != null)
            {
                choiceTexts[i].gameObject.SetActive(false);
                choiceTexts[i].text = string.Empty;
                choiceTexts[i].color = normalChoiceColor;
            }

            if (i < choiceQuestMarkerObjects.Count && choiceQuestMarkerObjects[i] != null)
            {
                choiceQuestMarkerObjects[i].SetActive(false);
            }
        }
    }

    public void SetChoices(List<ChoiceViewData> choices, int selectedIndex)
    {
        ClearChoices();

        if (choices == null)
            return;

        for (int i = 0; i < choiceTexts.Count; i++)
        {
            if (i >= choices.Count)
                break;

            TMP_Text choiceText = choiceTexts[i];
            if (choiceText == null)
                continue;

            choiceText.gameObject.SetActive(true);

            ChoiceViewData data = choices[i];

            bool isSelected = i == selectedIndex;
            string prefix;

            if (!data.IsSelectable)
            {
                prefix = disabledPrefix;
            }
            else
            {
                prefix = isSelected ? selectedPrefix : unselectedPrefix;
            }

            choiceText.text = prefix + (data.Text ?? string.Empty);
            choiceText.color = GetChoiceColor(data);

            if (i < choiceQuestMarkerObjects.Count && choiceQuestMarkerObjects[i] != null)
            {
                choiceQuestMarkerObjects[i].SetActive(data.ShowQuestMarker);
            }
        }
    }

    public void ShowContinueIndicator()
    {
        isContinueIndicatorVisible = true;

        if (continueIndicatorObject != null)
        {
            continueIndicatorObject.SetActive(true);
        }

        ResetContinueIndicatorPosition();
    }

    public void HideContinueIndicator()
    {
        isContinueIndicatorVisible = false;

        if (continueIndicatorObject != null)
        {
            continueIndicatorObject.SetActive(false);
        }

        ResetContinueIndicatorPosition();
    }

    private Color GetChoiceColor(ChoiceViewData data)
    {
        if (data == null)
            return normalChoiceColor;

        if (!data.IsSelectable && tintDisabledChoices)
            return disabledChoiceColor;

        if (data.IsQuestRelated && tintQuestChoices)
            return questChoiceColor;

        return normalChoiceColor;
    }

    private void UpdateContinueIndicatorAnimation()
    {
        if (!isContinueIndicatorVisible)
            return;

        if (continueIndicatorRect == null)
            return;

        float offsetY = Mathf.Sin(Time.unscaledTime * continueIndicatorMoveSpeed) * continueIndicatorMoveDistance;
        continueIndicatorRect.anchoredPosition = continueIndicatorStartAnchoredPosition + new Vector2(0f, offsetY);
    }

    private void ResetContinueIndicatorPosition()
    {
        if (continueIndicatorRect == null)
            return;

        continueIndicatorRect.anchoredPosition = continueIndicatorStartAnchoredPosition;
    }
}