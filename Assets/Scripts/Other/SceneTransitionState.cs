using UnityEngine;

public static class SceneTransitionState
{
    public static string PendingEntryPointId { get; private set; }

    public static void SetNextEntryPoint(string entryPointId)
    {
        PendingEntryPointId = string.IsNullOrWhiteSpace(entryPointId) ? null : entryPointId;
    }

    public static string ConsumePendingEntryPoint()
    {
        string value = PendingEntryPointId;
        PendingEntryPointId = null;
        return value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        PendingEntryPointId = null;
    }
}