using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public event Action<GameState> OnGameStateChanged;

    [SerializeField] private GameState initialState = GameState.Playing;

    private GameState currentState;

    public GameState CurrentState => currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentState = initialState;
        ApplyState(currentState);
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        ApplyState(currentState);
        OnGameStateChanged?.Invoke(currentState);
    }

    private void ApplyState(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                break;

            case GameState.Pause:
            case GameState.Menu:
            case GameState.Dialogue:
                Time.timeScale = 0f;
                break;
        }
    }
}