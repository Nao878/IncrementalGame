using TMPro;
using UnityEngine;

/// <summary>
/// ゲーム全体で使用する TMP フォント（NotoSansJP-Bold SDF）を解決して適用する。
/// </summary>
public static class GameFontSettings
{
    const string FontResourcePath = "Fonts/NotoSansJP-Bold SDF";
    static readonly string[] OsFontFallbackCandidates =
    {
        "Noto Sans JP Bold",
        "Noto Sans JP",
        "Yu Gothic UI",
        "Meiryo",
        "MS Gothic"
    };

    static TMP_FontAsset _cached;
    static TMP_FontAsset _dynamicFallback;

    public static TMP_FontAsset GetFont()
    {
        if (_cached == null)
        {
            _cached = Resources.Load<TMP_FontAsset>(FontResourcePath);
            if (_cached == null)
                _cached = Resources.Load<TMP_FontAsset>("NotoSansJP-Bold SDF");
            if (_cached == null)
                Debug.LogError("[GameFontSettings] Resources/" + FontResourcePath + " を読み込めませんでした。");
        }
        EnsureReadableKanjiFallback(_cached);
        return _cached;
    }

    public static void ApplyTo(TMP_Text text)
    {
        if (text == null) return;
        TMP_FontAsset f = GetFont();
        if (f == null) return;
        text.font = f;
        EnsureReadableKanjiFallback(f);
        text.havePropertiesChanged = true;
    }

    static void EnsureReadableKanjiFallback(TMP_FontAsset primary)
    {
        if (primary == null) return;
        if (primary.HasCharacters("漢字木火日月明林炎")) return;

        if (_dynamicFallback == null)
            _dynamicFallback = CreateDynamicFallbackAsset();
        if (_dynamicFallback == null) return;

        if (primary.fallbackFontAssetTable == null)
            primary.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
        if (!primary.fallbackFontAssetTable.Contains(_dynamicFallback))
            primary.fallbackFontAssetTable.Add(_dynamicFallback);
    }

    static TMP_FontAsset CreateDynamicFallbackAsset()
    {
        Font osFont = Font.CreateDynamicFontFromOSFont(OsFontFallbackCandidates, 90);
        if (osFont == null) return null;
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(osFont);
        if (asset == null) return null;
        asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        asset.name = "RuntimeKanjiFallback";
        return asset;
    }
}
