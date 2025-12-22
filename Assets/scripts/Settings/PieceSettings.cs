using UnityEngine;

[CreateAssetMenu(menuName = "Board Game/Piece Definition", fileName = "PieceDefinition")]
public class PieceSettings : ScriptableObject
{
    [Min(1)] public int maxTiles = 5;

    [Tooltip("Cells covered relative to the anchor cell (0,0). Example L: (0,0),(1,0),(0,1)")]
    public Vector2Int[] offsets = { Vector2Int.zero };

    public bool allowRotate = true;
    public bool allowFlip = false;

    void OnValidate()
    {
        if (offsets == null) offsets = new[] { Vector2Int.zero };
        if (offsets.Length < 1) offsets = new[] { Vector2Int.zero };
        if (offsets.Length > maxTiles)
        {
            // keep it simple: trim if someone adds too many
            System.Array.Resize(ref offsets, maxTiles);
        }
    }
}
