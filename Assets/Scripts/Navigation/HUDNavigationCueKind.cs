using UnityEngine;

public enum HUDNavigationCueKind
{
    Important,
    Secondary,
    NonTask
}

public static class HUDNavigationCuePalette
{
    public static readonly Color Important = FromHex(0xFF8043);
    public static readonly Color Secondary = FromHex(0x36C1B0);
    public static readonly Color NonTask = FromHex(0xFFDB1F);

    public static Color GetColor(HUDNavigationCueKind kind)
    {
        return kind switch
        {
            HUDNavigationCueKind.Important => Important,
            HUDNavigationCueKind.Secondary => Secondary,
            _ => NonTask
        };
    }

    private static Color FromHex(int rgb)
    {
        return new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f,
            1f);
    }
}
