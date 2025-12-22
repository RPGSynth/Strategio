using UnityEngine;
using System.Collections.Generic;

public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        // 1. Find or create PieceManager
        var pm = FindObjectOfType<PieceManager>();
        if (!pm)
        {
            var go = new GameObject("PieceManager");
            pm = go.AddComponent<PieceManager>();

            // Try to load default pieces if none exist (this part is tricky at runtime without Resources,
            // but we can try to find them if they were referenced elsewhere or just leave it empty
            // and let the user populate it via inspector in a real scenario.
            // For this bootstrap, we'll try to populate if we can find settings in Resources,
            // but we don't have a Resources folder.
            // So we'll rely on the scene setup or manual assignment.
            // However, to make it playable "out of the box" if I could run it:
            // I'll create dummy pieces if the bag is empty.)
        }

        // 2. Find PlacementController and link PieceManager
        var pc = FindObjectOfType<PlacementController>();
        if (pc)
        {
            pc.pieceManager = pm;
        }
        else
        {
            Debug.LogError("No PlacementController found in scene!");
        }

        // 3. Find or create GameUI
        var ui = FindObjectOfType<GameUI>();
        if (!ui)
        {
            var go = new GameObject("GameUI");
            ui = go.AddComponent<GameUI>();
            ui.pieceManager = pm;
            ui.placementController = pc;
        }
        else
        {
            // Ensure links
            if (!ui.pieceManager) ui.pieceManager = pm;
            if (!ui.placementController) ui.placementController = pc;
        }

        // 4. Populate bag if empty (for testing)
        if (pm.initialBagPieces.Count == 0 && pm.AvailablePieces.Count == 0)
        {
            // Create a dummy piece
            var piece = ScriptableObject.CreateInstance<PieceSettings>();
            piece.name = "L-Shape (Generated)";
            piece.offsets = new Vector2Int[] {
                Vector2Int.zero,
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, 2)
            };
            piece.allowRotate = true;
            piece.allowFlip = true;

            pm.AddPiece(piece);

            var piece2 = ScriptableObject.CreateInstance<PieceSettings>();
            piece2.name = "Dot (Generated)";
            piece2.offsets = new Vector2Int[] { Vector2Int.zero };

            pm.AddPiece(piece2);
        }
    }
}
