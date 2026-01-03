using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public PlayersSettings players;
    public int currentPlayerIndex = 0;

    public int PlayerCount => players ? players.Count : 0;

    public int CurrentPlayer => Mathf.Clamp(currentPlayerIndex, 0, Mathf.Max(0, PlayerCount - 1));

    public event Action<int> TurnChanged;

    public void SetCurrentPlayerIndex(int index, bool log = true)
    {
        if (PlayerCount <= 0) return;
        int clamped = Mathf.Clamp(index, 0, PlayerCount - 1);
        if (clamped == currentPlayerIndex) return;

        currentPlayerIndex = clamped;
        if (log)
            Debug.Log($"Turn: {players.GetName(CurrentPlayer)} (#{CurrentPlayer})");

        TurnChanged?.Invoke(CurrentPlayer);
    }

    public void NextTurn()
    {
        if (PlayerCount <= 0) return;
        int next = (currentPlayerIndex + 1) % PlayerCount;
        SetCurrentPlayerIndex(next);
    }
}
