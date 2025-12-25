using System.Collections.Generic;
using UnityEngine;

public class ScoringManager : MonoBehaviour
{
    [Header("Refs")]
    public BoardState state;
    public BoardGrid grid;
    public PlayersSettings players;

    [Header("Runtime Overlay")]
    public bool autoEvaluateOnStart = false;
    public KeyCode toggleOverlayKey = KeyCode.F8;
    public Material overlayMaterial; // should be transparent; if null, default cube material is used
    [Range(0f, 1f)] public float overlayAlphaFree = 0.25f;
    [Range(0f, 1f)] public float overlayAlphaCaptured = 0.5f;
    public float overlayY = 0.02f;

    public ScoreResult LastResult { get; private set; }

    static readonly Vector2Int[] N4 =
    {
        new Vector2Int( 1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int( 0, 1),
        new Vector2Int( 0,-1),
    };

    Transform overlayRoot;
    readonly List<Renderer> overlayTiles = new();
    MaterialPropertyBlock mpb;
    readonly List<GameObject> hiddenPieces = new();
    bool overlayActive = false;

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
        if (Input.GetKeyDown(toggleOverlayKey))
        {
            overlayActive = !overlayActive;
            if (overlayActive)
            {
                EvaluateScores();
                HidePieceViews();
                BuildOverlayTiles(LastResult);
                LogRanking(LastResult);
            }
            else
            {
                ShowPieceViews();
                HideOverlayTiles();
            }
        }
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
                    result.playerScores[bestOwner] += 1;
                    result.cellOwner[c.x, c.y] = bestOwner;
                }
            }
        }

        return result;
    }

    void BuildOverlayTiles(ScoreResult res)
    {
        if (!grid || grid.settings == null || res.cellOwner == null) return;

        int n = grid.settings.N;
        float s = grid.cellSize * 0.98f;

        EnsureOverlayRoot();
        int needed = CountActiveCells(res);
        EnsureOverlayTiles(needed);

        int idx = 0;
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                int owner = res.cellOwner[x, y];
                if (owner < 0) continue;

                bool captured = res.enclosedMask[x, y];
                Color c = players ? players.GetUIColor(owner, Color.white) : Color.white;
                c.a = captured ? overlayAlphaCaptured : overlayAlphaFree;

                var rend = overlayTiles[idx];
                rend.transform.position = grid.CellToWorld(x, y, overlayY);
                rend.transform.localScale = new Vector3(s, s * 0.05f, s);

                mpb.Clear();
                if (rend.sharedMaterial && rend.sharedMaterial.HasProperty("_BaseColor"))
                    mpb.SetColor("_BaseColor", c);
                else if (rend.sharedMaterial && rend.sharedMaterial.HasProperty("_Color"))
                    mpb.SetColor("_Color", c);
                rend.SetPropertyBlock(mpb);

                rend.gameObject.SetActive(true);
                idx++;
            }

        // deactivate unused tiles
        for (int i = idx; i < overlayTiles.Count; i++)
            overlayTiles[i].gameObject.SetActive(false);
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

    void EnsureOverlayRoot()
    {
        if (overlayRoot != null) return;
        var go = new GameObject("ScoreOverlay");
        overlayRoot = go.transform;
        overlayRoot.SetParent(transform, worldPositionStays: true);
    }

    void EnsureOverlayTiles(int needed)
    {
        while (overlayTiles.Count < needed)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"OverlayTile_{overlayTiles.Count}";
            go.transform.SetParent(overlayRoot, worldPositionStays: true);

            var col = go.GetComponent<Collider>();
            if (col) Destroy(col);

            var rend = go.GetComponent<Renderer>();
            if (overlayMaterial) rend.sharedMaterial = overlayMaterial;

            go.SetActive(false);
            overlayTiles.Add(rend);
        }
    }

    void HideOverlayTiles()
    {
        foreach (var r in overlayTiles)
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
}

public struct ScoreResult
{
    public int[] playerScores;
    public int[,] cellOwner;     // owner who gets the point for this cell (-1 if none)
    public bool[,] enclosedMask; // true if cell is enclosed (piece or empty)
    public bool[,] isPieceCell;
}
