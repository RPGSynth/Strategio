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

    void OnEnable()
    {
        if (!turn) turn = FindObjectOfType<TurnManager>();
        if (turn)
            turn.TurnChanged += HandleTurnChanged;
        Refresh(turn ? turn.CurrentPlayer : 0);
    }

    void OnDisable()
    {
        if (turn)
            turn.TurnChanged -= HandleTurnChanged;
    }

    void HandleTurnChanged(int playerIndex)
    {
        Refresh(playerIndex);
    }

    void Refresh(int playerIndex)
    {
        if (!label || !turn || !players || players.Count == 0) return;

        int i = Mathf.Clamp(playerIndex, 0, players.Count - 1);
        label.text = $"{players.GetName(i)} is playing";
        label.color = players.GetUIColor(i, Color.white);
    }
}
