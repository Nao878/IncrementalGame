using UnityEngine;
using TMPro;

/// <summary>
/// Collects incoming blocks, destroys them, and awards coins by stroke count.
/// </summary>
public class CollectorNode : FacilityNode
{
    public override FacilityType GetFacilityType() => FacilityType.Collector;

    public override void Initialize(GridCell cell, ConveyorDirection direction)
    {
        base.Initialize(cell, direction);
        BuildVisualText("✓", new Color(0.2f, 0.8f, 0.4f, 1f));
    }

    void BuildVisualText(string t, Color color)
    {
        GameObject textObj = new GameObject("FacilityTypeVisual");
        textObj.transform.SetParent(transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        GameFontSettings.ApplyTo(tmp);
        tmp.text = t;
        tmp.color = color;
        tmp.fontSize = 7;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    public override bool OnItemArrived(Item item)
    {
        if (item == null) return false;
        string kanji = item.Data != null ? item.Data.ItemText : string.Empty;
        int stroke = KanjiDatabaseManager.Instance != null
            ? KanjiDatabaseManager.Instance.GetStrokeCount(kanji)
            : 1;
        int gain = Mathf.Max(1, stroke) * 10;
        ScoreManager.Instance?.AddCoins(gain);
        ItemManager.Instance.UnregisterAndDestroy(item);
        return true;
    }
}
