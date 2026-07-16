using System;
using System.Collections.Generic;

public static class InventoryPickupPersistence
{
    private static readonly HashSet<string>
        collectedIds =
            new HashSet<string>();

    public static event Action DataLoaded;

    public static bool IsCollected(
        string pickupId
    )
    {
        if (string.IsNullOrWhiteSpace(pickupId))
            return false;

        return collectedIds.Contains(pickupId);
    }

    public static void MarkCollected(
        string pickupId
    )
    {
        if (string.IsNullOrWhiteSpace(pickupId))
            return;

        collectedIds.Add(pickupId);
    }

    public static List<string>
        CaptureCollectedIds()
    {
        return new List<string>(
            collectedIds
        );
    }

    public static void LoadCollectedIds(
        IEnumerable<string> savedIds
    )
    {
        collectedIds.Clear();

        if (savedIds != null)
        {
            foreach (string savedId in savedIds)
            {
                if (string.IsNullOrWhiteSpace(
                        savedId))
                {
                    continue;
                }

                collectedIds.Add(savedId);
            }
        }

        DataLoaded?.Invoke();
    }

    public static void Reset()
    {
        collectedIds.Clear();

        DataLoaded?.Invoke();
    }
}