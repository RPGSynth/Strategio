using UnityEngine;

[ExecuteAlways]
public class FitBoardTopDown : MonoBehaviour
{
    public Transform board;      // your plane transform
    public float margin = 100f;  // world units

    void LateUpdate()
    {
        if (!board) return;

        var cam = GetComponent<Camera>();
        if (!cam) return;

        cam.orthographic = true;

        // Lock zenithal view
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Board size in world (assuming plane lies on XZ)
        float boardWidth  = board.lossyScale.x * 10f; // Unity plane is 10 units wide
        float boardHeight = board.lossyScale.z * 10f; // Unity plane is 10 units tall

        float aspect = cam.aspect;

        // Fit vertically and horizontally
        float sizeByHeight = (boardHeight * 0.5f) + margin;
        float sizeByWidth  = ((boardWidth * 0.5f) / aspect) + margin;

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);

        // Center camera over board
        Vector3 center = board.position;
        float camY = center.y + 10f; // height doesn't affect ortho framing; keep it above everything
        transform.position = new Vector3(center.x, camY, center.z);

        // Optional: no skybox
        cam.clearFlags = CameraClearFlags.SolidColor;
    }
}
