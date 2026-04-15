using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Accepts blocks from any side, merges based on recipe, and outputs to configured side.
/// </summary>
public class CombinerNode : FacilityNode
{
    readonly List<Item> buffer = new List<Item>();
    private SpriteRenderer arrowRenderer;

    public override void Initialize(GridCell cell, ConveyorDirection direction)
    {
        base.Initialize(cell, direction);
        BuildArrow();
    }

    public override FacilityType GetFacilityType() => FacilityType.Combiner;

    public override bool OnItemArrived(Item item)
    {
        if (item == null) return false;

        buffer.Add(item);
        ItemManager.Instance.UnregisterAndDestroy(item);
        TryCombineAndOutput();
        return true;
    }

    public override void RotateOutput()
    {
        base.RotateOutput();
        RefreshArrow();
    }

    void TryCombineAndOutput()
    {
        if (buffer.Count < 2) return;

        KanjiDatabaseManager db = KanjiDatabaseManager.Instance;
        if (db == null) return;

        // FIFO pair for extensible N-gram recipes later.
        string a = buffer[0].Data != null ? buffer[0].Data.ItemText : string.Empty;
        string b = buffer[1].Data != null ? buffer[1].Data.ItemText : string.Empty;

        if (!db.TryMergeKanji(new List<string> { a, b }, out string merged))
            return;

        Vector2Int outPos = ownerCell.GridPosition + outputDirection.ToVector2Int();
        GridCell outCell = GridManager.Instance.GetCell(outPos);
        if (outCell == null || outCell.HasItem) return;

        buffer.RemoveAt(0);
        buffer.RemoveAt(0);

        Item created = ItemManager.Instance.CreateItem(outPos, merged, ItemManager.Instance.GetMergeColor(1), false);
        created.PlayMergeSuccessPop();
        MergeEffectPlayer.PlayMergeBurst(GridManager.Instance.GridToWorld(ownerCell.GridPosition));
    }

    void BuildArrow()
    {
        if (arrowRenderer != null) return;
        GameObject arrow = new GameObject("CombinerOutArrow");
        arrow.transform.SetParent(transform, false);
        arrow.transform.localPosition = new Vector3(0f, 0f, -0.05f);
        arrowRenderer = arrow.AddComponent<SpriteRenderer>();
        arrowRenderer.sprite = SpriteFactory.GetArrowSprite();
        arrowRenderer.color = new Color(0.95f, 0.8f, 0.25f, 0.9f);
        arrowRenderer.sortingOrder = 6;
        SpriteFactory.ApplyUnlitMaterial(arrowRenderer);
        RefreshArrow();
    }

    void RefreshArrow()
    {
        if (arrowRenderer == null) return;
        arrowRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, outputDirection.ToZRotation());
    }
}
