// PaletteRandomSpawner.cs
// Spawns a random set of contiguous polyominoes (size between minCells..maxCells)
// on a Palette plane. Pieces are drawn as tile-cubes (correct footprint),
// use a neutral palette material, can repeat, and disappear when picked (handled in ModeController).
//
// Key features:
// - Weighted size distribution: smaller pieces more frequent, max about `rarityMaxFactor` times rarer than min.
// - Optional auto-resize of the Palette plane (kept square) to fit spawnCount reasonably.
// - gapCells is an int because layout is cell-based (grid aligned).

using System;
using System.Collections.Generic;
using UnityEngine;

public class PaletteRandomSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Renderer paletteRenderer;      // Palette plane renderer
    public Transform paletteRoot;         // Parent for spawned palette pieces
    public BoardGrid boardGrid;           // Use board cell size for perfect match
    public Material paletteDefaultMat;    // Neutral material for palette pieces

    [Header("Pick Layer")]
    public string paletteLayerName = "Palette"; // spawned roots will be forced to this layer

    [Header("Random Pieces")]
    [Min(1)] public int spawnCount = 10;
    [Min(1)] public int minCells = 1;
    [Min(1)] public int maxCells = 5;

    [Tooltip("Max size is about this many times rarer than min size. Example 3 = max appears ~3x less often.")]
    [Min(1.1f)] public float rarityMaxFactor = 3f;

    [Header("Layout")]
    [Tooltip("Padding from palette edges in world units.")]
    [Min(0f)] public float marginWorld = 0.5f;

    [Tooltip("Extra height above palette surface.")]
    [Min(0f)] public float yOffset = 0.05f;

    [Tooltip("Empty CELL gap between pieces (grid aligned).")]
    [Min(0)] public int gapCells = 1;

    [Header("Auto Resize Palette (square)")]
    public bool autoResizePalette = true;

    [Tooltip("Extra cells added to square side estimate (breathing room).")]
    [Min(0f)] public float paletteCellPadding = 2f;

    [Tooltip("If true, Regenerate() will be called in Start().")]
    public bool generateOnStart = true;

    readonly List<GameObject> spawned = new();

    void Start()
    {
        if (generateOnStart)
            Regenerate();
    }

    public void Regenerate()
    {
        Clear();

        if (!paletteRenderer)
        {
            Debug.LogError("[PaletteRandomSpawner] paletteRenderer is NULL");
            return;
        }
        if (!paletteRoot)
        {
            Debug.LogError("[PaletteRandomSpawner] paletteRoot is NULL");
            return;
        }
        if (!boardGrid)
        {
            Debug.LogError("[PaletteRandomSpawner] boardGrid is NULL");
            return;
        }

        // Ensure cell size is valid
        float cell = boardGrid.cellSize > 0f ? boardGrid.cellSize : 1f;

        if (autoResizePalette)
            ResizePaletteToFit(cell);

        // Recompute bounds after resize
        Bounds b = paletteRenderer.bounds;

        float usableW = b.size.x - 2f * marginWorld;
        float usableH = b.size.z - 2f * marginWorld;

        if (usableW <= 0.01f || usableH <= 0.01f)
        {
            Debug.LogError($"[PaletteRandomSpawner] Palette usable area too small. " +
                           $"paletteSize={b.size} marginWorld={marginWorld}");
            return;
        }

        float left = b.min.x + marginWorld;
        float bottom = b.min.z + marginWorld;

        int maxCols = Mathf.Max(1, Mathf.FloorToInt(usableW / cell));
        int maxRows = Mathf.Max(1, Mathf.FloorToInt(usableH / cell));

        var rng = new System.Random();

        // Simple packing in cell-space rows
        int cursorX = 0;
        int cursorY = 0;
        int rowHeight = 0;

        int created = 0;
        int layerIndex = LayerMask.NameToLayer(paletteLayerName);

        for (int k = 0; k < spawnCount; k++)
        {
            int size = PickSizeWeighted(rng);

            Vector2Int[] offsets = RandomPolyomino.Generate(size, rng); // normalized, contiguous

            // Offsets are normalized -> bounds start at (0,0)
            int w, h;
            GetNormalizedBoundsWH(offsets, out w, out h);

            // Wrap to next row if needed
            if (cursorX + w > maxCols)
            {
                cursorX = 0;
                cursorY += rowHeight + gapCells;
                rowHeight = 0;
            }

            // Stop if no more vertical space
            if (cursorY + h > maxRows)
                break;

            float anchorX = left + cursorX * cell;
            float anchorZ = bottom + cursorY * cell;

            GameObject go = BuildPaletteDisplay(offsets, anchorX, anchorZ, cell, layerIndex);

            // Attach PalettePiece + runtime PieceSettings
            var ps = ScriptableObject.CreateInstance<PieceSettings>();
            ps.offsets = offsets;
            ps.allowRotate = true;
            ps.allowFlip = false;
            ps.name = $"Rand_{size}_{created}";

            var pp = go.AddComponent<PalettePiece>();
            pp.piece = ps;

            spawned.Add(go);

            cursorX += w + gapCells;
            rowHeight = Mathf.Max(rowHeight, h);

            created++;
        }

        Debug.Log($"[PaletteRandomSpawner] Spawned {created}/{spawnCount} pieces. " +
                  $"PaletteSize={paletteRenderer.bounds.size} cell={cell:F3} maxCols={maxCols} maxRows={maxRows}");
    }

    // --- Weighted size distribution ---
    // Size=lo most common, Size=hi about rarityMaxFactor times rarer.
    int PickSizeWeighted(System.Random rng)
    {
        int lo = Mathf.Max(1, minCells);
        int hi = Mathf.Max(lo, maxCells);
        if (lo == hi) return lo;

        float total = 0f;
        float[] w = new float[hi - lo + 1];

        for (int s = lo; s <= hi; s++)
        {
            float t = (s - lo) / (float)(hi - lo); // 0..1
            float weight = Mathf.Lerp(1f, 1f / Mathf.Max(1.001f, rarityMaxFactor), t);
            w[s - lo] = weight;
            total += weight;
        }

        double r = rng.NextDouble() * total;
        float acc = 0f;

        for (int i = 0; i < w.Length; i++)
        {
            acc += w[i];
            if (r <= acc) return lo + i;
        }
        return hi;
    }

    // --- Auto resize palette to keep it square and roomy ---
    void ResizePaletteToFit(float cell)
    {
        if (!paletteRenderer) return;

        // Heuristic: average footprint side grows ~ sqrt(size)
        // Use expected size roughly midrange.
        float lo = Mathf.Max(1, minCells);
        float hi = Mathf.Max(lo, maxCells);
        float expectedSize = (lo + hi) * 0.5f;

        // Approx average bbox side in cells ~ sqrt(expectedSize) + 1
        float approxSide = Mathf.Sqrt(expectedSize) + 1f;

        // Each piece consumes approx bbox area + gap; pack into square
        float approxCellsPerPiece = approxSide + gapCells;

        float sideCells = Mathf.Ceil(Mathf.Sqrt(spawnCount) * approxCellsPerPiece) + paletteCellPadding;
        sideCells = Mathf.Max(sideCells, 4f); // minimum sanity

        float sideWorld = sideCells * cell;

        // Unity Plane is 10x10 units when localScale is (1,1,1)
        Transform t = paletteRenderer.transform;
        float scale = sideWorld / 10f;

        t.localScale = new Vector3(scale, t.localScale.y, scale);
    }

    // --- Build the visual/collider representation on the palette ---
    GameObject BuildPaletteDisplay(Vector2Int[] offsets, float anchorX, float anchorZ, float cell, int layerIndex)
    {
        var root = new GameObject("PalettePolyomino");
        root.transform.SetParent(paletteRoot, worldPositionStays: true);

        // Build tiles and compute bounds for root collider
        Bounds? bb = null;
        float y = paletteRenderer.bounds.max.y + yOffset;

        foreach (var o in offsets)
        {
            float wx = anchorX + (o.x + 0.5f) * cell;
            float wz = anchorZ + (o.y + 0.5f) * cell;

            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "Tile";
            tile.transform.SetParent(root.transform, worldPositionStays: true);
            tile.transform.position = new Vector3(wx, y, wz);
            tile.transform.localScale = new Vector3(cell * 0.92f, cell * 0.2f, cell * 0.92f);

            var r = tile.GetComponent<Renderer>();
            if (r && paletteDefaultMat) r.sharedMaterial = paletteDefaultMat;

            // remove per-tile collider; we use one on root
            var c = tile.GetComponent<Collider>();
            if (c) Destroy(c);

            var trb = new Bounds(tile.transform.position, tile.transform.localScale);
            bb = bb.HasValue ? Encapsulate(bb.Value, trb) : trb;
        }

        // One collider for easy clicking
        var col = root.AddComponent<BoxCollider>();
        if (bb.HasValue)
        {
            var bounds = bb.Value;
            col.center = root.transform.InverseTransformPoint(bounds.center);
            col.size = bounds.size + new Vector3(0f, 0.3f, 0f);
        }

        // Layer assignment (so ModeController can raycast only Palette pieces)
        if (layerIndex >= 0)
            SetLayerRecursively(root, layerIndex);

        return root;
    }

    static void GetNormalizedBoundsWH(Vector2Int[] offsets, out int w, out int h)
    {
        // offsets are normalized (min at 0,0)
        int maxX = 0, maxY = 0;
        foreach (var o in offsets)
        {
            if (o.x > maxX) maxX = o.x;
            if (o.y > maxY) maxY = o.y;
        }
        w = maxX + 1;
        h = maxY + 1;
    }

    static Bounds Encapsulate(Bounds a, Bounds b) { a.Encapsulate(b.min); a.Encapsulate(b.max); return a; }

    void Clear()
    {
        foreach (var go in spawned)
            if (go) Destroy(go);
        spawned.Clear();

        if (paletteRoot)
        {
            for (int i = paletteRoot.childCount - 1; i >= 0; i--)
                Destroy(paletteRoot.GetChild(i).gameObject);
        }
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }
}
