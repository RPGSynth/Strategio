using System.Collections.Generic;
using UnityEngine;

public class PlacementController : MonoBehaviour
{
    public BoardGrid board;
    public BoardState state;

    [Header("Current piece")]
    public PieceSettings currentPiece;

    [Header("Preview")]
    public Transform previewRoot;     // empty object in scene
    public float previewY = 0.02f;

    int rot90 = 0;
    bool flip = false;

    readonly List<Transform> previewTiles = new();

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
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
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
        bool canPlace = state.CanPlace(currentPiece, anchor, rot90, flip, out List<Vector2Int> covered);

        // Draw preview
        DrawPreview(covered, canPlace);

        // Click to place
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            int id = state.Place(currentPiece, anchor, rot90, flip, out var placedCells);

            // Spawn a placed piece visual
            var go = new GameObject($"Piece_{id}_{currentPiece.name}");
            var view = go.AddComponent<PieceView>();
            view.board = board;
            view.y = previewY;
            view.Build(placedCells);
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
            r.material.color = Color.cyan;
            if (col) Destroy(col);

            previewTiles.Add(t);
        }
    }

    void SetPreviewActive(bool active)
    {
        if (previewRoot) previewRoot.gameObject.SetActive(active);
    }
}
