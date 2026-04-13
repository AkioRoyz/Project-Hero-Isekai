using UnityEngine;

public class LoadZoneTrigger : MonoBehaviour
{
    [SerializeField] private LoadZoneMenuController menuController;
    [SerializeField] private string playerTag = "Player";

    private bool playerInside;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playerInside || !other.CompareTag(playerTag))
            return;

        playerInside = true;

        if (menuController != null)
            menuController.OpenMenu();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = false;

        if (menuController != null)
            menuController.CloseMenu();
    }
}