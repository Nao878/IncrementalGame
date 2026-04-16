using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 左右からアイテムを受け取り、揃ったら合体させて前へ出力する施設。
/// </summary>
public class CombinerNode : FacilityNode
{
    private Item leftItem = null;
    private Item rightItem = null;

    private SpriteRenderer alertIcon;

    public override void Initialize(GridCell cell, ConveyorDirection direction)
    {
        base.Initialize(cell, direction);

        // 見た目の設定
        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.GetSquareSprite();
        sr.color = new Color(0.4f, 0.4f, 0.5f, 1f);
        sr.sortingOrder = 1;
        SpriteFactory.ApplyUnlitMaterial(sr);

        // 矢印（出力）
        GameObject outArrow = new GameObject("OutArrow");
        outArrow.transform.SetParent(transform, false);
        SpriteRenderer outSr = outArrow.AddComponent<SpriteRenderer>();
        outSr.sprite = SpriteFactory.GetArrowSprite();
        outSr.color = new Color(0.9f, 0.9f, 0.2f, 0.8f);
        outSr.sortingOrder = 2;
        outArrow.transform.localRotation = Quaternion.Euler(0f, 0f, direction.ToZRotation());
        SpriteFactory.ApplyUnlitMaterial(outSr);

        // 左入力表示
        CreateInputPin("LeftPin", direction.RotateCCW(), new Color(0.8f, 0.4f, 0.4f, 0.8f));
        // 右入力表示
        CreateInputPin("RightPin", direction.RotateCW(), new Color(0.4f, 0.4f, 0.8f, 0.8f));

        // エラーアイコン(Clog)
        GameObject alertObj = new GameObject("AlertIcon");
        alertObj.transform.SetParent(transform, false);
        alertObj.transform.localPosition = new Vector3(0f, 0f, -0.2f);
        var tmp = alertObj.AddComponent<TMPro.TextMeshPro>();
        GameFontSettings.ApplyTo(tmp);
        tmp.text = "⚠️";
        tmp.color = Color.yellow;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 12;
        tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);
        tmp.sortingOrder = 10;
        alertObj.SetActive(false);
        alertIcon = tmp.GetComponent<SpriteRenderer>(); // Just saving the object to toggle active
        // Wait, textmeshpro doesn't have spriterenderer. I'll just keep the GameObject.
    }

    private GameObject alertGo;

    private void Start()
    {
        Transform alertT = transform.Find("AlertIcon");
        if (alertT != null) alertGo = alertT.gameObject;
    }

    private void CreateInputPin(string name, ConveyorDirection dir, Color color)
    {
        GameObject pin = new GameObject(name);
        pin.transform.SetParent(transform, false);
        SpriteRenderer sr = pin.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.GetSquareSprite();
        sr.color = color;
        sr.sortingOrder = 2;
        pin.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        pin.transform.localPosition = new Vector3(dir.ToVector2Int().x * 0.35f, dir.ToVector2Int().y * 0.35f, 0f);
        SpriteFactory.ApplyUnlitMaterial(sr);

        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(pin.transform, false);
        SpriteRenderer arrowSr = arrowObj.AddComponent<SpriteRenderer>();
        arrowSr.sprite = SpriteFactory.GetArrowSprite();
        arrowSr.color = Color.white;
        arrowSr.sortingOrder = 3;
        arrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, dir.ToZRotation() + 180f);
        SpriteFactory.ApplyUnlitMaterial(arrowSr);
    }

    public override FacilityType GetFacilityType() => FacilityType.Combiner;

    public override bool CanAcceptItem(Item item)
    {
        if (ownerCell.HasItem && ownerCell.CurrentItem.Data != null && ownerCell.CurrentItem.Data.IsClog) return false;

        Vector2Int diff = item.PreviousGridPos - ownerCell.GridPosition;
        
        // Output -> Opposite of diff must match Left or Right input port.
        // Wait, if item comes FROM (x-1), diff is (-1, 0). 
        // This corresponds to coming from Left of the grid.
        // If Combiner expects input from its CCW direction:
        ConveyorDirection leftDir = outputDirection.RotateCCW();
        ConveyorDirection rightDir = outputDirection.RotateCW();

        if (diff == leftDir.ToVector2Int() && leftItem == null) return true;
        if (diff == rightDir.ToVector2Int() && rightItem == null) return true;

        return false;
    }

    public override bool OnItemArrived(Item item)
    {
        Vector2Int diff = item.PreviousGridPos - ownerCell.GridPosition;
        ConveyorDirection leftDir = outputDirection.RotateCCW();
        ConveyorDirection rightDir = outputDirection.RotateCW();

        if (diff == leftDir.ToVector2Int())
        {
            leftItem = item;
            // Hide the item inside the Combiner visually
            item.gameObject.SetActive(false);
        }
        else if (diff == rightDir.ToVector2Int())
        {
            rightItem = item;
            item.gameObject.SetActive(false);
        }
        else
        {
            return false; // Should not happen if CanAcceptItem is correct
        }

        TryMerge();
        return true;
    }

    private void TryMerge()
    {
        if (leftItem == null || rightItem == null) return;
        
        string leftK = leftItem.Data != null ? leftItem.Data.ItemText : "";
        string rightK = rightItem.Data != null ? rightItem.Data.ItemText : "";

        KanjiDatabaseManager db = KanjiDatabaseManager.Instance;
        string mergedKanji = null;
        bool merged = db != null && db.TryMergeKanji(new List<string> { leftK, rightK }, out mergedKanji);

        if (!merged || string.IsNullOrEmpty(mergedKanji))
        {
            int clogMergeCount = 0;
            clogMergeCount += (leftItem.Data != null ? leftItem.Data.MergeCount : 0);
            clogMergeCount += (rightItem.Data != null ? rightItem.Data.MergeCount : 0);
            
            string clogText = leftK + rightK;
            if (clogText.Length > 2) clogText = clogText.Substring(0, 2);
            
            ItemManager.Instance.UnregisterAndDestroy(leftItem);
            ItemManager.Instance.UnregisterAndDestroy(rightItem);
            leftItem = null;
            rightItem = null;

            HideAlert();
            
            Item cloggedItem = ItemManager.Instance.CreateItem(ownerCell.GridPosition, clogText, Color.gray, true);
            cloggedItem.Data.MergeCount = clogMergeCount;
            return;
        }

        // Check output cell
        Vector2Int outPos = ownerCell.GridPosition + outputDirection.ToVector2Int();
        if (!GridManager.Instance.IsInBounds(outPos))
        {
            ShowAlert();
            return;
        }

        GridCell outCell = GridManager.Instance.GetCell(outPos);
        if (outCell == null || (!outCell.HasConveyor && !outCell.HasFacility) || outCell.HasItem)
        {
            ShowAlert();
            return;
        }

        // Generate merged item
        int totalMergeCount = 0;
        totalMergeCount += (leftItem.Data != null ? leftItem.Data.MergeCount : 0) + 1;
        totalMergeCount += (rightItem.Data != null ? rightItem.Data.MergeCount : 0) + 1;
        
        ItemManager.Instance.UnregisterAndDestroy(leftItem);
        ItemManager.Instance.UnregisterAndDestroy(rightItem);
        leftItem = null;
        rightItem = null;

        HideAlert();

        Color newColor = ItemManager.Instance.GetMergeColor(totalMergeCount);

        Item newItem = ItemManager.Instance.CreateItem(outPos, mergedKanji, newColor, false);
        newItem.Data.MergeCount = totalMergeCount;
        newItem.PlayMergeSuccessPop();

        MergeEffectPlayer.PlayMergeBurst(GridManager.Instance.GridToWorld(ownerCell.GridPosition));
    }

    private void Update()
    {
        // Try to merge and output continuously if stalled
        if (leftItem != null && rightItem != null)
        {
            TryMerge();
        }
    }

    private void ShowAlert()
    {
        if (alertGo != null) alertGo.SetActive(true);
    }

    private void HideAlert()
    {
        if (alertGo != null) alertGo.SetActive(false);
    }
}
