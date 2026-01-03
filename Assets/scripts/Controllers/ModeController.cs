using UnityEngine;

public class ModeController : MonoBehaviour
{
    [Header("Refs")]
    public TopDownCameraController camController;
    public PlacementController placement;      // your existing placement script
    public Renderer boardRenderer;             // Renderer of the board plane (the real playable surface)
    public Renderer paletteRenderer;           // Renderer of the palette plane
    public ScoringManager scoring;
    public GameObject paletteButton;           // optional: UI button to open palette

    [Header("Camera margins")]
    public float boardViewMargin = 0f;
    public float paletteViewMargin = 0f;

    [Header("Palette picking")]
    public LayerMask palettePickMask = ~0;     // set to Palette layer ideally
    public bool autoCloseOnPick = false;

    bool paletteMode = false;

    void Start()
    {
        // Ensure first view uses the same fit logic as runtime mode switches.
        paletteMode = false;
        EnterBoardMode();
    }

    public void TogglePaletteMode()
    {
        if (scoring && scoring.IsOverlayActive)
        {
            Debug.LogWarning("Cannot open palette while overlay is active.");
            return;
        }

        paletteMode = !paletteMode;

        if (paletteMode)
            EnterPaletteMode();
        else
            EnterBoardMode();
    }

    void EnterPaletteMode()
    {
        if (placement) placement.enabled = false; // prevents placing while picking
        if (camController)
        {
            camController.clampRenderer = paletteRenderer;
            camController.clampToBoard = true;
        }

        if (camController && paletteRenderer)
        {
            float fit = camController.ComputeFitOrthoForRenderer(paletteRenderer, paletteViewMargin);
            camController.SetTargetView(paletteRenderer.bounds.center, fit);
        }
    }

    void EnterBoardMode()
    {
        if (placement) placement.enabled = true;
        if (camController)
        {
            camController.clampRenderer = boardRenderer;
            camController.clampToBoard = true;
        }
        

        if (camController && boardRenderer)
        {
            float fit = camController.ComputeFitOrthoForRenderer(boardRenderer, boardViewMargin);
            camController.SetTargetView(boardRenderer.bounds.center, fit);
        }
    }

    void Update()
    {
        // Hide palette button while overlay is active
        if (paletteButton && scoring)
            paletteButton.SetActive(!scoring.IsOverlayActive);

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
                        placement.SetCurrentPieceFromPalette(pp);

                        if (autoCloseOnPick)
                            TogglePaletteMode();
                    }
                }
        }

    }
}
