using UnityEngine;

public class CursorVisibilityController : MonoBehaviour
{
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private bool applyInEditor = true;

    private void Awake()
    {
        ApplyCursorState();
    }

    private void OnEnable()
    {
        ApplyCursorState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            ApplyCursorState();
    }

    private void ApplyCursorState()
    {
#if UNITY_EDITOR
        if (!applyInEditor)
            return;
#endif
        Cursor.visible = false;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.Confined;
    }
}