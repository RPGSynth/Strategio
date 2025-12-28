using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ScoringManager : MonoBehaviour
{
    [Header("Refs")]
    public BoardState state;
    public BoardGrid grid;
    public PlayersSettings players;
    public TurnManager turn;
    public PlacementController placement;

    [Header("Territory View (Overlay)")]
    public bool autoEvaluateOnStart = false;
    [FormerlySerializedAs("toggleOverlayKey")] public KeyCode toggleTerritoryKey = KeyCode.F8;
    [FormerlySerializedAs("overlayTilePrefab")] public GameObject territoryTilePrefab;
    [FormerlySerializedAs("overlayTileScale"), Range(0.1f, 3f)] public float territoryTileScale = 1f;
    [FormerlySerializedAs("overlayY")] public float territoryY = 0.02f;
    [Range(0f, 1f)] public float territoryCapturedAlpha = 0.5f;
    [Header("Debug")]
    [Tooltip("Enable detailed logs for territory overlay material/color application.")]
    public bool debugOverlayLogs = false;
    [Range(1, 64)] public int debugOverlayTileLimit = 12;

    public ScoreResult LastResult { get; private set; }

    static readonly Vector2Int[] N4 =
    {
        new Vector2Int( 1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int( 0, 1),
        new Vector2Int( 0,-1),
    };

    Transform territoryRoot;
    readonly List<Renderer> territoryTiles = new();
    MaterialPropertyBlock mpb;
    static Material fallbackTerritoryMat;
    readonly List<GameObject> hiddenPieces = new();
    bool territoryActive = false;
    bool territoryPendingAdvance = false;

    const float PieceTilePadding = 0.92f;   // matches PieceView default padding
    const float PieceHeightFactor = 0.2f;   // matches PieceView height factor
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int ColorAId = Shader.PropertyToID("_ColorA");
    static readonly int ColorBId = Shader.PropertyToID("_ColorB");

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    void Start()
    {
        if (autoEvaluateOnStart)
            EvaluateScores();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleTerritoryKey))
        {
            // Debug key: allow unconditional view (without consuming piece/turn)
            if (!territoryActive)
            {
                EvaluateScores();
                HidePieceViews();
                BuildTerritoryTiles(LastResult);
            }
            else
            {
                ShowPieceViews();
                HideTerritoryTiles();
            }
            territoryActive = !territoryActive;
            territoryPendingAdvance = false;
        }
    }

    // Hook this to a UI button to reveal/hide the winning state. Consumes the held piece.
    public void ToggleTerritoryByButton()
    {
        ToggleTerritory();
    }

    public ScoreResult EvaluateScores()
    {
        LastResult = ComputeScores();
        return LastResult;
    }

    ScoreResult ComputeScores()
    {
        var result = new ScoreResult();
        if (!state || !grid || grid.settings == null || players == null)
            return result;

        int n = grid.settings.N;
        int playerCount = Mathf.Max(1, players.Count);
        result.playerScores = new int[playerCount];
        result.cellOwner = new int[n, n];
        result.enclosedMask = new bool[n, n];
        result.isPieceCell = new bool[n, n];

        // Snapshot occupancy
        int[,] occ = new int[n, n];
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                occ[x, y] = state.GetPieceIdAtCell(new Vector2Int(x, y));
                if (occ[x, y] > 0) result.isPieceCell[x, y] = true;
                result.cellOwner[x, y] = -1;
            }

        // 1) Flood free empty cells from border (pieces are walls)
        bool[,] freeEmpty = new bool[n, n];
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        void EnqueueIfEmpty(int x, int y)
        {
            if (occ[x, y] != -1) return;
            if (freeEmpty[x, y]) return;
            freeEmpty[x, y] = true;
            q.Enqueue(new Vector2Int(x, y));
        }

        for (int x = 0; x < n; x++)
        {
            EnqueueIfEmpty(x, 0);
            EnqueueIfEmpty(x, n - 1);
        }
        for (int y = 0; y < n; y++)
        {
            EnqueueIfEmpty(0, y);
            EnqueueIfEmpty(n - 1, y);
        }

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            foreach (var d in N4)
            {
                int nx = c.x + d.x;
                int ny = c.y + d.y;
                if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                EnqueueIfEmpty(nx, ny);
            }
        }

        // 2) Determine enclosed pieces (no contact with freeEmpty or edge)
        bool[,] enclosedMask = new bool[n, n]; // true if enclosed (piece or empty)
        bool IsOutside(int x, int y) => x < 0 || y < 0 || x >= n || y >= n;

        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                if (occ[x, y] == -1)
                {
                    if (!freeEmpty[x, y])
                        enclosedMask[x, y] = true; // enclosed empty
                    continue;
                }

                bool touchesFree = false;
                foreach (var d in N4)
                {
                    int nx = x + d.x;
                    int ny = y + d.y;
                    if (IsOutside(nx, ny))
                    {
                        touchesFree = true;
                        break;
                    }
                    if (freeEmpty[nx, ny])
                    {
                        touchesFree = true;
                        break;
                    }
                }

                enclosedMask[x, y] = !touchesFree;
                result.enclosedMask[x, y] = enclosedMask[x, y];
            }
        }

        // 3) Score free piece cells directly
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                if (occ[x, y] <= 0) continue;
                if (enclosedMask[x, y]) continue;
                if (state.TryGetOwner(occ[x, y], out int owner))
                {
                    owner = Mathf.Clamp(owner, 0, playerCount - 1);
                    result.playerScores[owner] += 1;
                    result.cellOwner[x, y] = owner;
                }
            }
        }

        // 4) Process enclosed components (empty + enclosed pieces)
        bool[,] visited = new bool[n, n];

        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                if (!enclosedMask[x, y] || visited[x, y]) continue;

                List<Vector2Int> cells = new List<Vector2Int>();
                Dictionary<int, int> ownerBoundaryCounts = new Dictionary<int, int>();
                HashSet<Vector2Int> countedBoundaryCells = new HashSet<Vector2Int>();

                // BFS over enclosed cells
                Queue<Vector2Int> cq = new Queue<Vector2Int>();
                cq.Enqueue(new Vector2Int(x, y));
                visited[x, y] = true;

                while (cq.Count > 0)
                {
                    var c = cq.Dequeue();
                    cells.Add(c);

                    foreach (var d in N4)
                    {
                        int nx = c.x + d.x;
                        int ny = c.y + d.y;
                        if (IsOutside(nx, ny))
                            continue;

                        if (enclosedMask[nx, ny])
                        {
                            if (!visited[nx, ny])
                            {
                                visited[nx, ny] = true;
                                cq.Enqueue(new Vector2Int(nx, ny));
                            }
                        }
                        else
                        {
                            // boundary: count piece cells outside this enclosed region
                            if (occ[nx, ny] > 0)
                            {
                                var boundaryCell = new Vector2Int(nx, ny);
                                if (countedBoundaryCells.Add(boundaryCell))
                                {
                                    if (state.TryGetOwner(occ[nx, ny], out int owner))
                                    {
                                        if (!ownerBoundaryCounts.ContainsKey(owner))
                                            ownerBoundaryCounts[owner] = 0;
                                        ownerBoundaryCounts[owner] += 1;
                                    }
                                }
                            }
                        }
                    }
                }

                // Determine majority boundary owner
                int bestOwner = -1;
                int bestCount = -1;
                bool tie = false;
                foreach (var kvp in ownerBoundaryCounts)
                {
                    if (kvp.Value > bestCount)
                    {
                        bestCount = kvp.Value;
                        bestOwner = kvp.Key;
                        tie = false;
                    }
                    else if (kvp.Value == bestCount)
                    {
                        tie = true;
                    }
                }

                if (bestOwner < 0 || tie) continue; // no boundary owner or tie: nobody claims enclosed cells

                bestOwner = Mathf.Clamp(bestOwner, 0, playerCount - 1);

                foreach (var c in cells)
                {
                    // If this is an empty cell, award it to the boundary owner.
                    // If this is a piece cell, it is captured (removed from original owner) but not awarded to the captor.
                    if (!result.isPieceCell[c.x, c.y])
                    {
                        result.playerScores[bestOwner] += 1;
                        result.cellOwner[c.x, c.y] = bestOwner;
                    }
                    else
                    {
                        // Preserve original owner color for overlay, but no score for anyone.
                        if (state.TryGetOwner(occ[c.x, c.y], out int pieceOwner))
                            result.cellOwner[c.x, c.y] = Mathf.Clamp(pieceOwner, 0, playerCount - 1);
                        else
                            result.cellOwner[c.x, c.y] = -1;
                    }

                    result.enclosedMask[c.x, c.y] = true;
                }
            }
        }

        return result;
    }

    void ToggleTerritory()
    {
        if (!territoryActive)
        {
            // Turning ON: require a held piece
            if (!placement || !placement.HasPiece) return;

            EvaluateScores();
            HidePieceViews();
            BuildTerritoryTiles(LastResult);
            LogRanking(LastResult);

            territoryActive = true;
            territoryPendingAdvance = true;
            placement.ConsumeCurrentPiece();
        }
        else
        {
            // Turning OFF
            territoryActive = false;
            ShowPieceViews();
            HideTerritoryTiles();

            if (territoryPendingAdvance && turn != null)
                turn.NextTurn();

            territoryPendingAdvance = false;
        }
    }

    void BuildTerritoryTiles(ScoreResult res)
    {
        if (!grid || grid.settings == null || res.cellOwner == null) return;

        int n = grid.settings.N;
        // Match board piece visuals; compute same scale as PieceView (uses padding 0.92 and height factor 0.2).
        int logRemaining = debugOverlayLogs ? debugOverlayTileLimit : 0;

        EnsureTerritoryRoot();
        int needed = CountActiveCells(res);
        EnsureTerritoryTiles(needed);

        // Log ranking in territory mode so we can see who is leading.
        LogRanking(res);

        int idx = 0;
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                int owner = res.cellOwner[x, y];
                if (owner < 0) continue;

                var rend = territoryTiles[idx];
                // Use the owner's placed material so the overlay matches board visuals.
                rend.sharedMaterial = GetOverlayMaterialForOwner(owner, rend.sharedMaterial);

                rend.transform.position = grid.CellToWorld(x, y, territoryY);
                float s = grid.cellSize * PieceTilePadding;
                float pieceScale = placement ? placement.boardTileScale : 1f;
                Vector3 baseScale = new Vector3(s * pieceScale, s * PieceHeightFactor * pieceScale, s * pieceScale);
                rend.transform.localScale = baseScale * territoryTileScale;
                bool captured = res.enclosedMask[x, y];
                float alpha = captured ? territoryCapturedAlpha : 1f;
                ApplyOverlayPropertyBlock(rend, alpha);

                if (debugOverlayLogs && logRemaining >= 0)
                {
                    var mat = rend ? rend.sharedMaterial : null;
                    string matName = mat ? mat.name : "null";
                    Debug.Log($"[Overlay] Tile idx={idx} cell=({x},{y}) owner={owner} mat={matName} rq={(mat ? mat.renderQueue : -1)}");
                }

                rend.gameObject.SetActive(true);
                idx++;
            }

        // deactivate unused tiles
        for (int i = idx; i < territoryTiles.Count; i++)
            territoryTiles[i].gameObject.SetActive(false);
    }

    int CountActiveCells(ScoreResult res)
    {
        int n = res.cellOwner.GetLength(0);
        int count = 0;
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
                if (res.cellOwner[x, y] >= 0) count++;
        return count;
    }

    void EnsureTerritoryRoot()
    {
        if (territoryRoot != null) return;
        var go = new GameObject("ScoreOverlay");
        territoryRoot = go.transform;
        territoryRoot.SetParent(transform, worldPositionStays: true);
    }

    void EnsureTerritoryTiles(int needed)
    {
        while (territoryTiles.Count < needed)
        {
            GameObject prefab = territoryTilePrefab ? territoryTilePrefab : placement ? placement.boardTilePrefab : null;
            GameObject go = prefab ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(territoryRoot, worldPositionStays: false);
            go.name = $"OverlayTile_{territoryTiles.Count}";

            var col = go.GetComponent<Collider>();
            if (col) Destroy(col);

            var rend = go.GetComponent<Renderer>();
            // Start with a valid material from the prefab/board; avoid custom-created fallbacks.
            if (!rend.sharedMaterial)
            {
                var srcMat = GetOverlayMaterialForOwner(-1, null);
                if (srcMat) rend.sharedMaterial = srcMat;
            }

            go.SetActive(false);
            territoryTiles.Add(rend);
        }
    }

    void HideTerritoryTiles()
    {
        foreach (var r in territoryTiles)
            if (r) r.gameObject.SetActive(false);
    }

    void HidePieceViews()
    {
        hiddenPieces.Clear();
        var pieces = FindObjectsOfType<PieceView>(includeInactive: false);
        foreach (var pv in pieces)
        {
            if (!pv || !pv.gameObject.activeSelf) continue;
            hiddenPieces.Add(pv.gameObject);
            pv.gameObject.SetActive(false);
        }
    }

    void ShowPieceViews()
    {
        foreach (var go in hiddenPieces)
            if (go) go.SetActive(true);
        hiddenPieces.Clear();
    }

    public bool IsOverlayActive => territoryActive;

    Material GetOverlayMaterialForOwner(int ownerIndex, Material fallbackSource)
    {
        Material source = null;
        if (players && ownerIndex >= 0 && ownerIndex < players.Count)
            source = players.GetPlacedMat(ownerIndex);

        if (!source) source = fallbackSource;
        if (!source && placement && placement.boardTilePrefab)
        {
            var r = placement.boardTilePrefab.GetComponent<Renderer>();
            if (r) source = r.sharedMaterial;
        }
        if (!source && territoryTilePrefab)
        {
            var r = territoryTilePrefab.GetComponent<Renderer>();
            if (r) source = r.sharedMaterial;
        }

        // Do not change blend state: use the exact player material so visuals match board mode.
        return source;
    }

    void LogRanking(ScoreResult res)
    {
        if (res.playerScores == null || players == null) return;
        int count = res.playerScores.Length;
        var list = new List<(int player, int score)>(count);
        for (int i = 0; i < count; i++)
            list.Add((i, res.playerScores[i]));

        list.Sort((a, b) =>
        {
            int cmp = b.score.CompareTo(a.score);
            return cmp != 0 ? cmp : a.player.CompareTo(b.player);
        });

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("[Scoring] Ranking: ");
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append(" | ");
            string name = players.GetName(list[i].player);
            sb.Append($"{i + 1}) {name}: {list[i].score}");
        }
        Debug.Log(sb.ToString());
    }

    void ApplyOverlayPropertyBlock(Renderer rend, float alpha)
    {
        if (!rend)
            return;

        mpb.Clear();
        var mat = rend.sharedMaterial;
        bool applied = false;

        // If the shader exposes gradient endpoints, flatten them to match the board base hue and set alpha.
        if (mat && mat.HasProperty(ColorAId) && mat.HasProperty(ColorBId))
        {
            // Flatten by using B as the canonical color: ColorA = ColorB in overlay.
            Color b = mat.GetColor(ColorBId);
            b.a = alpha;
            mpb.SetColor(ColorAId, b);
            mpb.SetColor(ColorBId, b);
            applied = true;
        }
        else if (mat && mat.HasProperty(ColorBId))
        {
            Color b = mat.GetColor(ColorBId);
            b.a = alpha;
            mpb.SetColor(ColorBId, b);
            applied = true;
        }
        else if (mat && mat.HasProperty(ColorAId))
        {
            Color a = mat.GetColor(ColorAId);
            a.a = alpha;
            mpb.SetColor(ColorAId, a);
            applied = true;
        }

        // Also set common tint slots' alpha so transparency is respected.
        if (mat && mat.HasProperty(ColorId))
        {
            Color c = mat.GetColor(ColorId);
            c.a = alpha;
            mpb.SetColor(ColorId, c);
            applied = true;
        }

        if (mat && mat.HasProperty(BaseColorId))
        {
            Color c = mat.GetColor(BaseColorId);
            c.a = alpha;
            mpb.SetColor(BaseColorId, c);
            applied = true;
        }

        if (applied)
            rend.SetPropertyBlock(mpb);
        else
            rend.SetPropertyBlock(null);
    }
}

public struct ScoreResult
{
    public int[] playerScores;
    public int[,] cellOwner;     // owner who gets the point for this cell (-1 if none)
    public bool[,] enclosedMask; // true if cell is enclosed (piece or empty)
    public bool[,] isPieceCell;
}
