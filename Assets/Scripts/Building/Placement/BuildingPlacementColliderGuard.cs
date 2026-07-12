using System.Collections.Generic;
using UnityEngine;

public class BuildingPlacementColliderGuard : MonoBehaviour
{
    [SerializeField] private float boundsPadding = 0.05f;

    private readonly List<ColliderState> colliderStates = new();
    private BuildingInstance buildingInstance;

    //Starts temporary collision protection after a building is placed.
    //建筑放置后启动临时碰撞保护。
    public void Begin(BuildingInstance building)
    {
        buildingInstance = building;
        CacheAndSoftenColliders();

        if (colliderStates.Count == 0)
        {
            CompletePlacement();
        }
    }

    //Waits until the player leaves the cached building bounds.
    //等待玩家离开缓存的建筑范围。
    private void Update()
    {
        if (buildingInstance == null)
        {
            Destroy(this);
            return;
        }

        if (!IsPlayerInsideAnyBuildingCollider())
        {
            CompletePlacement();
        }
    }

    //Caches enabled colliders and disables them during the safety window.
    //缓存启用的碰撞体，并在安全窗口期间禁用它们。
    private void CacheAndSoftenColliders()
    {
        colliderStates.Clear();

        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            if (collider.GetComponentInParent<BuildingNavMeshProxy>() != null)
            {
                continue;
            }

            ColliderState state = new(collider);
            colliderStates.Add(state);
            collider.enabled = false;
        }
    }

    //Checks whether the player bounds still overlap the cached building bounds.
    //检测玩家包围盒是否仍与缓存的建筑包围盒重叠。
    private bool IsPlayerInsideAnyBuildingCollider()
    {
        Bounds? playerBounds = GetPlayerBounds();
        if (!playerBounds.HasValue)
        {
            return false;
        }

        Bounds paddedPlayerBounds = playerBounds.Value;
        paddedPlayerBounds.Expand(boundsPadding);

        foreach (ColliderState state in colliderStates)
        {
            if (state.Collider == null)
            {
                continue;
            }

            Bounds buildingBounds = state.WorldBounds;
            buildingBounds.Expand(boundsPadding);
            if (buildingBounds.Intersects(paddedPlayerBounds))
            {
                return true;
            }
        }

        return false;
    }

    //Finds the active player bounds from PlayerController or Player tag.
    //从 PlayerController 或 Player 标签查找当前玩家包围盒。
    private static Bounds? GetPlayerBounds()
    {
        PlayerController player = PlayerController.instance;
        if (player != null)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider != null && playerCollider.enabled)
            {
                return playerCollider.bounds;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null && controller.enabled)
            {
                return controller.bounds;
            }
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer == null)
        {
            return null;
        }

        Collider taggedCollider = taggedPlayer.GetComponent<Collider>();
        if (taggedCollider != null && taggedCollider.enabled)
        {
            return taggedCollider.bounds;
        }

        CharacterController taggedController = taggedPlayer.GetComponent<CharacterController>();
        return taggedController != null && taggedController.enabled ? taggedController.bounds : null;
    }

    //Restores building collision and marks placement as completed.
    //恢复建筑碰撞，并标记摆放完成。
    private void CompletePlacement()
    {
        RestoreColliders();

        if (buildingInstance != null)
        {
            buildingInstance.MarkPlacementCompleted();
        }

        Destroy(this);
    }

    //Restores every collider to its cached state.
    //将所有碰撞体恢复到缓存状态。
    private void RestoreColliders()
    {
        foreach (ColliderState state in colliderStates)
        {
            if (state.Collider == null)
            {
                continue;
            }

            state.Collider.enabled = state.WasEnabled;
            state.Collider.isTrigger = state.WasTrigger;
        }

        colliderStates.Clear();
    }

    private readonly struct ColliderState
    {
        public readonly Collider Collider;
        public readonly bool WasEnabled;
        public readonly bool WasTrigger;
        public readonly Bounds WorldBounds;

        public ColliderState(Collider collider)
        {
            Collider = collider;
            WasEnabled = collider.enabled;
            WasTrigger = collider.isTrigger;
            WorldBounds = collider.bounds;
        }
    }
}
