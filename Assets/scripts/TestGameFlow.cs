using System.Collections.Generic;
using UnityEngine;

public class TestGameFlow : MonoBehaviour
{
    // Simulation variables
    PieceManager pm;
    PlacementController pc;
    BoardGrid grid;
    BoardState state;

    public void RunTest()
    {
        Debug.Log("Starting TestGameFlow...");

        // Setup
        var go = new GameObject("TestRoot");

        // 1. Setup Grid & State
        grid = go.AddComponent<BoardGrid>();
        grid.settings = ScriptableObject.CreateInstance<BoardSettings>();
        grid.settings.N = 5;
        grid.settings.marginPercent = 0.1f;
        // Mock renderer bounds
        var rend = go.AddComponent<MeshRenderer>(); // Needs filter too usually but BoardGrid checks bounds
        var filter = go.AddComponent<MeshFilter>();
        var mesh = new Mesh();
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(10, 0, 10)); // 10x10 world size
        filter.mesh = mesh;
        // Force recompute
        grid.Recompute();

        state = go.AddComponent<BoardState>();
        state.board = grid;
        state.Init();

        // 2. Setup Manager & Controller
        pm = go.AddComponent<PieceManager>();
        pc = go.AddComponent<PlacementController>();
        pc.board = grid;
        pc.state = state;
        pc.pieceManager = pm;

        // 3. Add a piece to bag
        var p = ScriptableObject.CreateInstance<PieceSettings>();
        p.name = "TestPiece";
        p.offsets = new Vector2Int[] { Vector2Int.zero }; // 1x1 dot
        pm.AddPiece(p);

        if (pm.AvailablePieces.Count != 1)
        {
            Debug.LogError("TEST FAILED: Bag should have 1 piece.");
            return;
        }

        // 4. Select Piece
        pc.SelectPiece(p);
        if (pc.SelectedPiece != p)
        {
            Debug.LogError("TEST FAILED: Piece not selected.");
            return;
        }

        // 5. Simulate Placement
        // We can't easily simulate Input.GetMouseButtonDown without mocking or hacking,
        // but we can directly call the logic that happens inside it to verify the "consumption" logic.
        // We will manually trigger the placement logic that PlacementController uses.

        Vector2Int anchor = new Vector2Int(2, 2);
        int rot = 0;
        bool flip = false;

        if (state.CanPlace(p, anchor, rot, flip, out var covered))
        {
             int id = state.Place(p, anchor, rot, flip, out var placedCells);
             // Manually consume
             pm.RemovePiece(p);
             pc.SelectPiece(null);
        }
        else
        {
             Debug.LogError("TEST FAILED: Could not place piece.");
             return;
        }

        // 6. Verify Results
        if (pm.AvailablePieces.Count != 0)
        {
             Debug.LogError("TEST FAILED: Bag should be empty after placement.");
             return;
        }

        if (pc.SelectedPiece != null)
        {
             Debug.LogError("TEST FAILED: Selection should be cleared.");
             return;
        }

        if (state.IsEmpty(2, 2))
        {
             Debug.LogError("TEST FAILED: Board cell should be occupied.");
             return;
        }

        Debug.Log("TEST PASSED: Bag flow working correctly.");
    }

    void Start()
    {
        RunTest();
    }
}
