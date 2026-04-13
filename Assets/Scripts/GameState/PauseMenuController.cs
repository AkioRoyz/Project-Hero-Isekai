using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameInput gameInput;
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private Button resumeButton;

    [Header("Pause Visual Effect")]
    [SerializeField] private Volume pauseGrayScaleVolume;
    [SerializeField, Range(0f, 1f)] private float pauseGrayScaleWeight = 1f;

    private GameInput subscribedInput;

    private void Awake()
    {
        ResolveReferences();
        ForceClosedVisual();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;

        ResolveReferences();
        RebindInput();

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        ForceClosedVisual();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        UnbindInput();

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveReferences();
        RebindInput();
        ForceClosedVisual();
    }

    private void ResolveReferences()
    {
        gameInput = GameInput.Instance != null
            ? GameInput.Instance
            : FindFirstObjectByType<GameInput>();
    }

    private void RebindInput()
    {
        UnbindInput();

        if (gameInput == null)
            return;

        gameInput.OnPauseToggle += HandlePauseToggle;
        gameInput.OnPauseMenuSelect += HandlePauseMenuSelect;

        subscribedInput = gameInput;
    }

    private void UnbindInput()
    {
        if (subscribedInput == null)
            return;

        subscribedInput.OnPauseToggle -= HandlePauseToggle;
        subscribedInput.OnPauseMenuSelect -= HandlePauseMenuSelect;
        subscribedInput = null;
    }

    private void ForceClosedVisual()
    {
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (pauseGrayScaleVolume != null)
            pauseGrayScaleVolume.weight = 0f;

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState == GameState.Pause)
        {
            GameStateManager.Instance.SetState(GameState.Playing);
        }

        if (gameInput != null)
            gameInput.SwitchToPlayerMode();
    }

    private void HandlePauseToggle()
    {
        if (GameStateManager.Instance == null || gameInput == null)
            return;

        if (GameStateManager.Instance.CurrentState == GameState.Playing)
        {
            OpenPauseMenu();
        }
        else if (GameStateManager.Instance.CurrentState == GameState.Pause)
        {
            ResumeGame();
        }
    }

    private void HandlePauseMenuSelect()
    {
        if (GameStateManager.Instance == null)
            return;

        if (GameStateManager.Instance.CurrentState != GameState.Pause)
            return;

        ResumeGame();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        bool isPause = newState == GameState.Pause;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(isPause);

        if (pauseGrayScaleVolume != null)
            pauseGrayScaleVolume.weight = isPause ? pauseGrayScaleWeight : 0f;

        if (isPause && resumeButton != null)
            resumeButton.Select();
    }

    public void ResumeGame()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (pauseGrayScaleVolume != null)
            pauseGrayScaleVolume.weight = 0f;

        if (gameInput != null)
            gameInput.SwitchToPlayerMode();
    }

    private void OpenPauseMenu()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Pause);

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(true);

        if (pauseGrayScaleVolume != null)
            pauseGrayScaleVolume.weight = pauseGrayScaleWeight;

        if (gameInput != null)
            gameInput.SwitchToPauseMenuMode();

        if (resumeButton != null)
            resumeButton.Select();
    }
}