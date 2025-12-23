using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Board Game/Players Settings", fileName = "PlayersSettings")]
public class PlayersSettings : ScriptableObject
{
    [Serializable]
    public class Player
    {
        public string name = "Player";
        public Material placedMaterial;
        public Color uiColor = Color.white;
    }

    public List<Player> players = new List<Player>
    {
        new Player { name = "P1" },
        new Player { name = "P2" },
    };

    public int Count => players != null ? players.Count : 0;

    public Material GetPlacedMat(int playerIndex)
    {
        if (players == null || players.Count == 0) return null;
        playerIndex = Mathf.Clamp(playerIndex, 0, players.Count - 1);
        return players[playerIndex].placedMaterial;
    }

    public string GetName(int playerIndex)
    {
        if (players == null || players.Count == 0) return "Player";
        playerIndex = Mathf.Clamp(playerIndex, 0, players.Count - 1);
        return players[playerIndex].name;
    }
}
