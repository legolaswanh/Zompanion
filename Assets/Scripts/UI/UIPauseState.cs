using System.Collections.Generic;
using UnityEngine;

public static class UIPauseState
{
    private static readonly HashSet<object> PauseOwners = new HashSet<object>();

    public static bool IsPaused => PauseOwners.Count > 0;

    public static void RequestPause(object owner)
    {
        if (owner == null)
            return;

        if (PauseOwners.Add(owner))
            RefreshTimeScale();
    }

    public static void ReleasePause(object owner)
    {
        if (owner == null)
            return;

        if (PauseOwners.Remove(owner))
            RefreshTimeScale();
    }

    public static void ClearAll()
    {
        PauseOwners.Clear();
        RefreshTimeScale();
    }

    private static void RefreshTimeScale()
    {
        Time.timeScale = IsPaused ? 0f : 1f;
    }
}
