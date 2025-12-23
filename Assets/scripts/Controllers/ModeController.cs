using UnityEngine;

public class ModeController : MonoBehaviour
{
    [Header("Refs")]
    public TopDownCameraController camController;
    public PlacementController placement;      // your existing placement script
    public Renderer boardRenderer;             // Renderer of the board plane (the real playable surface)
    public Renderer paletteRenderer;           // Renderer of the palette plane

    [Header("Camera margins")]
    public float boardViewMargin = 0f;
    public float paletteViewMargin = 0f;

    [Header("Palette picking")]
    public LayerMask palettePickMask = ~0;     // set to Palette layer ideally
    public bool autoCloseOnPick = false;

    bool paletteMode = false;

    public void TogglePaletteMode()
    {
        paletteMode = !paletteMode;

        if (paletteMode)
            EnterPaletteMode();
        else
            EnterBoardMode();
    }

    void EnterPaletteMode()
    {
        if (placement) placement.enabled = false; // prevents placing while picking
        if (camController) camController.clampToBoard = false; // <-- allow travel

        if (camController && paletteRenderer)
        {
            float fit = camController.ComputeFitOrthoForRenderer(paletteRenderer, paletteViewMargin);
            camController.SetTargetView(paletteRenderer.bounds.center, fit);
        }
    }

    void EnterBoardMode()
    {
        if (placement) placement.enabled = true;
        if (camController) camController.clampToBoard = false; 
        

        if (camController && boardRenderer)
        {
            float fit = camController.ComputeFitOrthoForRenderer(boardRenderer, boardViewMargin);
            camController.SetTargetView(boardRenderer.bounds.center, fit);
        }
    }

    void Update()
    {
        if (!paletteMode) return;

        // Click on palette pieces to select them
        if (Input.GetMouseButtonDown(0))
        {
            var cam = Camera.main;
            if (!cam) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, palettePickMask))
            {
                var pp = hit.collider.GetComponentInParent<PalettePiece>();
                if (pp && pp.piece && placement)
                {
                    placement.currentPiece = pp.piece;
                    Debug.Log($"Selected piece: {pp.piece.name}");

                    if (autoCloseOnPick)
                        TogglePaletteMode();
                }
            }
        }
    }
}
