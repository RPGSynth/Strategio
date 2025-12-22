using UnityEngine;

public class GameUI : MonoBehaviour
{
    public PieceManager pieceManager;
    public PlacementController placementController;

    private Vector2 scrollPosition;

    void OnGUI()
    {
        if (!pieceManager || !placementController) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, Screen.height - 20));
        GUILayout.Label("Available Pieces:");

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        var pieces = pieceManager.AvailablePieces;

        // Iterate backwards or use a copy if we were modifying the list,
        // but here we just select, so simple iteration is fine.
        // However, if multiple identical pieces exist, we need to handle that.
        // For UI, we might want to group them or just list them all.
        // I'll list them all for now.

        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            string name = p ? p.name : "Unknown Piece";

            // Highlight if selected
            bool isSelected = (placementController.SelectedPiece == p);
            string prefix = isSelected ? ">> " : "";

            if (GUILayout.Button(prefix + name))
            {
                placementController.SelectPiece(p);
            }
        }

        GUILayout.EndScrollView();

        if (placementController.SelectedPiece != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("Selected: " + placementController.SelectedPiece.name);
            GUILayout.Label("Press 'E'/'Q' to Rotate");
            GUILayout.Label("Press 'F' to Flip");
            if (GUILayout.Button("Deselect"))
            {
                placementController.SelectPiece(null);
            }
        }

        GUILayout.EndArea();
    }
}
