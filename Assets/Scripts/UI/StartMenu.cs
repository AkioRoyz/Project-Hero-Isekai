using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [Header("Start Game")]
    [SerializeField] private string firstGameplaySceneName = "SampleScene";
    [SerializeField] private string startEntryPointId;

    public void StartTest()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(firstGameplaySceneName, startEntryPointId);
            return;
        }

        SceneTransitionState.SetNextEntryPoint(startEntryPointId);
        SceneManager.LoadScene(firstGameplaySceneName, LoadSceneMode.Single);
    }

    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}