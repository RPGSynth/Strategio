using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public PlayersSettings players;
    public int currentPlayerIndex = 0;

    public int PlayerCount => players ? players.Count : 0;

    public int CurrentPlayer => Mathf.Clamp(currentPlayerIndex, 0, Mathf.Max(0, PlayerCount - 1));

    public void NextTurn()
    {
        if (PlayerCount <= 0) return;
        currentPlayerIndex = (currentPlayerIndex + 1) % PlayerCount;
        Debug.Log($"Turn: {players.GetName(CurrentPlayer)} (#{CurrentPlayer})");
    }
}
