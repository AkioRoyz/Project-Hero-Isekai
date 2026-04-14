using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class PauseMenuController : MonoBehaviour
{
    private enum ActiveRoot
    {
        Main,
        Save,
        Load
    }

    [Header("Main References")]
    [SerializeField] private GameObject pauseCanvasRoot;
    [SerializeField] private GameObject mainRoot;
    [SerializeField] private PauseMenuSaveLoadRootUI saveRootUI;
    [SerializeField] private PauseMenuSaveLoadRootUI loadRootUI;
    [SerializeField] private PauseMenuSlotViewUI[] mainMenuSlots;

    [Header("Pause Visuals")]
    [SerializeField] private GameObject pauseOverlayRoot;
    [SerializeField] private GameObject quitMessageRoot;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InputSystemUIInputModule inputModule;
    [SerializeField] private string gameplayActionMapName = "Player";
    [SerializeField] private string pauseActionMapName = "PauseMenu";

    [SerializeField] private InputActionReference openPauseAction;
    [SerializeField] private InputActionReference closePauseAction;
    [SerializeField] private InputActionReference upSelectAction;
    [SerializeField] private InputActionReference downSelectAction;
    [SerializeField] private InputActionReference selectChoiceAction;
    [SerializeField] private InputActionReference cancelAction;

    [Header("Behaviour")]
    [SerializeField] private bool manageTimeScale = true;
    [SerializeField] private bool manageActionMaps = true;
    [SerializeField] private bool openMenuOnStart = false;

    [Header("Delays")]
    [SerializeField] private float saveDelayAfterClose = 0.05f;
    [SerializeField] private float loadDelayBeforeSaveLoadCall = 0.35f;
    [SerializeField] private float quitDelay = 0.6f;

    [Header("Project Hooks")]
    [SerializeField] private UnityEvent onPauseOpened;
    [SerializeField] private UnityEvent onPauseClosed;
    [SerializeField] private UnityEvent onBeforeLoadTransition;
    [SerializeField] private UnityEvent onQuitStarted;

    private bool isOpen;
    private bool isBusy;
    private ActiveRoot activeRoot = ActiveRoot.Main;
    private int mainSelectionIndex;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        BindRootEvents();
        ForceClosedState();
    }

    private void Start()
    {
        RefreshMainMenuVisuals();

        if (openMenuOnStart)
        {
            OpenPauseMenu();
        }
    }

    private void OnEnable()
    {
        SubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();

        if (manageTimeScale)
        {
            Time.timeScale = 1f;
        }
    }

    private void OnDestroy()
    {
        UnbindRootEvents();
    }

    private void BindRootEvents()
    {
        if (saveRootUI != null)
        {
            saveRootUI.DataSlotChosen += HandleSaveSlotChosen;
            saveRootUI.BackRequested += HandleSaveBackRequested;
        }

        if (loadRootUI != null)
        {
            loadRootUI.DataSlotChosen += HandleLoadSlotChosen;
            loadRootUI.BackRequested += HandleLoadBackRequested;
        }
    }

    private void UnbindRootEvents()
    {
        if (saveRootUI != null)
        {
            saveRootUI.DataSlotChosen -= HandleSaveSlotChosen;
            saveRootUI.BackRequested -= HandleSaveBackRequested;
        }

        if (loadRootUI != null)
        {
            loadRootUI.DataSlotChosen -= HandleLoadSlotChosen;
            loadRootUI.BackRequested -= HandleLoadBackRequested;
        }
    }

    private void SubscribeInput()
    {
        SubscribeAction(openPauseAction, HandleOpenPausePerformed);
        SubscribeAction(closePauseAction, HandleClosePausePerformed);
        SubscribeAction(upSelectAction, HandleUpPerformed);
        SubscribeAction(downSelectAction, HandleDownPerformed);
        SubscribeAction(selectChoiceAction, HandleSubmitPerformed);
        SubscribeAction(cancelAction, HandleCancelPerformed);
    }

    private void UnsubscribeInput()
    {
        UnsubscribeAction(openPauseAction, HandleOpenPausePerformed);
        UnsubscribeAction(closePauseAction, HandleClosePausePerformed);
        UnsubscribeAction(upSelectAction, HandleUpPerformed);
        UnsubscribeAction(downSelectAction, HandleDownPerformed);
        UnsubscribeAction(selectChoiceAction, HandleSubmitPerformed);
        UnsubscribeAction(cancelAction, HandleCancelPerformed);
    }

    private void SubscribeAction(InputActionReference actionReference, System.Action<InputAction.CallbackContext> callback)
    {
        if (actionReference == null || actionReference.action == null)
        {
            return;
        }

        actionReference.action.performed += callback;
    }

    private void UnsubscribeAction(InputActionReference actionReference, System.Action<InputAction.CallbackContext> callback)
    {
        if (actionReference == null || actionReference.action == null)
        {
            return;
        }

        actionReference.action.performed -= callback;
    }

    private void HandleOpenPausePerformed(InputAction.CallbackContext context)
    {
        if (isBusy || isOpen)
        {
            return;
        }

        OpenPauseMenu();
    }

    private void HandleClosePausePerformed(InputAction.CallbackContext context)
    {
        if (isBusy || !isOpen)
        {
            return;
        }

        ClosePauseMenu();
    }

    private void HandleUpPerformed(InputAction.CallbackContext context)
    {
        if (isBusy || !isOpen)
        {
            return;
        }

        MoveSelection(-1);
    }

    private void HandleDownPerformed(InputAction.CallbackContext context)
    {
        if (isBusy || !isOpen)
        {
            return;
        }

        MoveSelection(+1);
    }

    private void HandleSubmitPerformed(InputAction.CallbackContext context)
    {
        if (isBusy || !isOpen)
        {
            return;
        }

        SubmitCurrentSelection();
    }

    private void HandleCancelPerformed(InputAction.CallbackContext context)
    {
        if (isBusy || !isOpen)
        {
            return;
        }

        if (activeRoot == ActiveRoot.Main)
        {
            ClosePauseMenu();
            return;
        }

        ShowMainRoot();
    }

    public void OpenPauseMenu()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        activeRoot = ActiveRoot.Main;

        if (pauseCanvasRoot != null)
        {
            pauseCanvasRoot.SetActive(true);
        }

        if (pauseOverlayRoot != null)
        {
            pauseOverlayRoot.SetActive(true);
        }

        if (quitMessageRoot != null)
        {
            quitMessageRoot.SetActive(false);
        }

        if (manageTimeScale)
        {
            Time.timeScale = 0f;
        }

        if (manageActionMaps && playerInput != null && !string.IsNullOrWhiteSpace(pauseActionMapName))
        {
            playerInput.SwitchCurrentActionMap(pauseActionMapName);
        }

        ShowMainRoot();
        onPauseOpened?.Invoke();
    }

    public void ClosePauseMenu()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        activeRoot = ActiveRoot.Main;

        HideAllRoots();

        if (pauseOverlayRoot != null)
        {
            pauseOverlayRoot.SetActive(false);
        }

        if (pauseCanvasRoot != null)
        {
            pauseCanvasRoot.SetActive(false);
        }

        if (quitMessageRoot != null)
        {
            quitMessageRoot.SetActive(false);
        }

        if (manageActionMaps && playerInput != null && !string.IsNullOrWhiteSpace(gameplayActionMapName))
        {
            playerInput.SwitchCurrentActionMap(gameplayActionMapName);
        }

        if (manageTimeScale)
        {
            Time.timeScale = 1f;
        }

        onPauseClosed?.Invoke();
    }

    public void TogglePauseMenu()
    {
        if (isOpen)
        {
            ClosePauseMenu();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    public void ResumeGame()
    {
        ClosePauseMenu();
    }

    public void OpenSaveRoot()
    {
        if (!isOpen || saveRootUI == null)
        {
            return;
        }

        activeRoot = ActiveRoot.Save;

        if (mainRoot != null)
        {
            mainRoot.SetActive(false);
        }

        if (loadRootUI != null)
        {
            loadRootUI.HideRoot();
        }

        saveRootUI.ShowRoot();
    }

    public void OpenLoadRoot()
    {
        if (!isOpen || loadRootUI == null)
        {
            return;
        }

        activeRoot = ActiveRoot.Load;

        if (mainRoot != null)
        {
            mainRoot.SetActive(false);
        }

        if (saveRootUI != null)
        {
            saveRootUI.HideRoot();
        }

        loadRootUI.ShowRoot();
    }

    public void ShowMainRoot()
    {
        activeRoot = ActiveRoot.Main;

        if (mainRoot != null)
        {
            mainRoot.SetActive(true);
        }

        if (saveRootUI != null)
        {
            saveRootUI.HideRoot();
        }

        if (loadRootUI != null)
        {
            loadRootUI.HideRoot();
        }

        RefreshMainMenuVisuals();
    }

    private void HideAllRoots()
    {
        if (mainRoot != null)
        {
            mainRoot.SetActive(false);
        }

        if (saveRootUI != null)
        {
            saveRootUI.HideRoot();
        }

        if (loadRootUI != null)
        {
            loadRootUI.HideRoot();
        }
    }

    private void ForceClosedState()
    {
        isOpen = false;
        isBusy = false;
        activeRoot = ActiveRoot.Main;

        if (pauseCanvasRoot != null)
        {
            pauseCanvasRoot.SetActive(false);
        }

        if (pauseOverlayRoot != null)
        {
            pauseOverlayRoot.SetActive(false);
        }

        if (quitMessageRoot != null)
        {
            quitMessageRoot.SetActive(false);
        }

        HideAllRoots();

        if (manageTimeScale)
        {
            Time.timeScale = 1f;
        }
    }

    private void MoveSelection(int direction)
    {
        switch (activeRoot)
        {
            case ActiveRoot.Main:
                MoveMainSelection(direction);
                break;

            case ActiveRoot.Save:
                saveRootUI?.MoveSelection(direction);
                break;

            case ActiveRoot.Load:
                loadRootUI?.MoveSelection(direction);
                break;
        }
    }

    private void SubmitCurrentSelection()
    {
        switch (activeRoot)
        {
            case ActiveRoot.Main:
                SubmitMainSelection();
                break;

            case ActiveRoot.Save:
                saveRootUI?.SubmitCurrentSelection();
                break;

            case ActiveRoot.Load:
                loadRootUI?.SubmitCurrentSelection();
                break;
        }
    }

    private void MoveMainSelection(int direction)
    {
        if (mainMenuSlots == null || mainMenuSlots.Length == 0)
        {
            return;
        }

        mainSelectionIndex += direction;

        if (mainSelectionIndex < 0)
        {
            mainSelectionIndex = mainMenuSlots.Length - 1;
        }
        else if (mainSelectionIndex >= mainMenuSlots.Length)
        {
            mainSelectionIndex = 0;
        }

        RefreshMainMenuVisuals();
    }

    private void RefreshMainMenuVisuals()
    {
        if (mainMenuSlots == null || mainMenuSlots.Length == 0)
        {
            return;
        }

        if (mainSelectionIndex < 0 || mainSelectionIndex >= mainMenuSlots.Length)
        {
            mainSelectionIndex = 0;
        }

        for (int i = 0; i < mainMenuSlots.Length; i++)
        {
            if (mainMenuSlots[i] == null)
            {
                continue;
            }

            mainMenuSlots[i].ShowAsMainOption();
            mainMenuSlots[i].SetSelected(i == mainSelectionIndex);
        }
    }

    private void SubmitMainSelection()
    {
        switch (mainSelectionIndex)
        {
            case 0:
                ClosePauseMenu();
                break;

            case 1:
                OpenSaveRoot();
                break;

            case 2:
                OpenLoadRoot();
                break;

            case 3:
                StartCoroutine(QuitRoutine());
                break;
        }
    }

    private void HandleSaveSlotChosen(int slotIndex)
    {
        if (isBusy)
        {
            return;
        }

        StartCoroutine(SaveRoutine(slotIndex));
    }

    private void HandleLoadSlotChosen(int slotIndex)
    {
        if (isBusy)
        {
            return;
        }

        StartCoroutine(LoadRoutine(slotIndex));
    }

    private void HandleSaveBackRequested()
    {
        ShowMainRoot();
        mainSelectionIndex = 1;
        RefreshMainMenuVisuals();
    }

    private void HandleLoadBackRequested()
    {
        ShowMainRoot();
        mainSelectionIndex = 2;
        RefreshMainMenuVisuals();
    }

    private IEnumerator SaveRoutine(int slotIndex)
    {
        isBusy = true;

        ClosePauseMenu();

        yield return null;
        yield return new WaitForEndOfFrame();

        if (saveDelayAfterClose > 0f)
        {
            yield return new WaitForSecondsRealtime(saveDelayAfterClose);
        }

        if (saveRootUI != null && saveRootUI.SaveAdapter != null)
        {
            saveRootUI.SaveAdapter.SaveToSlot(slotIndex);
        }

        isBusy = false;
    }

    private IEnumerator LoadRoutine(int slotIndex)
    {
        isBusy = true;

        ClosePauseMenu();

        onBeforeLoadTransition?.Invoke();

        if (loadDelayBeforeSaveLoadCall > 0f)
        {
            yield return new WaitForSecondsRealtime(loadDelayBeforeSaveLoadCall);
        }

        if (loadRootUI != null && loadRootUI.SaveAdapter != null)
        {
            loadRootUI.SaveAdapter.LoadFromSlot(slotIndex);
        }

        isBusy = false;
    }

    private IEnumerator QuitRoutine()
    {
        isBusy = true;

        if (quitMessageRoot != null)
        {
            quitMessageRoot.SetActive(true);
        }

        onQuitStarted?.Invoke();

        if (quitDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(quitDelay);
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

        isBusy = false;
    }
}