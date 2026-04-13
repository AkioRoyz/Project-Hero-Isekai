using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    private enum PauseRoot
    {
        Buttons,
        Save,
        Load
    }

    [Header("References")]
    [SerializeField] private GameInput gameInput;
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject buttonsRoot;
    [SerializeField] private GameObject saveRoot;
    [SerializeField] private GameObject loadRoot;
    [SerializeField] private SaveLoadPanelUI savePanel;
    [SerializeField] private SaveLoadPanelUI loadPanel;
    [SerializeField] private Button[] rootButtons;
    [SerializeField] private Button resumeButton;

    [Header("Pause Visual Effect")]
    [SerializeField] private Volume pauseGrayScaleVolume;
    [SerializeField, Range(0f, 1f)] private float pauseGrayScaleWeight = 1f;

    [Header("Scene Transition")]
    [SerializeField] private FullScreenFadeController fadeController;
    [SerializeField] private string loadZoneSceneName = "LoadZone";
    [SerializeField, Min(0.01f)] private float sceneFadeDuration = 0.35f;

    [Header("Quit")]
    [SerializeField] private LocalizedString quitMessage;
    [SerializeField, Min(0.01f)] private float quitFadeDuration = 0.65f;
    [SerializeField, Min(0f)] private float quitMessageHoldDuration = 1.2f;

    private GameInput subscribedInput;
    private PauseRoot currentRoot = PauseRoot.Buttons;
    private int selectedRootButtonIndex;
    private bool isBusy;

    private void Awake()
    {
        ResolveReferences();
        WireChildPanels();
        ForceClosedVisual();
    }

    private void OnEnable()
    {
        ResolveReferences();
        WireChildPanels();
        RebindInput();

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        UnbindInput();

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void ResolveReferences()
    {
        if (gameInput == null)
            gameInput = GameInput.Instance;

        if (fadeController == null)
            fadeController = FindFirstObjectByType<FullScreenFadeController>();
    }

    private void WireChildPanels()
    {
        if (savePanel != null)
        {
            savePanel.BackRequested -= ShowButtonsRoot;
            savePanel.BackRequested += ShowButtonsRoot;
        }

        if (loadPanel != null)
        {
            loadPanel.BackRequested -= ShowButtonsRoot;
            loadPanel.BackRequested += ShowButtonsRoot;
        }
    }

    private void RebindInput()
    {
        UnbindInput();

        if (gameInput == null)
            return;

        gameInput.OnPauseToggle += HandlePauseToggle;
        gameInput.OnPauseMenuUp += HandlePauseMenuUp;
        gameInput.OnPauseMenuDown += HandlePauseMenuDown;
        gameInput.OnPauseMenuSelect += HandlePauseMenuSelect;
        subscribedInput = gameInput;
    }

    private void UnbindInput()
    {
        if (subscribedInput == null)
            return;

        subscribedInput.OnPauseToggle -= HandlePauseToggle;
        subscribedInput.OnPauseMenuUp -= HandlePauseMenuUp;
        subscribedInput.OnPauseMenuDown -= HandlePauseMenuDown;
        subscribedInput.OnPauseMenuSelect -= HandlePauseMenuSelect;
        subscribedInput = null;
    }

    private void ForceClosedVisual()
    {
        ApplyClosedVisual(false);

        if (pauseGrayScaleVolume != null)
            pauseGrayScaleVolume.weight = 0f;

        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.Pause)
            GameStateManager.Instance.SetState(GameState.Playing);

        if (gameInput != null)
            gameInput.SwitchToPlayerMode();
    }

    private void ApplyClosedVisual(bool switchInputToPlayer)
    {
        currentRoot = PauseRoot.Buttons;
        selectedRootButtonIndex = 0;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (buttonsRoot != null)
            buttonsRoot.SetActive(true);

        if (saveRoot != null)
            saveRoot.SetActive(false);

        if (loadRoot != null)
            loadRoot.SetActive(false);

        if (pauseGrayScaleVolume != null)
            pauseGrayScaleVolume.weight = 0f;

        if (switchInputToPlayer && gameInput != null)
            gameInput.SwitchToPlayerMode();
    }

    private void HandlePauseToggle()
    {
        if (isBusy || GameStateManager.Instance == null || gameInput == null)
            return;

        if (GameStateManager.Instance.CurrentState == GameState.Playing)
        {
            OpenPauseMenu();
            return;
        }

        if (GameStateManager.Instance.CurrentState != GameState.Pause)
            return;

        if (currentRoot != PauseRoot.Buttons)
        {
            ShowButtonsRoot();
            return;
        }

        ResumeGame();
    }

    private void HandlePauseMenuUp()
    {
        if (isBusy || GameStateManager.Instance == null || GameStateManager.Instance.CurrentState != GameState.Pause)
            return;

        MoveSelection(-1);
    }

    private void HandlePauseMenuDown()
    {
        if (isBusy || GameStateManager.Instance == null || GameStateManager.Instance.CurrentState != GameState.Pause)
            return;

        MoveSelection(1);
    }

    private void HandlePauseMenuSelect()
    {
        if (isBusy || GameStateManager.Instance == null || GameStateManager.Instance.CurrentState != GameState.Pause)
            return;

        SubmitSelection();
    }

    private void MoveSelection(int direction)
    {
        switch (currentRoot)
        {
            case PauseRoot.Save:
                if (savePanel != null)
                    savePanel.MoveSelection(direction);
                break;

            case PauseRoot.Load:
                if (loadPanel != null)
                    loadPanel.MoveSelection(direction);
                break;

            default:
                MoveRootButtonSelection(direction);
                break;
        }
    }

    private void SubmitSelection()
    {
        switch (currentRoot)
        {
            case PauseRoot.Save:
                if (savePanel != null)
                    savePanel.Submit();
                break;

            case PauseRoot.Load:
                if (loadPanel != null)
                    loadPanel.Submit();
                break;

            default:
                InvokeSelectedRootButton();
                break;
        }
    }

    private void MoveRootButtonSelection(int direction)
    {
        List<Button> buttons = GetAvailableRootButtons();
        if (buttons.Count == 0)
            return;

        selectedRootButtonIndex = (selectedRootButtonIndex + direction) % buttons.Count;

        if (selectedRootButtonIndex < 0)
            selectedRootButtonIndex += buttons.Count;

        ApplyRootButtonSelection();
    }

    private void InvokeSelectedRootButton()
    {
        List<Button> buttons = GetAvailableRootButtons();
        if (buttons.Count == 0)
            return;

        selectedRootButtonIndex = Mathf.Clamp(selectedRootButtonIndex, 0, buttons.Count - 1);
        buttons[selectedRootButtonIndex].onClick.Invoke();
    }

    private List<Button> GetAvailableRootButtons()
    {
        List<Button> buttons = new List<Button>();

        if (rootButtons == null)
            return buttons;

        for (int i = 0; i < rootButtons.Length; i++)
        {
            if (rootButtons[i] != null && rootButtons[i].isActiveAndEnabled)
                buttons.Add(rootButtons[i]);
        }

        return buttons;
    }

    private int GetInitialRootButtonIndex()
    {
        List<Button> buttons = GetAvailableRootButtons();
        if (buttons.Count == 0)
            return -1;

        if (resumeButton != null)
        {
            int resumeIndex = buttons.IndexOf(resumeButton);
            if (resumeIndex >= 0)
                return resumeIndex;
        }

        return 0;
    }

    private void ApplyRootButtonSelection()
    {
        List<Button> buttons = GetAvailableRootButtons();
        if (buttons.Count == 0)
            return;

        selectedRootButtonIndex = Mathf.Clamp(selectedRootButtonIndex, 0, buttons.Count - 1);
        buttons[selectedRootButtonIndex].Select();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Pause)
            return;

        ApplyClosedVisual(false);
    }

    private void OpenPauseMenu()
    {
        GameStateManager.Instance.SetState(GameState.Pause);

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(true);

        if (pauseGrayScaleVolume != null)
            pauseGrayScaleVolume.weight = pauseGrayScaleWeight;

        if (gameInput != null)
            gameInput.SwitchToPauseMenuMode();

        ShowButtonsRoot();
    }

    public void ShowButtonsRoot()
    {
        currentRoot = PauseRoot.Buttons;

        if (buttonsRoot != null)
            buttonsRoot.SetActive(true);

        if (saveRoot != null)
            saveRoot.SetActive(false);

        if (loadRoot != null)
            loadRoot.SetActive(false);

        selectedRootButtonIndex = GetInitialRootButtonIndex();
        ApplyRootButtonSelection();
    }

    public void OpenSaveRoot()
    {
        currentRoot = PauseRoot.Save;

        if (buttonsRoot != null)
            buttonsRoot.SetActive(false);

        if (saveRoot != null)
            saveRoot.SetActive(true);

        if (loadRoot != null)
            loadRoot.SetActive(false);

        if (savePanel != null)
            savePanel.Show();
    }

    public void OpenLoadRoot()
    {
        currentRoot = PauseRoot.Load;

        if (buttonsRoot != null)
            buttonsRoot.SetActive(false);

        if (saveRoot != null)
            saveRoot.SetActive(false);

        if (loadRoot != null)
            loadRoot.SetActive(true);

        if (loadPanel != null)
            loadPanel.Show();
    }

    public void ResumeGame()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);

        ApplyClosedVisual(true);
    }

    public void OnResumePressed()
    {
        ResumeGame();
    }

    public void OnOpenSavePressed()
    {
        OpenSaveRoot();
    }

    public void OnOpenLoadPressed()
    {
        OpenLoadRoot();
    }

    public void OnExitToLoadZonePressed()
    {
        if (isBusy)
            return;

        StartCoroutine(LoadSceneRoutine(loadZoneSceneName));
    }

    public void OnQuitGamePressed()
    {
        if (isBusy)
            return;

        StartCoroutine(QuitRoutine());
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isBusy = true;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);

        Time.timeScale = 1f;
        ApplyClosedVisual(false);

        if (fadeController != null)
            yield return fadeController.FadeOut(sceneFadeDuration);

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator QuitRoutine()
    {
        isBusy = true;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);

        Time.timeScale = 1f;
        ApplyClosedVisual(false);

        if (fadeController != null)
            yield return fadeController.FadeOutWithMessage(quitMessage, quitFadeDuration, quitMessageHoldDuration);
        else if (quitMessageHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(quitMessageHoldDuration);

#if UNITY_EDITOR
        Debug.Log("PauseMenuController: quit requested.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}