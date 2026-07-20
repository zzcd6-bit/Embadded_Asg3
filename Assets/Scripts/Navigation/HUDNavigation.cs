using UnityEngine;

public static class HUDNavigation
{
    public static bool StartFollowing(GameObject target)
    {
        return StartFollowing(target, HUDNavigationCueKind.NonTask);
    }

    public static bool StartFollowing(GameObject target, HUDNavigationCueKind cueKind)
    {
        NavigationHUDIndicator indicator = FindIndicator();
        if (indicator == null)
        {
            Debug.LogWarning("[HUDNavigation] NavigationHUDIndicator was not found in the scene.");
            return false;
        }

        indicator.StartFollowing(target, cueKind);
        return true;
    }

    public static bool StopFollowing(GameObject target)
    {
        NavigationHUDIndicator indicator = FindIndicator();
        if (indicator == null)
        {
            return false;
        }

        indicator.StopFollowing(target);
        return true;
    }

    private static NavigationHUDIndicator FindIndicator()
    {
        return NavigationHUDIndicator.Instance != null
            ? NavigationHUDIndicator.Instance
            : Object.FindAnyObjectByType<NavigationHUDIndicator>(FindObjectsInactive.Include);
    }
}
