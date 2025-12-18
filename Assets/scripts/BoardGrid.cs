using UnityEngine;

public class BoardGrid : MonoBehaviour
{
    [Header("Grid")]
    public int N = 29;                    // cells per side (29x29)
    [Range(0f, 0.2f)] public float marginPercent = 0.08f; // must match your grid texture marginPercent

    [Header("Read-only debug")]
    public Vector2 boardWorldSize;
    public Vector2 playableWorldSize;
    public float cellSize;

    Renderer rend;

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
        if (!rend) return;

        // World size of the board mesh (works for Plane, Quad, custom mesh)
        Vector3 size = rend.bounds.size;     // x width, z height in world
        boardWorldSize = new Vector2(size.x, size.z);

        playableWorldSize = boardWorldSize * (1f - 2f * marginPercent);

        // Cell size for N cells spanning the playable area
        // (N cells means N intervals; centers are spaced playable/N)
        cellSize = playableWorldSize.x / N;  // assume square cells; board should be square-ish
    }

    // Convert cell (x,y) to world position at cell center
    public Vector3 CellToWorld(int x, int y, float yWorld = 0f)
    {
        Vector3 center = rend.bounds.center;

        float left = center.x - boardWorldSize.x * 0.5f;
        float bottom = center.z - boardWorldSize.y * 0.5f;

        float marginX = boardWorldSize.x * marginPercent;
        float marginZ = boardWorldSize.y * marginPercent;

        float playableLeft = left + marginX;
        float playableBottom = bottom + marginZ;

        float wx = playableLeft + (x + 0.5f) * cellSize;
        float wz = playableBottom + (y + 0.5f) * cellSize;

        return new Vector3(wx, yWorld, wz);
    }

    // Convert world position to nearest cell (x,y). Returns false if outside playable area.
    public bool WorldToCell(Vector3 world, out Vector2Int cell)
    {
        cell = new Vector2Int(-1, -1);
        Vector3 center = rend.bounds.center;

        float left = center.x - boardWorldSize.x * 0.5f;
        float bottom = center.z - boardWorldSize.y * 0.5f;

        float marginX = boardWorldSize.x * marginPercent;
        float marginZ = boardWorldSize.y * marginPercent;

        float playableLeft = left + marginX;
        float playableBottom = bottom + marginZ;

        float localX = world.x - playableLeft;
        float localZ = world.z - playableBottom;

        // Outside playable area?
        if (localX < 0 || localZ < 0 || localX >= playableWorldSize.x || localZ >= playableWorldSize.y)
            return false;

        int x = Mathf.FloorToInt(localX / cellSize);
        int y = Mathf.FloorToInt(localZ / cellSize);

        // Clamp just in case of edge floating error
        x = Mathf.Clamp(x, 0, N - 1);
        y = Mathf.Clamp(y, 0, N - 1);

        cell = new Vector2Int(x, y);
        return true;
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < N && y < N;
}
