using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerInkPouchConfig",
    menuName = "ZhongZhegnchao/Íæ¼ÒÄ«ÄÒÅäÖÃ"
)]
public class PlayerInkPouchConfig : ScriptableObject
{
    [Header("Ä«ÄÒÈİÁ¿")]
    public int maxInk = 10;

    [Header("³õÊ¼Ä«ÄÒ")]
    public int startInk = 10;

    [Header("»÷É±»Ö¸´")]
    public int restoreInkOnEnemyKill = 2;
}