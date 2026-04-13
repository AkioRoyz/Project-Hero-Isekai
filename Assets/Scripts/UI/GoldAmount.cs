using TMPro;
using UnityEngine;

public class GoldAmount : MonoBehaviour
{
    [SerializeField] private GoldSystem goldSystem;
    [SerializeField] private TextMeshProUGUI goldText;

    private void OnEnable()
    {
        if (goldSystem != null)
        {
            goldSystem.OnGoldChange += GoldTextChange;
        }

        GoldTextChange();
    }

    private void OnDisable()
    {
        if (goldSystem != null)
        {
            goldSystem.OnGoldChange -= GoldTextChange;
        }
    }

    private void GoldTextChange()
    {
        if (goldSystem == null || goldText == null)
            return;

        goldText.text = goldSystem.GoldAmount.ToString();
    }
}