using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// KanjiRecipe.csv から合成レシピを読み込み、合体判定を提供する。
/// キーは「左から右へ」の順序を <see cref="MergePartSeparator"/> で連結した文字列（将来 N 文字対応）。
/// </summary>
public class KanjiDatabaseManager : MonoBehaviour
{
    public static KanjiDatabaseManager Instance { get; private set; }

    public const string RecipeResourceName = "KanjiRecipe";
    public const char MergePartSeparator = '\u001e';

    readonly Dictionary<string, string> _mergeTable = new Dictionary<string, string>();
    readonly Dictionary<string, int> _strokeCountTable = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, string> MergeTable => _mergeTable;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadRecipesFromResources();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadRecipesFromResources()
    {
        _mergeTable.Clear();
        _strokeCountTable.Clear();
        TextAsset ta = Resources.Load<TextAsset>(RecipeResourceName);
        if (ta == null)
        {
            Debug.LogWarning("[KanjiDatabaseManager] Resources/" + RecipeResourceName + ".csv が見つかりません。フォールバックレシピを使用します。");
            RegisterFallbackRecipes();
            return;
        }

        string text;
        try
        {
            var enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
            text = enc.GetString(ta.bytes);
        }
        catch (Exception e)
        {
            Debug.LogError("[KanjiDatabaseManager] CSV の UTF-8 デコードに失敗: " + e.Message);
            RegisterFallbackRecipes();
            return;
        }

        ParseAndRegister(text);
        if (_mergeTable.Count == 0)
        {
            Debug.LogWarning("[KanjiDatabaseManager] 有効なレシピがありません。フォールバックを登録します。");
            RegisterFallbackRecipes();
        }
        else
            Debug.Log("[KanjiDatabaseManager] 合成レシピ " + _mergeTable.Count + " 件を読み込みました。");
    }

    void ParseAndRegister(string csvText)
    {
        using (var reader = new StringReader(csvText))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                string[] cells = line.Split(',');
                if (cells.Length < 2)
                    continue;

                for (int i = 0; i < cells.Length; i++)
                    cells[i] = cells[i].Trim();

                string result = cells[cells.Length - 1];
                if (string.IsNullOrEmpty(result))
                    continue;

                var parts = new List<string>();
                for (int i = 0; i < cells.Length - 1; i++)
                {
                    if (!string.IsNullOrEmpty(cells[i]))
                        parts.Add(cells[i]);
                }

                if (parts.Count == 0)
                    continue;

                _mergeTable[BuildMergeKey(parts)] = result;

                // Optional stroke count in 4th column for 3-column recipe schema (+ optional metadata).
                if (cells.Length >= 4 && int.TryParse(cells[3], out int strokes))
                    _strokeCountTable[result] = Mathf.Max(1, strokes);
            }
        }
    }

    void RegisterFallbackRecipes()
    {
        RegisterPair("日", "月", "明");
        RegisterPair("木", "木", "林");
        RegisterPair("火", "火", "炎");
        _strokeCountTable["木"] = 4;
        _strokeCountTable["日"] = 4;
        _strokeCountTable["月"] = 4;
        _strokeCountTable["火"] = 4;
        _strokeCountTable["明"] = 8;
        _strokeCountTable["林"] = 8;
        _strokeCountTable["炎"] = 8;
    }

    void RegisterPair(string left, string right, string result)
    {
        _mergeTable[BuildMergeKey(new List<string> { left, right })] = result;
    }

    public static string BuildMergeKey(IReadOnlyList<string> orderedParts)
    {
        if (orderedParts == null || orderedParts.Count == 0)
            return string.Empty;
        return string.Join(MergePartSeparator.ToString(), orderedParts);
    }

    /// <summary>
    /// Lookup merge result for ordered kanji parts. False if recipe or order does not match.
    /// </summary>
    public bool TryMergeKanji(IReadOnlyList<string> orderedKanjiParts, out string mergedKanji)
    {
        mergedKanji = null;
        if (orderedKanjiParts == null || orderedKanjiParts.Count == 0)
            return false;
        string key = BuildMergeKey(orderedKanjiParts);
        return _mergeTable.TryGetValue(key, out mergedKanji);
    }

    public int GetStrokeCount(string kanji)
    {
        if (string.IsNullOrEmpty(kanji)) return 1;
        if (_strokeCountTable.TryGetValue(kanji, out int value))
            return Mathf.Max(1, value);
        // Fallback heuristic: at least 1, 1-char kanji defaults to 1 if unknown.
        return Mathf.Max(1, kanji.Length);
    }
}
