using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    [SerializeField] private string firstSceneName = "SampleScene";
    [SerializeField] private string targetEntryPointId;
    [SerializeField] private bool useSceneTransitionManager = false;

    private bool hasLoaded;

    private void Start()
    {
        if (hasLoaded)
            return;

        hasLoaded = true;

        if (string.IsNullOrWhiteSpace(firstSceneName))
        {
            Debug.LogError("[BootLoader] First scene name is empty.", this);
            return;
        }

        if (useSceneTransitionManager && SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(firstSceneName, targetEntryPointId);
            return;
        }

        SceneTransitionState.SetNextEntryPoint(targetEntryPointId);
        SceneManager.LoadScene(firstSceneName, LoadSceneMode.Single);
    }
}