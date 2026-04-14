using UnityEngine;

/// <summary>
/// プロシージャルにスプライトを生成するユーティリティクラス。
/// 外部アセットなしでグリッド、矢印、ブロックのスプライトを動的に作成する。
/// </summary>
public static class SpriteFactory
{
    private static Sprite _squareSprite;
    private static Sprite _arrowSprite;
    private static Sprite _blockSprite;
    private static Sprite _generatorSprite;
    private static Material _unlitMaterial;

    /// <summary>
    /// URP環境でスプライトが暗くならないようにするUnlitマテリアル
    /// </summary>
    public static Material GetUnlitMaterial()
    {
        if (_unlitMaterial == null)
        {
            // Sprites-Default (Unlit) マテリアルを探す
            Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null)
                unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader != null)
                _unlitMaterial = new Material(unlitShader);
        }
        return _unlitMaterial;
    }

    /// <summary>
    /// SpriteRendererにUnlitマテリアルを適用するヘルパー
    /// </summary>
    public static void ApplyUnlitMaterial(SpriteRenderer sr)
    {
        Material mat = GetUnlitMaterial();
        if (mat != null && sr != null)
            sr.material = mat;
    }

    /// <summary>
    /// グリッドのマス目用スプライト（枠線付きの四角）
    /// </summary>
    public static Sprite GetSquareSprite()
    {
        if (_squareSprite == null)
            _squareSprite = CreateSquare();
        return _squareSprite;
    }

    /// <summary>
    /// コンベアの矢印スプライト
    /// </summary>
    public static Sprite GetArrowSprite()
    {
        if (_arrowSprite == null)
            _arrowSprite = CreateArrow();
        return _arrowSprite;
    }

    /// <summary>
    /// アイテムブロック用スプライト
    /// </summary>
    public static Sprite GetBlockSprite()
    {
        if (_blockSprite == null)
            _blockSprite = CreateBlock();
        return _blockSprite;
    }

    /// <summary>
    /// ジェネレーター用スプライト
    /// </summary>
    public static Sprite GetGeneratorSprite()
    {
        if (_generatorSprite == null)
            _generatorSprite = CreateGenerator();
        return _generatorSprite;
    }

    private static Sprite CreateSquare()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color fillColor = new Color(0.22f, 0.22f, 0.25f, 1f);
        Color borderColor = new Color(0.35f, 0.35f, 0.4f, 1f);
        int borderWidth = 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < borderWidth || x >= size - borderWidth ||
                                y < borderWidth || y >= size - borderWidth;
                tex.SetPixel(x, y, isBorder ? borderColor : fillColor);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateArrow()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color transparent = new Color(0, 0, 0, 0);
        Color arrowColor = new Color(0.3f, 0.85f, 0.4f, 1f);

        // 全体を透明に
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, transparent);

        // 矢印の軸（中央の縦線）
        int shaftWidth = 10;
        int shaftLeft = (size - shaftWidth) / 2;
        int shaftBottom = 8;
        int shaftTop = 44;
        for (int y = shaftBottom; y < shaftTop; y++)
            for (int x = shaftLeft; x < shaftLeft + shaftWidth; x++)
                tex.SetPixel(x, y, arrowColor);

        // 矢印の頭（三角形）
        int headBase = 28;
        int headTop = 56;
        int centerX = size / 2;
        for (int y = shaftTop; y < headTop; y++)
        {
            float t = (float)(y - shaftTop) / (headTop - shaftTop);
            int halfWidth = (int)Mathf.Lerp(headBase / 2, 0, t);
            for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
            {
                if (x >= 0 && x < size)
                    tex.SetPixel(x, y, arrowColor);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateBlock()
    {
        int size = 48;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color transparent = new Color(0, 0, 0, 0);
        Color blockColor = Color.white;
        int margin = 8;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = x >= margin && x < size - margin &&
                              y >= margin && y < size - margin;
                tex.SetPixel(x, y, inside ? blockColor : transparent);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateGenerator()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color fillColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        Color borderColor = new Color(0.9f, 0.6f, 0.2f, 1f);
        Color accentColor = new Color(1f, 0.75f, 0.3f, 1f);
        int borderWidth = 3;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < borderWidth || x >= size - borderWidth ||
                                y < borderWidth || y >= size - borderWidth;
                tex.SetPixel(x, y, isBorder ? borderColor : fillColor);
            }
        }

        // 中心にダイヤマーク
        int cx = size / 2;
        int cy = size / 2;
        int diamondSize = 10;
        for (int dy = -diamondSize; dy <= diamondSize; dy++)
        {
            for (int dx = -diamondSize; dx <= diamondSize; dx++)
            {
                int px = cx + dx;
                int py = cy + dy;
                if (px >= 0 && px < size && py >= 0 && py < size)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= diamondSize)
                        tex.SetPixel(px, py, accentColor);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
