using TMPro;
using UnityEngine;

public class TurnIndicatorUI : MonoBehaviour
{
    public TurnManager turn;
    public PlayersSettings players;
    public TMP_Text label;

    void Awake()
    {
        if (!label) label = GetComponent<TMP_Text>();
        if (!turn) turn = FindObjectOfType<TurnManager>();
    }

    void Start() => Refresh();
    void Update() => Refresh(); // simple + reliable (we can optimize later)

    void Refresh()
    {
        if (!label || !turn || !players || players.Count == 0) return;

        int i = turn.CurrentPlayer;

        label.text = $"{players.GetName(i)} is playing";

        // Prefer explicit uiColor, fallback to material color if uiColor wasn't set meaningfully
        Color c = players.players[i].uiColor;
        if (c.a <= 0.001f || (c.r == 1f && c.g == 1f && c.b == 1f)) // "default-ish"
        {
            var mat = players.GetPlacedMat(i);
            c = TryGetMaterialColor(mat, Color.white);
        }

        label.color = c;
    }

    static Color TryGetMaterialColor(Material mat, Color fallback)
    {
        if (!mat) return fallback;

        // URP Lit uses _BaseColor; built-in Standard uses _Color
        if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
        if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
        return fallback;
    }
}
