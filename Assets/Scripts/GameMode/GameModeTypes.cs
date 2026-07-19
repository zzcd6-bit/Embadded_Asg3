using System;

public enum GameModeState
{
    Exploration,
    Combat,
    BrushBulletTime,
    Build,
    Dialogue,
    Cutscene,
    Menu,
    Dead
}

public enum GameInputOwner
{
    Gameplay,
    Brush,
    Build,
    Dialogue,
    UI,
    Cutscene,
    None
}

[Serializable]
public class GameModeCapabilities
{
    public GameInputOwner inputOwner = GameInputOwner.Gameplay;

    public bool canMove = true;
    public bool canLook = true;
    public bool canAttack = true;
    public bool canLockOn = true;
    public bool canEnterBrush = true;
    public bool canDrawBrush;
    public bool canEnterBuild = true;
    public bool canUseBuildPlacement;
    public bool canInteract = true;
    public bool canOpenMenu = true;
    public bool canTakeDamage = true;
    public bool canRecoverInk = true;
    public bool canBeDetectedByEnemy = true;

    public bool showInteractionUI = true;
    public bool showPlayerBars = true;

    public bool manageCursor = true;
    public bool lockCursor = true;
    public bool showCursor;

    public float timeScale = 1f;
    public int timeScalePriority;
    public float timeScaleTransitionDuration;

    public GameModeCapabilities Clone()
    {
        return (GameModeCapabilities)MemberwiseClone();
    }
}

public class GameModeChangedInfo : EventInfoBase
{
    public GameModeState oldMode;
    public GameModeState newMode;
    public GameModeCapabilities oldCapabilities;
    public GameModeCapabilities newCapabilities;
}

public class EnemyCombatChangedInfo : EventInfoBase
{
    public UnityEngine.Object source;
    public UnityEngine.GameObject enemyObject;
    public bool isActive;
    public string reason;
}
