using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TopDownCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform board;                 // optional: for reset center / clamping
    public Renderer clampRenderer;          // optional: override clamp/zoom bounds (palette or board)
    public bool clampToBoard = true;        // keep camera from panning past board
    public float clampPadding = 0.1f;       // extra space allowed (world units)

    [Header("Movement")]
    public float panSpeed = 8f;             // world units per second
    public float smoothTime = 0.12f;        // lower = snappier, higher = floatier

    [Header("Zoom (Orthographic)")]
    public float zoomSpeed = 8f;            // ortho units per second
    public float minOrthoSize = 1.5f;
    // public float maxOrthoSize = 30f;
    public float voidMargin = 5.0f;

    [Header("Keys")]
     KeyCode resetKey = KeyCode.R;
   
     KeyCode zoomOutKey = KeyCode.S;

    Camera cam;

    Vector3 defaultPos;
    float defaultOrtho;

    Vector3 targetPos;
    float targetOrtho;

    Vector3 posVel;
    float orthoVel;


    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        // Lock zenithal rotation
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Default values: from current scene setup
        defaultPos = transform.position;
        defaultOrtho = cam.orthographicSize;

        // Optional: align default to board center on start
        if (board != null)
        {
            defaultPos = new Vector3(board.position.x, defaultPos.y, board.position.z);
            transform.position = defaultPos;
        }

        targetPos = transform.position;
        targetOrtho = cam.orthographicSize;
    }

    void Update()
    {
        // Reset
        if (Input.GetKeyDown(resetKey))
        {
            targetPos = defaultPos;
            targetOrtho = defaultOrtho;
        }

        // Pan (zenithal: move in XZ)
        float dx = 0f, dz = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))  dx -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) dx += 1f;
        if (Input.GetKey(KeyCode.UpArrow))    dz += 1f;
        if (Input.GetKey(KeyCode.DownArrow))  dz -= 1f;

        Vector3 panDir = new Vector3(dx, 0f, dz);
        if (panDir.sqrMagnitude > 1f) panDir.Normalize();

        targetPos += panDir * panSpeed * Time.deltaTime;

        // Zoom (orthographic size)
        float zoomDelta = 0f;
        if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W)) zoomDelta -= 1f;
        if (Input.GetKey(zoomOutKey)) zoomDelta += 1f;

        targetOrtho += zoomDelta * zoomSpeed * Time.deltaTime;
        float currentMaxZoom = ComputeMaxOrthoSizeFromBoard();
        targetOrtho = Mathf.Clamp(targetOrtho, minOrthoSize, currentMaxZoom);

        // Optional clamping so you don't "see past" the board
        if (clampToBoard && board != null)
            targetPos = ClampTargetToBoard(targetPos, targetOrtho);
    }

    void LateUpdate()
    {
        // Keep zenithal rotation locked
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Smooth movement + zoom
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref posVel, smoothTime);
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetOrtho, ref orthoVel, smoothTime);
    }

    Vector3 ClampTargetToBoard(Vector3 pos, float orthoSize)
    {
        var rend = GetClampRenderer();
        if (rend == null) return pos;

        Bounds b = rend.bounds;

        float halfH = orthoSize;
        float halfW = orthoSize * cam.aspect;

        float minX = b.min.x + halfW - clampPadding;
        float maxX = b.max.x - halfW + clampPadding;
        float minZ = b.min.z + halfH - clampPadding;
        float maxZ = b.max.z - halfH + clampPadding;

        // If zoomed out beyond board, collapse clamp to center
        if (minX > maxX) { minX = maxX = b.center.x; }
        if (minZ > maxZ) { minZ = maxZ = b.center.z; }

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        return pos;
    }

    float ComputeMaxOrthoSizeFromBoard()
    {
        var rend = GetClampRenderer();
        if (!rend) return 50f;

        Bounds b = rend.bounds;
        float boardW = b.size.x;
        float boardH = b.size.z;
        
        float maxByHeight = (boardH * 0.5f) + voidMargin;
        float maxByWidth  = (boardW * 0.5f + voidMargin) / cam.aspect;

        float computed = Mathf.Min(maxByHeight, maxByWidth);

        return Mathf.Max(computed, minOrthoSize);
    }

    Renderer GetClampRenderer()
    {
        if (clampRenderer) return clampRenderer;
        return board ? board.GetComponent<Renderer>() : null;
    }

    public void SetTargetView(Vector3 worldCenterXZ, float orthoSize)
    {
        // keep current height (y)
        targetPos = new Vector3(worldCenterXZ.x, targetPos.y, worldCenterXZ.z);
        targetOrtho = Mathf.Max(orthoSize, minOrthoSize);
    }

    
    public float ComputeFitOrthoForRenderer(Renderer rend, float marginWorld = 0f)
    {
        if (!rend) return cam.orthographicSize;

        Bounds b = rend.bounds;
        float boardW = b.size.x;
        float boardH = b.size.z;

        float halfH = (boardH * 0.5f) + marginWorld;
        float halfW = ((boardW * 0.5f) + marginWorld) / cam.aspect;

        return Mathf.Max(Mathf.Min(halfH, halfW), minOrthoSize);
    }



}
