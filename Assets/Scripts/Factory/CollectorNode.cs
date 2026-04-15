using UnityEngine;

/// <summary>
/// Collects incoming blocks, destroys them, and awards coins by stroke count.
/// </summary>
public class CollectorNode : FacilityNode
{
    public override FacilityType GetFacilityType() => FacilityType.Collector;

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
