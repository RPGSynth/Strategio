using System.Collections.Generic;
using UnityEngine;

public class PieceManager : MonoBehaviour
{
    [Header("Initial Bag Config")]
    public List<PieceSettings> initialBagPieces = new List<PieceSettings>();

    // The current bag of pieces available to place
    private List<PieceSettings> bag = new List<PieceSettings>();

    public List<PieceSettings> AvailablePieces => bag;

    void Awake()
    {
        // Populate the bag from the initial list
        if (initialBagPieces != null)
        {
            bag.AddRange(initialBagPieces);
        }
    }

    public void AddPiece(PieceSettings piece)
    {
        bag.Add(piece);
    }

    public void RemovePiece(PieceSettings piece)
    {
        if (bag.Contains(piece))
        {
            bag.Remove(piece);
        }
    }
}
