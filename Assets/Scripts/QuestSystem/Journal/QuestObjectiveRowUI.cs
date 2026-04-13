using TMPro;
using UnityEngine;

public class QuestObjectiveRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private float completedAlpha = 0.45f;
    [SerializeField] private string completedPrefix = "• ";
    [SerializeField] private string activePrefix = "• ";

    public void SetData(string text, bool isCompleted)
    {
        if (objectiveText == null)
            return;

        objectiveText.text = (isCompleted ? completedPrefix : activePrefix) + (text ?? string.Empty);

        Color color = objectiveText.color;
        color.a = isCompleted ? completedAlpha : 1f;
        objectiveText.color = color;
    }
}