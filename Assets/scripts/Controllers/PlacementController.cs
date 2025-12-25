using System.Collections.Generic;
using UnityEngine;

public class PlacementController : MonoBehaviour
{
    public BoardGrid board;
    public BoardState state;
    public LayerMask boardMask; 

    [Header("Materials")]
    public Material previewValidMat;
    public Material previewInvalidMat;
    public Material placedMat;

    [Header("Player")]
    public TurnManager turn;
    public PlayersSettings players;
    int placementCounter = 1;

    [Header("Current piece")]
    public PieceSettings currentPiece;

    [Header("Preview")]
    public Transform previewRoot;     // empty object in scene
    public float previewY = 0.02f;
    [Range(0f, 1f)] public float previewPlayerAlpha = 0.25f;

    int rot90 = 0;
    bool flip = false;

    readonly Dictionary<int, GameObject> placedVisuals = new();
    readonly List<Transform> previewTiles = new();
    MaterialPropertyBlock previewBlock;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    void Update()
    {
        if (!board || !state || !currentPiece) return;

        // Rotate / flip controls (optional, but handy)
        if (currentPiece.allowRotate)
        {
            if (Input.GetKeyDown(KeyCode.E)) rot90 = (rot90 + 1) % 4;
            if (Input.GetKeyDown(KeyCode.Q)) rot90 = (rot90 + 3) % 4;
        }
        if (currentPiece.allowFlip && Input.GetKeyDown(KeyCode.F))
            flip = !flip;

        // Raycast to board
        var cam = Camera.main;
        if (!cam) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, boardMask))
        {
            SetPreviewActive(false);
            return;
        }

        if (!board.WorldToCell(hit.point, out var anchor))
        {
            SetPreviewActive(false);
            return;
        }

        // Compute covered cells
        bool canPlace = state.CanPlace(currentPiece, anchor, rot90, flip, out var covered);

        // Filter for drawing only
        var drawable = new List<Vector2Int>(covered.Count);
        foreach (var c in covered)
            if (board.InBounds(c.x, c.y))
                drawable.Add(c);

        // Draw preview
        DrawPreview(covered, canPlace);

        // Click to place
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            int owner = (turn && turn.PlayerCount > 0) ? turn.CurrentPlayer : 0;

            int id = state.Place(currentPiece, anchor, rot90, flip, owner, placementCounter, out var placedCells);
            if (id == -1) return;
            placementCounter++;

            var go = new GameObject($"Piece_{id}_{currentPiece.name}");
            placedVisuals[id] = go;

            var view = go.AddComponent<PieceView>();
            view.board = board;
            view.y = previewY;
            if (players) view.placedMat = players.GetPlacedMat(owner);
            view.Build(placedCells);

            if (turn) turn.NextTurn();
        }

        if (Input.GetMouseButtonDown(1))
        {
            int pieceId = state.GetPieceIdAtCell(anchor);
            if (pieceId != -1)
            {
                if (state.Remove(pieceId))
                {
                    if (placedVisuals.TryGetValue(pieceId, out var go))
                    {
                        Destroy(go);
                        placedVisuals.Remove(pieceId);
                    }
                    if (turn) turn.NextTurn();
                }
            }
        }
    }

    void DrawPreview(List<Vector2Int> covered, bool canPlace)
    {
        if (!previewRoot) return;

        EnsurePreviewTiles(covered.Count);

        float s = board.cellSize * 0.92f;
        for (int i = 0; i < previewTiles.Count; i++)
        {
            bool active = i < covered.Count;
            previewTiles[i].gameObject.SetActive(active);
            if (!active) continue;

            previewTiles[i].position = board.CellToWorld(covered[i].x, covered[i].y, previewY);
            previewTiles[i].localScale = new Vector3(s, s * 0.15f, s);

            // simple feedback: higher/lower alpha look would require a material.
            // For now: move slightly higher if invalid
            previewTiles[i].position += canPlace ? Vector3.zero : new Vector3(0f, 0.01f, 0f);


            // Change material depending on place status
            var r = previewTiles[i].GetComponent<Renderer>();
            if (r)
            {
                if (canPlace)
                {
                    bool usePlayerMat = TryGetPlayerPreviewMaterial(out var validMat);
                    r.sharedMaterial = usePlayerMat ? validMat : previewValidMat;
                    if (usePlayerMat)
                        ApplyPreviewAlpha(r, validMat);
                    else
                        r.SetPropertyBlock(null);
                }
                else
                {
                    r.sharedMaterial = previewInvalidMat;
                    r.SetPropertyBlock(null);
                }
            }
        }

        previewRoot.gameObject.SetActive(true);
    }

    void EnsurePreviewTiles(int needed)
    {
        while (previewTiles.Count < needed)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            t.SetParent(previewRoot, worldPositionStays: true);

            var col = t.GetComponent<Collider>();
            var r = t.GetComponent<Renderer>();
            if (col) Destroy(col);

            previewTiles.Add(t);
        }
    }

    void SetPreviewActive(bool active)
    {
        if (previewRoot) previewRoot.gameObject.SetActive(active);
    }

    bool TryGetPlayerPreviewMaterial(out Material mat)
    {
        mat = null;
        if (!players || !turn || players.Count == 0) return false;
        mat = players.GetPlacedMat(turn.CurrentPlayer);
        return mat != null;
    }

    void ApplyPreviewAlpha(Renderer r, Material mat)
    {
        if (!r || !mat)
        {
            if (r) r.SetPropertyBlock(null);
            return;
        }

        previewBlock ??= new MaterialPropertyBlock();
        previewBlock.Clear();

        if (mat.HasProperty(BaseColorId))
        {
            Color c = mat.GetColor(BaseColorId);
            c.a = previewPlayerAlpha;
            previewBlock.SetColor(BaseColorId, c);
            r.SetPropertyBlock(previewBlock);
            return;
        }

        if (mat.HasProperty(ColorId))
        {
            Color c = mat.GetColor(ColorId);
            c.a = previewPlayerAlpha;
            previewBlock.SetColor(ColorId, c);
            r.SetPropertyBlock(previewBlock);
            return;
        }

        r.SetPropertyBlock(null);
    }
}
