using UnityEngine;

public class PersistentRoot : MonoBehaviour
{
    public static PersistentRoot Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    private void Awake()
    {
        PersistentRoot[] allRoots =
            FindObjectsByType<PersistentRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (allRoots.Length > 1)
        {
            PersistentRoot keeper = ChooseKeeper(allRoots);

            if (keeper != this)
            {
                Debug.LogWarning(
                    $"[PersistentRoot] Duplicate root destroyed: '{name}' from scene '{gameObject.scene.name}'. " +
                    $"Keeper: '{keeper.name}' from scene '{keeper.gameObject.scene.name}'.",
                    this);

                Destroy(gameObject);
                return;
            }
        }

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                $"[PersistentRoot] Duplicate instance destroyed: '{name}' from scene '{gameObject.scene.name}'.",
                this);

            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (Instance != this)
            return;

        if (gameObject.scene.name != "DontDestroyOnLoad")
            DontDestroyOnLoad(gameObject);
    }

    private PersistentRoot ChooseKeeper(PersistentRoot[] roots)
    {
        PersistentRoot ddolRoot = null;
        PersistentRoot best = null;

        for (int i = 0; i < roots.Length; i++)
        {
            PersistentRoot root = roots[i];
            if (root == null) continue;

            if (root.gameObject.scene.name == "DontDestroyOnLoad")
            {
                ddolRoot = root;
                break;
            }

            if (best == null || root.GetInstanceID() < best.GetInstanceID())
                best = root;
        }

        return ddolRoot != null ? ddolRoot : best;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}