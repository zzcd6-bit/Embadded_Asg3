using UnityEngine;

[DisallowMultipleComponent]
public class PlayerLevelUpUIFeedback :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerCharacterStatsController
        statsController;

    [Header("Debug")]
    [SerializeField]
    private bool debugLog = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (statsController != null)
        {
            statsController.LevelChanged +=
                OnLevelChanged;
        }
    }

    private void OnDisable()
    {
        if (statsController != null)
        {
            statsController.LevelChanged -=
                OnLevelChanged;
        }
    }

    private void ResolveReferences()
    {
        if (statsController != null)
            return;

        statsController =
            GetComponent<
                PlayerCharacterStatsController>();

        if (statsController == null)
        {
            statsController =
                GetComponentInParent<
                    PlayerCharacterStatsController>();
        }

        if (statsController == null)
        {
            statsController =
                GetComponentInChildren<
                    PlayerCharacterStatsController>(true);
        }
    }

    private void OnLevelChanged(
        int newLevel
    )
    {
        if (newLevel <= 0)
            return;

        UIMgr.Instance.ShowPanel<
            BrushRecognitionResultPanel
        >(
            E_UILayer.Top,
            panel =>
            {
                panel.ShowLevelUp(newLevel);
            },
            true
        );

        if (debugLog)
        {
            Debug.Log(
                $"[PlayerLevelUpUIFeedback] " +
                $"Show Level Up UI. " +
                $"Level={newLevel}",
                this
            );
        }
    }
}