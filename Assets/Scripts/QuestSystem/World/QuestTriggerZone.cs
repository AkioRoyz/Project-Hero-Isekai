using UnityEngine;

public class QuestTriggerZone : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private string zoneId;

    [Header("Behaviour")]
    [SerializeField] private bool notifyObjectiveOnEnter = true;
    [SerializeField] private QuestData[] questsToAcceptOnEnter;

    private int playerLayer;

    private void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != playerLayer)
            return;

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("QuestTriggerZone: QuestManager.Instance is missing.");
            return;
        }

        if (notifyObjectiveOnEnter && !string.IsNullOrWhiteSpace(zoneId))
        {
            QuestManager.Instance.NotifyTriggerZoneReached(zoneId);
        }

        if (questsToAcceptOnEnter == null || questsToAcceptOnEnter.Length == 0)
            return;

        for (int i = 0; i < questsToAcceptOnEnter.Length; i++)
        {
            QuestData questData = questsToAcceptOnEnter[i];

            if (questData == null)
                continue;

            QuestManager.Instance.AcceptQuest(questData.QuestId);
        }
    }
}