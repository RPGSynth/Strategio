using System.Collections.Generic;
using UnityEngine;

public class PieceView : MonoBehaviour
{

    public Material placedMat;
    public GameObject boardTilePrefab;
    [Range(0.1f, 1f)] public float boardTileScale = 0.5f;
    public BoardGrid board;
    public float y = 0.02f;          // height above board
    public float tilePadding = 0.92f; // 0.92 leaves a small gap between tiles

    readonly List<Transform> tiles = new();

    public void Build(List<Vector2Int> coveredCells)
    {
        Clear();

        if (!board) return;

        float s = board.cellSize * tilePadding;

        foreach (var c in coveredCells)
        {
            Transform t = boardTilePrefab ? Instantiate(boardTilePrefab).transform
                                           : GameObject.CreatePrimitive(PrimitiveType.Cube).transform;

            t.SetParent(transform, worldPositionStays: false);
            t.localScale = new Vector3(s * boardTileScale, s * 0.2f, s * boardTileScale);
            t.position = board.CellToWorld(c.x, c.y, y);

            // Remove collider so raycasts hit board, not pieces (optional)
            var col = t.GetComponent<Collider>();
            if (col) Destroy(col);
            
            var r = t.GetComponent<Renderer>();
            if (r && placedMat) r.sharedMaterial = placedMat;

            tiles.Add(t);
        }
    }

    void Clear()
    {
        for (int i = tiles.Count - 1; i >= 0; i--)
            if (tiles[i]) Destroy(tiles[i].gameObject);
        tiles.Clear();
    }

}
