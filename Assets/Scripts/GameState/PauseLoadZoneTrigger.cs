using UnityEngine;

public class PauseLoadZoneTrigger : MonoBehaviour
{
    [Header("Trigger Rules")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOncePerEntry = true;
    [SerializeField] private bool doNotTriggerIfPauseAlreadyOpen = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private bool triggeredThisEntry;

    private void Reset()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            collider2D.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidTriggerObject(other.gameObject))
        {
            return;
        }

        if (triggerOnlyOncePerEntry && triggeredThisEntry)
        {
            return;
        }

        PauseMenuController pauseMenuController = FindFirstObjectByType<PauseMenuController>();
        if (pauseMenuController == null)
        {
            if (verboseLogs)
            {
                Debug.LogWarning("[PauseLoadZoneTrigger] PauseMenuController was not found in the scene.");
            }

            return;
        }

        if (doNotTriggerIfPauseAlreadyOpen && pauseMenuController.IsOpen)
        {
            return;
        }

        triggeredThisEntry = true;
        pauseMenuController.OpenTriggerLoadMenu();

        if (verboseLogs)
        {
            Debug.Log("[PauseLoadZoneTrigger] Trigger load menu requested.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValidTriggerObject(other.gameObject))
        {
            return;
        }

        triggeredThisEntry = false;
    }

    private bool IsValidTriggerObject(GameObject otherObject)
    {
        if (otherObject == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(playerTag))
        {
            return true;
        }

        return otherObject.CompareTag(playerTag);
    }
}