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
        [HideInInspector] public Material placedMaterial;
        [HideInInspector] public Color uiColor = Color.white;
    }

    public List<Player> players = new List<Player>
    {
        new Player { name = "P1" },
        new Player { name = "P2" },
    };

    [Header("Auto Naming")]
    public bool autoNamePlayers = true;

    public int Count => players != null ? players.Count : 0;

    const string PlayerMaterialsFolder = "materials/players";
    const string PlayerMaterialPrefix = "P_";
    const string DefaultPlayerMaterialName = "P_default";

    [NonSerialized] readonly Dictionary<int, Material> materialCache = new();
    [NonSerialized] Material defaultMaterial;
    [NonSerialized] bool defaultMaterialLoaded;

    public Material GetPlacedMat(int playerIndex)
    {
        if (players == null || players.Count == 0) return GetDefaultMaterialOrError();
        playerIndex = Mathf.Clamp(playerIndex, 0, players.Count - 1);

        if (materialCache.TryGetValue(playerIndex, out var cached) && cached)
            return cached;

        var mat = LoadPlayerMaterial(playerIndex);
        if (!mat)
            mat = GetDefaultMaterialOrError();

        materialCache[playerIndex] = mat;
        return mat;
    }

    public string GetName(int playerIndex)
    {
        if (players == null || players.Count == 0) return "Player";
        playerIndex = Mathf.Clamp(playerIndex, 0, players.Count - 1);
        return players[playerIndex].name;
    }

    public Color GetUIColor(int playerIndex, Color fallback)
    {
        var mat = GetPlacedMat(playerIndex);
        if (!mat) return fallback;

        Color c = TryGetMaterialColor(mat, fallback);
        c.a = 1f;
        return c;
    }

    Material LoadPlayerMaterial(int playerIndex)
    {
        string matName = $"{PlayerMaterialPrefix}{playerIndex}";
        return LoadMaterialByName(matName);
    }

    Material GetDefaultMaterialOrError()
    {
        if (defaultMaterialLoaded)
            return defaultMaterial;

        defaultMaterial = LoadMaterialByName(DefaultPlayerMaterialName);
        defaultMaterialLoaded = true;

        if (!defaultMaterial)
            Debug.LogError("[PlayersSettings] ERR_PLAYER_MAT_DEFAULT_MISSING");

        return defaultMaterial;
    }

    static Material LoadMaterialByName(string matName)
    {
        string resourcesPath = $"{PlayerMaterialsFolder}/{matName}";
        return Resources.Load<Material>(resourcesPath);
    }

    static Color TryGetMaterialColor(Material mat, Color fallback)
    {
        if (!mat) return fallback;

        if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
        if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
        return fallback;
    }

    void OnValidate()
    {
        if (!autoNamePlayers || players == null) return;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null) players[i] = new Player();
            if (ShouldAutoName(players[i].name))
                players[i].name = $"P{i + 1}";
        }
    }

    static bool ShouldAutoName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return true;
        if (playerName == "Player") return true;
        if (playerName.Length < 2 || playerName[0] != 'P') return false;
        for (int i = 1; i < playerName.Length; i++)
        {
            if (!char.IsDigit(playerName[i]))
                return false;
        }
        return true;
    }
}
