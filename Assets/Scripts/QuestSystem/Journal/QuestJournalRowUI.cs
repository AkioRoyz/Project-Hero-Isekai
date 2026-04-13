using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestJournalRowUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI shortDescriptionText;
    [SerializeField] private Image selectedBackground;

    [Header("Active State")]
    [SerializeField] private float inactiveAlpha = 0.6f;

    public void SetData(string title, string shortDescription)
    {
        if (titleText != null)
        {
            titleText.text = title ?? string.Empty;
        }

        if (shortDescriptionText != null)
        {
            shortDescriptionText.text = shortDescription ?? string.Empty;
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedBackground != null)
        {
            selectedBackground.enabled = selected;
        }
    }

    public void SetListActiveVisual(bool isActive)
    {
        SetTextAlpha(titleText, isActive ? 1f : inactiveAlpha);
        SetTextAlpha(shortDescriptionText, isActive ? 1f : inactiveAlpha);

        if (!isActive && selectedBackground != null)
        {
            selectedBackground.enabled = false;
        }
    }

    private void SetTextAlpha(TextMeshProUGUI textComponent, float alpha)
    {
        if (textComponent == null)
            return;

        Color color = textComponent.color;
        color.a = alpha;
        textComponent.color = color;
    }
}