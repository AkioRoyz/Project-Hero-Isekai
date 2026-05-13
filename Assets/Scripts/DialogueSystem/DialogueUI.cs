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

    [System.Serializable]
    public class ChoiceSlot
    {
        [Header("Root")]
        [Tooltip("Корневой объект варианта ответа. Включается, когда этот вариант должен отображаться.")]
        public GameObject root;

        [Header("Text")]
        [Tooltip("Текст варианта ответа.")]
        public TMP_Text choiceText;

        [Header("Backgrounds")]
        [Tooltip("Фон обычного / невыбранного состояния.")]
        public Image unselectedBackgroundImage;

        [Tooltip("Фон выбранного состояния.")]
        public Image selectedBackgroundImage;

        [Header("Quest Marker")]
        [Tooltip("Image иконки квеста. Будет включаться только если у варианта ответа включён Show Quest Marker.")]
        public Image questMarkerImage;

        [Header("Custom Colors")]
        [Tooltip("Если включено, этот слот будет использовать свои цвета вместо общих цветов DialogueUI.")]
        public bool useCustomColors;

        public Color unselectedTextColor = Color.white;
        public Color selectedTextColor = Color.white;
        public Color disabledTextColor = Color.gray;

        public Color unselectedQuestMarkerColor = Color.white;
        public Color selectedQuestMarkerColor = Color.white;
        public Color disabledQuestMarkerColor = Color.gray;
    }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Pause Visual Effect")]
    [SerializeField] private PauseMenuBlackAndWhiteEffect blackAndWhiteEffect;
    [SerializeField] private bool usePauseVisualEffect = true;

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

    [Header("Dialogue Background UI")]
    [Tooltip("Image, который рисует фон окна диалога. Лучше использовать отдельный дочерний объект позади текста/портрета/вариантов ответа.")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("Стандартный фон диалога. Используется, если в текущей ноде не выбран свой фон.")]
    [SerializeField] private Sprite defaultBackgroundSprite;

    [Header("Portrait UI")]
    [SerializeField] private GameObject portraitRoot;
    [SerializeField] private Image portraitImage;

    [Header("Choice Slots")]
    [Tooltip("Слоты вариантов ответа. Каждый слот — это корневой объект ответа + текст + фон выбранного/невыбранного состояния + иконка квеста.")]
    [SerializeField] private List<ChoiceSlot> choiceSlots = new();

    [Header("Default Choice Colors")]
    [Tooltip("Цвет текста невыбранного варианта ответа.")]
    [SerializeField] private Color unselectedChoiceTextColor = Color.white;

    [Tooltip("Цвет текста выбранного варианта ответа.")]
    [SerializeField] private Color selectedChoiceTextColor = Color.white;

    [Tooltip("Цвет текста недоступного варианта ответа.")]
    [SerializeField] private Color disabledChoiceTextColor = Color.gray;

    [Tooltip("Цвет иконки квеста у невыбранного варианта ответа.")]
    [SerializeField] private Color unselectedQuestMarkerColor = Color.white;

    [Tooltip("Цвет иконки квеста у выбранного варианта ответа.")]
    [SerializeField] private Color selectedQuestMarkerColor = Color.white;

    [Tooltip("Цвет иконки квеста у недоступного варианта ответа.")]
    [SerializeField] private Color disabledQuestMarkerColor = Color.gray;

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
    private bool pauseVisualEffectEnabled;
    private Vector2 continueIndicatorStartAnchoredPosition;

    private void Awake()
    {
        ResolvePauseVisualEffect();

        if (continueIndicatorRect != null)
        {
            continueIndicatorStartAnchoredPosition = continueIndicatorRect.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        ResetContinueIndicatorPosition();
    }

    private void OnDisable()
    {
        DisablePauseVisualEffect();
    }

    private void Update()
    {
        UpdateContinueIndicatorAnimation();
    }

    public void Show()
    {
        EnablePauseVisualEffect();

        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        DisablePauseVisualEffect();

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
        SetDialogueBackground(null);
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

    public void SetDialogueBackground(Sprite nodeBackground)
    {
        if (backgroundImage == null)
            return;

        Sprite finalBackground = nodeBackground != null ? nodeBackground : defaultBackgroundSprite;
        bool hasBackground = finalBackground != null;

        backgroundImage.sprite = finalBackground;
        backgroundImage.enabled = hasBackground;
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
        for (int i = 0; i < choiceSlots.Count; i++)
        {
            ClearChoiceSlot(choiceSlots[i]);
        }
    }

    public void SetChoices(List<ChoiceViewData> choices, int selectedIndex)
    {
        ClearChoices();

        if (choices == null)
            return;

        int count = Mathf.Min(choiceSlots.Count, choices.Count);

        for (int i = 0; i < count; i++)
        {
            ChoiceSlot slot = choiceSlots[i];

            if (slot == null)
                continue;

            ChoiceViewData data = choices[i];

            bool isSelected = data.IsSelectable && i == selectedIndex;
            bool isDisabled = !data.IsSelectable;

            SetChoiceSlotVisible(slot, true);
            SetChoiceText(slot, data.Text);
            SetChoiceBackground(slot, isSelected);
            SetChoiceTextColor(slot, isSelected, isDisabled);
            SetQuestMarker(slot, data.ShowQuestMarker, isSelected, isDisabled);
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

    private void ResolvePauseVisualEffect()
    {
        if (!usePauseVisualEffect || blackAndWhiteEffect != null)
            return;

        PauseMenuBlackAndWhiteEffect[] effects = FindObjectsByType<PauseMenuBlackAndWhiteEffect>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (effects != null && effects.Length > 0)
            blackAndWhiteEffect = effects[0];
    }

    private void EnablePauseVisualEffect()
    {
        if (!usePauseVisualEffect || pauseVisualEffectEnabled)
            return;

        ResolvePauseVisualEffect();

        if (blackAndWhiteEffect == null)
            return;

        blackAndWhiteEffect.EnablePauseEffect();
        pauseVisualEffectEnabled = true;
    }

    private void DisablePauseVisualEffect()
    {
        if (!pauseVisualEffectEnabled)
            return;

        if (blackAndWhiteEffect != null)
            blackAndWhiteEffect.DisablePauseEffect();

        pauseVisualEffectEnabled = false;
    }

    private void ClearChoiceSlot(ChoiceSlot slot)
    {
        if (slot == null)
            return;

        SetChoiceSlotVisible(slot, false);

        if (slot.choiceText != null)
        {
            slot.choiceText.text = string.Empty;
            slot.choiceText.color = GetUnselectedTextColor(slot);
        }

        SetImageObjectActive(slot.unselectedBackgroundImage, false);
        SetImageObjectActive(slot.selectedBackgroundImage, false);
        SetImageObjectActive(slot.questMarkerImage, false);
    }

    private void SetChoiceSlotVisible(ChoiceSlot slot, bool isVisible)
    {
        if (slot == null)
            return;

        if (slot.root != null)
        {
            slot.root.SetActive(isVisible);
            return;
        }

        if (slot.choiceText != null)
        {
            slot.choiceText.gameObject.SetActive(isVisible);
        }
    }

    private void SetChoiceText(ChoiceSlot slot, string text)
    {
        if (slot == null || slot.choiceText == null)
            return;

        slot.choiceText.text = text ?? string.Empty;
    }

    private void SetChoiceBackground(ChoiceSlot slot, bool isSelected)
    {
        if (slot == null)
            return;

        SetImageObjectActive(slot.unselectedBackgroundImage, !isSelected);
        SetImageObjectActive(slot.selectedBackgroundImage, isSelected);
    }

    private void SetChoiceTextColor(ChoiceSlot slot, bool isSelected, bool isDisabled)
    {
        if (slot == null || slot.choiceText == null)
            return;

        if (isDisabled)
        {
            slot.choiceText.color = GetDisabledTextColor(slot);
            return;
        }

        slot.choiceText.color = isSelected
            ? GetSelectedTextColor(slot)
            : GetUnselectedTextColor(slot);
    }

    private void SetQuestMarker(ChoiceSlot slot, bool showQuestMarker, bool isSelected, bool isDisabled)
    {
        if (slot == null || slot.questMarkerImage == null)
            return;

        SetImageObjectActive(slot.questMarkerImage, showQuestMarker);

        if (!showQuestMarker)
            return;

        if (isDisabled)
        {
            slot.questMarkerImage.color = GetDisabledQuestMarkerColor(slot);
            return;
        }

        slot.questMarkerImage.color = isSelected
            ? GetSelectedQuestMarkerColor(slot)
            : GetUnselectedQuestMarkerColor(slot);
    }

    private void SetImageObjectActive(Image image, bool isActive)
    {
        if (image == null)
            return;

        image.gameObject.SetActive(isActive);
    }

    private Color GetUnselectedTextColor(ChoiceSlot slot)
    {
        if (slot != null && slot.useCustomColors)
            return slot.unselectedTextColor;

        return unselectedChoiceTextColor;
    }

    private Color GetSelectedTextColor(ChoiceSlot slot)
    {
        if (slot != null && slot.useCustomColors)
            return slot.selectedTextColor;

        return selectedChoiceTextColor;
    }

    private Color GetDisabledTextColor(ChoiceSlot slot)
    {
        if (slot != null && slot.useCustomColors)
            return slot.disabledTextColor;

        return disabledChoiceTextColor;
    }

    private Color GetUnselectedQuestMarkerColor(ChoiceSlot slot)
    {
        if (slot != null && slot.useCustomColors)
            return slot.unselectedQuestMarkerColor;

        return unselectedQuestMarkerColor;
    }

    private Color GetSelectedQuestMarkerColor(ChoiceSlot slot)
    {
        if (slot != null && slot.useCustomColors)
            return slot.selectedQuestMarkerColor;

        return selectedQuestMarkerColor;
    }

    private Color GetDisabledQuestMarkerColor(ChoiceSlot slot)
    {
        if (slot != null && slot.useCustomColors)
            return slot.disabledQuestMarkerColor;

        return disabledQuestMarkerColor;
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