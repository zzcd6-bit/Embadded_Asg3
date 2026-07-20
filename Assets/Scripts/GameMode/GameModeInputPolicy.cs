public static class GameModeInputPolicy
{
    public static bool CanDispatch(E_EventType eventType)
    {
        GameModeCapabilities capabilities =
            GameModeManager.Instance.CurrentCapabilities;

        switch (eventType)
        {
            case E_EventType.E_Input_Horizontal:
            case E_EventType.E_Input_Vertical:
            case E_EventType.E_Input_Jump:
            case E_EventType.E_Input_Dodge:
            case E_EventType.E_Player_Jump:
                return capabilities.canMove;

            case E_EventType.E_Input_Attack:
                return capabilities.canAttack;

            case E_EventType.E_Input_LockOn:
                return capabilities.canLockOn;

            case E_EventType.E_Player_NormalAttack:
                return capabilities.canAttack || capabilities.canDrawBrush;

            case E_EventType.E_Brush_Enter:
                return capabilities.canEnterBrush;

            case E_EventType.E_Brush_Exit:
            case E_EventType.E_Brush_DrawEnd:
                return capabilities.canDrawBrush ||
                       GameModeManager.Instance.CurrentMode ==
                       GameModeState.BrushBulletTime;

            case E_EventType.E_Build_TestStartItem1:
                return capabilities.canEnterBuild;

            case E_EventType.E_Build_PlacementConfirm:
            case E_EventType.E_Build_PlacementCancel:
            case E_EventType.E_Build_PlacementRotate:
            case E_EventType.E_Build_PlacementDelete:
                return capabilities.canUseBuildPlacement;

            case E_EventType.E_Interaction_ExecutePrimary:
            case E_EventType.E_Interaction_ExecuteAlternative:
                return capabilities.canInteract;

            case E_EventType.E_Input_CharacterPanel:
                return capabilities.canOpenMenu;

            default:
                return true;
        }
    }
}
