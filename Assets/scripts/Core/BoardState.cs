using System.Collections.Generic;
using UnityEngine;

public class BoardState : MonoBehaviour
{
    public BoardGrid board;

    // -1 = empty, otherwise pieceId
    int[,] occ;
    int nextPieceId = 1;

    // For undo / debugging: pieceId -> list of occupied cells
    readonly Dictionary<int, List<Vector2Int>> pieceCells = new();

    // pieceId -> playerIndex
    readonly Dictionary<int, int> pieceOwner = new(); // pieceId -> playerIndex


    void Awake()
    {
        Init();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        // Safe to re-init in editor if you want, but not required
    }
#endif

    public void Init()
    {
        if (!board || !board.settings) return;
        occ = new int[board.settings.N, board.settings.N];
        for (int x = 0; x < board.settings.N; x++)
            for (int y = 0; y < board.settings.N; y++)
                occ[x, y] = -1;
        pieceCells.Clear();
        nextPieceId = 1;
    }

    public bool IsEmpty(int x, int y) => occ[x, y] == -1;

    public bool CanPlace(PieceSettings def, Vector2Int anchor, int rot90, bool flip, out List<Vector2Int> covered)
    {
        covered = new List<Vector2Int>(def.offsets.Length);

        bool valid = true;

        foreach (var off in def.offsets)
        {
            Vector2Int t = TransformOffset(off, rot90, flip);
            Vector2Int cell = anchor + t;

            // Always add, even if invalid (so preview can draw it)
            covered.Add(cell);

            if (!board.InBounds(cell.x, cell.y))
            {
                valid = false;
                continue;
            }

            if (!IsEmpty(cell.x, cell.y))
            {
                valid = false;
            }
        }

        return valid;
    }

    public int Place(PieceSettings def, Vector2Int anchor, int rot90, bool flip, int ownerIndex, out List<Vector2Int> covered)
    {
        if (!CanPlace(def, anchor, rot90, flip, out covered))
            return -1;

        int id = nextPieceId++;
        pieceOwner[id] = ownerIndex;

        foreach (var c in covered)
            occ[c.x, c.y] = id;

        pieceCells[id] = covered;
        return id;
    }

    public bool Remove(int pieceId)
    {
        if (!pieceCells.TryGetValue(pieceId, out var cells)) return false;

        foreach (var c in cells)
            occ[c.x, c.y] = -1;

        pieceCells.Remove(pieceId);
        pieceOwner.Remove(pieceId);
        return true;
    }
    public int GetPieceIdAtCell(Vector2Int cell)
    {
        if (!board || !board.settings) return -1;
        if (!board.InBounds(cell.x, cell.y)) return -1;
        return occ[cell.x, cell.y];
    }

    static Vector2Int TransformOffset(Vector2Int o, int rot90, bool flip)
    {
        // flip horizontally in local piece space
        if (flip) o = new Vector2Int(-o.x, o.y);

        // rot90: 0,1,2,3 => 0°,90°,180°,270°
        rot90 = ((rot90 % 4) + 4) % 4;
        return rot90 switch
        {
            0 => o,
            1 => new Vector2Int(-o.y, o.x),
            2 => new Vector2Int(-o.x, -o.y),
            3 => new Vector2Int(o.y, -o.x),
            _ => o
        };
    }
    
    public bool TryGetOwner(int pieceId, out int ownerIndex) => pieceOwner.TryGetValue(pieceId, out ownerIndex);

}
