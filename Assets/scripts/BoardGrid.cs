using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BoardGrid : MonoBehaviour
{
    public BoardSettings settings;

    [Header("Read-only debug")]
    public Vector2 boardWorldSize;
    public Vector2 playableWorldSize;
    public float cellSize;

    Renderer rend;

    public int N => settings ? settings.N : 0;
    public float MarginPercent => settings ? settings.marginPercent : 0f;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        Recompute();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!rend) rend = GetComponent<Renderer>();
        Recompute();
    }
#endif

    public void Recompute()
    {
        if (!rend || !settings) return;

        Vector3 size = rend.bounds.size;        // world size
        boardWorldSize = new Vector2(size.x, size.z);

        playableWorldSize = boardWorldSize * (1f - 2f * settings.marginPercent);

        // Square cells: choose x dimension as reference
        cellSize = playableWorldSize.x / settings.N;
    }

    public Vector3 CellToWorld(int x, int y, float yWorld = 0f)
    {
        if (!rend || !settings) return Vector3.zero;

        Bounds b = rend.bounds;
        Vector3 center = b.center;

        float left = center.x - b.size.x * 0.5f;
        float bottom = center.z - b.size.z * 0.5f;

        float marginX = b.size.x * settings.marginPercent;
        float marginZ = b.size.z * settings.marginPercent;

        float playableLeft = left + marginX;
        float playableBottom = bottom + marginZ;

        float wx = playableLeft + (x + 0.5f) * cellSize;
        float wz = playableBottom + (y + 0.5f) * cellSize;

        return new Vector3(wx, yWorld, wz);
    }

    public bool WorldToCell(Vector3 world, out Vector2Int cell)
    {
        cell = new Vector2Int(-1, -1);
        if (!rend || !settings) return false;

        Bounds b = rend.bounds;
        Vector3 center = b.center;

        float left = center.x - b.size.x * 0.5f;
        float bottom = center.z - b.size.z * 0.5f;

        float marginX = b.size.x * settings.marginPercent;
        float marginZ = b.size.z * settings.marginPercent;

        float playableLeft = left + marginX;
        float playableBottom = bottom + marginZ;

        float localX = world.x - playableLeft;
        float localZ = world.z - playableBottom;

        if (localX < 0 || localZ < 0 || localX >= playableWorldSize.x || localZ >= playableWorldSize.y)
            return false;

        int x = Mathf.FloorToInt(localX / cellSize);
        int y = Mathf.FloorToInt(localZ / cellSize);

        x = Mathf.Clamp(x, 0, settings.N - 1);
        y = Mathf.Clamp(y, 0, settings.N - 1);

        cell = new Vector2Int(x, y);
        return true;
    }
}
