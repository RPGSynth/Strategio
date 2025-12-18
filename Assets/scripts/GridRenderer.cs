using System;
using UnityEngine;


public class GridTextureGenerator : MonoBehaviour
{
    public Renderer gridOverlayRenderer;

    [Range(10,101)] public int boardSize = 29;
    public int targetPixelsPerCell = 128; // 64 / 128 / 256
    public float marginPercent = 0.08f;   // 0.05–0.12
    public int lineWidthPx = 2;

    void Start()
    {
        Regenerate();
    }

    void Regenerate()
    {
        if (!gridOverlayRenderer) return;

        int textureSize = ComputeTextureSize(boardSize, targetPixelsPerCell, marginPercent);
        int marginPx    = ComputeMarginPx(textureSize, marginPercent);

        var tex = GenerateGridTexture(boardSize, textureSize, marginPx, lineWidthPx);
        tex.wrapMode  = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        gridOverlayRenderer.material.mainTexture = tex;
    }

    int ComputeTextureSize(int n, int ppc, float marginPct)
    {
        float usable = 1f - 2f * marginPct;
        int needed = Mathf.CeilToInt(((n - 1) * ppc) / Mathf.Max(usable, 0.01f));
        return Mathf.ClosestPowerOfTwo(needed);
    }

    int ComputeMarginPx(int texSize, float marginPct)
    {
        return Mathf.RoundToInt(texSize * marginPct);
    }


    Texture2D GenerateGridTexture(int size, int texSize, int margin, int lineWidth)
    {
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        var clear = new Color(0, 0, 0, 0);
        var line = new Color(0, 0, 0, 1);

        // Clear
        var pixels = new Color[texSize * texSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        int inner = texSize - 2 * margin;
        float step = inner / (float)(size - 1);

        void DrawVerticalLine(int x)
        {
            for (int dx = -lineWidth/2; dx <= lineWidth/2; dx++)
            {
                int xx = x + dx;
                if (xx < 0 || xx >= texSize) continue;
                for (int y = margin; y <= texSize - margin; y++)
                    pixels[y * texSize + xx] = line;
            }
        }

        void DrawHorizontalLine(int y)
        {
            for (int dy = -lineWidth/2; dy <= lineWidth/2; dy++)
            {
                int yy = y + dy;
                if (yy < 0 || yy >= texSize) continue;
                for (int x = margin; x <= texSize - margin; x++)
                    pixels[yy * texSize + x] = line;
            }
        }

        for (int i = 0; i < size; i++)
        {
            int x = Mathf.RoundToInt(margin + i * step);
            int y = Mathf.RoundToInt(margin + i * step);
            DrawVerticalLine(x);
            DrawHorizontalLine(y);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
