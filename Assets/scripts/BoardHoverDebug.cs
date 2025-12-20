using UnityEngine;

public class BoardHoverDebug : MonoBehaviour
{
    public BoardGrid board;
    public Transform highlight; // a small quad/cube you move around
    public float highlightY = 0.02f;
    void Start()
    {
        if (board && highlight)
        {
            float s = board.cellSize;
            highlight.localScale = new Vector3(s, highlight.localScale.y, s);
        }
    }
    void Update()
    {
        if (!board || !highlight) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            if (board.WorldToCell(hit.point, out var cell))
            {
                highlight.gameObject.SetActive(true);
                highlight.position = board.CellToWorld(cell.x, cell.y, highlightY);
            }
            else
            {
                highlight.gameObject.SetActive(false);
            }
        }
        else highlight.gameObject.SetActive(false);
    }
}
