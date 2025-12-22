using UnityEngine;

public class GridTextureGenerator : MonoBehaviour
{
    public BoardSettings settings;
    public Renderer gridOverlayRenderer;

    void Start() => Regenerate();

#if UNITY_EDITOR
    void OnValidate() => Regenerate();
#endif

    void Regenerate()
    {
        if (!settings || !gridOverlayRenderer) return;

        int n = settings.N;
        float marginPercent = settings.marginPercent;

        int ppc = settings.targetPixelsPerCell;
        int lineWidthPx = settings.lineWidthPx;

        int textureSize = ComputeTextureSize(n, ppc, marginPercent);
        int marginPx = ComputeMarginPx(textureSize, marginPercent);

        var tex = GenerateGridTexture(n, textureSize, marginPx, lineWidthPx);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = settings.gridFilterMode;

        // In edit mode, sharedMaterial is safer; at runtime material is fine.
#if UNITY_EDITOR
        if (!Application.isPlaying)
            gridOverlayRenderer.sharedMaterial.mainTexture = tex;
        else
            gridOverlayRenderer.material.mainTexture = tex;
#else
        gridOverlayRenderer.material.mainTexture = tex;
#endif
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


    Texture2D GenerateGridTexture(int cellsPerSide, int texSize, int margin, int lineWidth)
    {
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        var clear = new Color(0, 0, 0, 0);
        var line = new Color(0, 0, 0, 1);

        var pixels = new Color[texSize * texSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        int inner = texSize - 2 * margin;
        float step = inner / (float)cellsPerSide; // ✅ N squares => step = inner / N

        void DrawVertical(int x)
        {
            for (int dx = -lineWidth / 2; dx <= lineWidth / 2; dx++)
            {
                int xx = x + dx;
                if (xx < 0 || xx >= texSize) continue;
                for (int y = margin; y <= texSize - margin; y++)
                    pixels[y * texSize + xx] = line;
            }
        }

        void DrawHorizontal(int y)
        {
            for (int dy = -lineWidth / 2; dy <= lineWidth / 2; dy++)
            {
                int yy = y + dy;
                if (yy < 0 || yy >= texSize) continue;
                for (int x = margin; x <= texSize - margin; x++)
                    pixels[yy * texSize + x] = line;
            }
        }

        // ✅ draw N+1 lines
        for (int i = 0; i <= cellsPerSide; i++)
        {
            int x = Mathf.RoundToInt(margin + i * step);
            int y = Mathf.RoundToInt(margin + i * step);
            DrawVertical(x);
            DrawHorizontal(y);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
