using UnityEngine;

[CreateAssetMenu(menuName = "Board Game/Board Settings", fileName = "BoardSettings")]
public class BoardSettings : ScriptableObject
{
    [Header("Board")]
    [Min(1)] public int N = 29;
    [Range(0f, 0.2f)] public float marginPercent = 0.08f;

    [Header("Grid Texture (Zoom-in quality)")]
    [Min(16)] public int targetPixelsPerCell = 128; // 64 / 128 / 256
    [Min(1)] public int lineWidthPx = 2;

    [Header("Optional")]
    public FilterMode gridFilterMode = FilterMode.Bilinear;
}
