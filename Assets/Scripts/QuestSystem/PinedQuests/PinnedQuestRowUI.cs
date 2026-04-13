using TMPro;
using UnityEngine;

public class PinnedQuestRowUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questTasksText;

    public void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void SetData(string title, string tasksText, Color questColor)
    {
        if (questTitleText != null)
        {
            questTitleText.text = title ?? string.Empty;
            questTitleText.color = questColor;
        }

        if (questTasksText != null)
        {
            questTasksText.text = tasksText ?? string.Empty;
            questTasksText.color = questColor;
        }
    }
}