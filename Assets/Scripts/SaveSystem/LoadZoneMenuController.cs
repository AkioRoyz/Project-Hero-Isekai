using UnityEngine;
using UnityEngine.InputSystem;

public class LoadZoneMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private SaveLoadPanelUI loadPanel;
    [SerializeField] private InputActionReference moveUpAction;
    [SerializeField] private InputActionReference moveDownAction;
    [SerializeField] private InputActionReference submitAction;
    [SerializeField] private InputActionReference backAction;

    private bool isOpen;

    private void Awake()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);

        if (loadPanel != null)
        {
            loadPanel.BackRequested -= CloseMenu;
            loadPanel.BackRequested += CloseMenu;
        }
    }

    private void OnDisable()
    {
        DisableInput();
    }

    public void OpenMenu()
    {
        if (isOpen)
            return;

        isOpen = true;

        if (menuRoot != null)
            menuRoot.SetActive(true);

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Menu);

        EnableInput();

        if (loadPanel != null)
            loadPanel.Show();
    }

    public void CloseMenu()
    {
        if (!isOpen)
            return;

        isOpen = false;

        DisableInput();

        if (loadPanel != null)
            loadPanel.Hide();

        if (menuRoot != null)
            menuRoot.SetActive(false);

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);
    }

    private void EnableInput()
    {
        BindAction(moveUpAction, HandleMoveUp);
        BindAction(moveDownAction, HandleMoveDown);
        BindAction(submitAction, HandleSubmit);
        BindAction(backAction, HandleBack);
    }

    private void DisableInput()
    {
        UnbindAction(moveUpAction, HandleMoveUp);
        UnbindAction(moveDownAction, HandleMoveDown);
        UnbindAction(submitAction, HandleSubmit);
        UnbindAction(backAction, HandleBack);
    }

    private void BindAction(InputActionReference actionReference, System.Action<InputAction.CallbackContext> handler)
    {
        if (actionReference == null || actionReference.action == null)
            return;

        actionReference.action.performed -= handler;
        actionReference.action.performed += handler;
        actionReference.action.Enable();
    }

    private void UnbindAction(InputActionReference actionReference, System.Action<InputAction.CallbackContext> handler)
    {
        if (actionReference == null || actionReference.action == null)
            return;

        actionReference.action.performed -= handler;
        actionReference.action.Disable();
    }

    private void HandleMoveUp(InputAction.CallbackContext context)
    {
        if (loadPanel != null)
            loadPanel.MoveSelection(-1);
    }

    private void HandleMoveDown(InputAction.CallbackContext context)
    {
        if (loadPanel != null)
            loadPanel.MoveSelection(1);
    }

    private void HandleSubmit(InputAction.CallbackContext context)
    {
        if (loadPanel != null)
            loadPanel.Submit();
    }

    private void HandleBack(InputAction.CallbackContext context)
    {
        CloseMenu();
    }
}