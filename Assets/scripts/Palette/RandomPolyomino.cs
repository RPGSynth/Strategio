using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RandomPolyomino
{
    static readonly Vector2Int[] N4 =
    {
        new Vector2Int( 1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int( 0, 1),
        new Vector2Int( 0,-1),
    };

    // Generates a contiguous 4-connected polyomino of exactly `cellsCount` squares.
    public static Vector2Int[] Generate(int cellsCount, System.Random rng)
    {
        cellsCount = Mathf.Max(1, cellsCount);

        var cells = new HashSet<Vector2Int> { Vector2Int.zero };
        var frontier = new List<Vector2Int>();

        void AddFrontierFrom(Vector2Int c)
        {
            foreach (var d in N4)
            {
                var n = c + d;
                if (!cells.Contains(n) && !frontier.Contains(n))
                    frontier.Add(n);
            }
        }

        AddFrontierFrom(Vector2Int.zero);

        while (cells.Count < cellsCount)
        {
            // pick a random frontier cell; adding it guarantees side-adjacency
            int idx = rng.Next(frontier.Count);
            var next = frontier[idx];
            frontier.RemoveAt(idx);

            if (cells.Add(next))
                AddFrontierFrom(next);

            // if frontier ever empties (rare), restart safely
            if (frontier.Count == 0 && cells.Count < cellsCount)
                return Generate(cellsCount, rng);
        }

        return Normalize(cells.ToArray());
    }

    // Normalize so minX=minY=0
    public static Vector2Int[] Normalize(Vector2Int[] offsets)
    {
        int minX = offsets.Min(o => o.x);
        int minY = offsets.Min(o => o.y);

        for (int i = 0; i < offsets.Length; i++)
            offsets[i] = new Vector2Int(offsets[i].x - minX, offsets[i].y - minY);

        Array.Sort(offsets, (a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return offsets;
    }

    // Canonical key to avoid duplicates (treat rotations+flips as same shape)
    public static string CanonicalKey(Vector2Int[] offsets)
    {
        // 8 transforms: 4 rotations * optional flip
        Vector2Int Apply(Vector2Int v, int t)
        {
            // rotate (t % 4)
            Vector2Int r = (t % 4) switch
            {
                0 => new Vector2Int( v.x,  v.y),
                1 => new Vector2Int(-v.y,  v.x),
                2 => new Vector2Int(-v.x, -v.y),
                _ => new Vector2Int( v.y, -v.x),
            };

            // flip across Y axis for t>=4
            if (t >= 4) r = new Vector2Int(-r.x, r.y);
            return r;
        }

        string best = null;

        for (int t = 0; t < 8; t++)
        {
            var tmp = new Vector2Int[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
                tmp[i] = Apply(offsets[i], t);

            tmp = Normalize(tmp);

            string key = string.Join(";", System.Array.ConvertAll(tmp, o => $"{o.x},{o.y}"));
            if (best == null || string.CompareOrdinal(key, best) < 0)
                best = key;
        }

        return best ?? "";
    }
}
