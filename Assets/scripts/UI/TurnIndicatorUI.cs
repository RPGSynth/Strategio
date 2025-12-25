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
        label.color = players.GetUIColor(i, Color.white);
    }
}
