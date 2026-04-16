using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 左右からアイテムを受け取り、揃ったら合体させて前へ出力する施設。
/// 内部に左右のスロット(文字列)を保持し、受け取ったブロックは即座にDestroyする。
/// </summary>
public class CombinerNode : FacilityNode
{
    public string leftSlot = "";
    public string rightSlot = "";

    private TMPro.TextMeshPro leftTextUI;
    private TMPro.TextMeshPro rightTextUI;
    private GameObject alertGo;

    // キャッシュ用（排出時に使う等）
    private int leftMergeCount = 0;
    private int rightMergeCount = 0;

    public override void Initialize(GridCell cell, ConveyorDirection direction)
    {
        base.Initialize(cell, direction);

        // 見た目の設定 (背景板)
        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.GetSquareSprite();
        sr.color = new Color(0.4f, 0.4f, 0.5f, 1f);
        sr.sortingOrder = 1;
        SpriteFactory.ApplyUnlitMaterial(sr);

        // 削除対応のためのコライダーを追加
        BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);
        col.isTrigger = true; // クリック判定やトリガー用に

        // 出力矢印
        GameObject outArrow = new GameObject("OutArrow");
        outArrow.transform.SetParent(transform, false);
        SpriteRenderer outSr = outArrow.AddComponent<SpriteRenderer>();
        outSr.sprite = SpriteFactory.GetArrowSprite();
        outSr.color = new Color(0.9f, 0.9f, 0.2f, 0.8f);
        outSr.sortingOrder = 2;
        outArrow.transform.localRotation = Quaternion.Euler(0f, 0f, direction.ToZRotation());
        SpriteFactory.ApplyUnlitMaterial(outSr);

        // 入力ピン生成
        CreateInputPin("LeftPin", direction.RotateCCW(), new Color(0.8f, 0.4f, 0.4f, 0.8f));
        CreateInputPin("RightPin", direction.RotateCW(), new Color(0.4f, 0.4f, 0.8f, 0.8f));

        // テキスト表示用UIの作成
        leftTextUI = CreateSlotText("LeftSlotText", new Vector3(-0.25f, 0.1f, -0.1f));
        rightTextUI = CreateSlotText("RightSlotText", new Vector3(0.25f, 0.1f, -0.1f));

        // エラー(詰まり)の警告UI
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
        alertGo = alertObj;
        
        UpdateUI();
    }

    private TMPro.TextMeshPro CreateSlotText(string objName, Vector3 localPos)
    {
        GameObject textObj = new GameObject(objName);
        textObj.transform.SetParent(transform, false);
        textObj.transform.localPosition = localPos;

        TMPro.TextMeshPro tmp = textObj.AddComponent<TMPro.TextMeshPro>();
        GameFontSettings.ApplyTo(tmp); // NotoSansJP-Boldなどを適用
        tmp.text = "?";
        tmp.color = Color.white;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 8;
        tmp.rectTransform.sizeDelta = new Vector2(0.5f, 0.5f);
        tmp.sortingOrder = 10;
        return tmp;
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
        // 自動吸入（Proximity Intake）するため、コンベアからのプッシュは常に拒否して待機させる
        return false;
    }

    public override bool OnItemArrived(Item item)
    {
        return false;
    }

    private void UpdateUI()
    {
        if (leftTextUI != null)
        {
            leftTextUI.text = string.IsNullOrEmpty(leftSlot) ? "?" : leftSlot;
        }
        if (rightTextUI != null)
        {
            rightTextUI.text = string.IsNullOrEmpty(rightSlot) ? "?" : rightSlot;
        }
    }

    private void TryMerge()
    {
        if (string.IsNullOrEmpty(leftSlot) || string.IsNullOrEmpty(rightSlot)) return;
        
        // 排出先のチェック
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
            return; // 排出先が埋まっているので待機
        }

        HideAlert();

        KanjiDatabaseManager db = KanjiDatabaseManager.Instance;
        string mergedKanji = null;
        bool merged = db != null && db.TryMergeKanji(new List<string> { leftSlot, rightSlot }, out mergedKanji);

        if (!merged || string.IsNullOrEmpty(mergedKanji))
        {
            // --- 失敗パターン：ゴミ（廃棄物）としてそのまま排出する ---
            int clogMergeCount = leftMergeCount + rightMergeCount;
            string clogText = leftSlot + rightSlot;
            if (clogText.Length > 2) clogText = clogText.Substring(0, 2);
            
            // スロットリセット
            leftSlot = "";
            rightSlot = "";
            leftMergeCount = 0;
            rightMergeCount = 0;
            UpdateUI();

            Item cloggedItem = ItemManager.Instance.CreateItem(outPos, clogText, Color.gray, false);
            cloggedItem.Data.MergeCount = clogMergeCount;
            return;
        }

        // --- 成功パターン ---
        int totalMergeCount = leftMergeCount + rightMergeCount + 2;
        
        // スロットリセット
        leftSlot = "";
        rightSlot = "";
        leftMergeCount = 0;
        rightMergeCount = 0;
        UpdateUI();

        Color newColor = ItemManager.Instance.GetMergeColor(totalMergeCount);
        Item newItem = ItemManager.Instance.CreateItem(outPos, mergedKanji, newColor, false);
        newItem.Data.MergeCount = totalMergeCount;
        newItem.PlayMergeSuccessPop();

        MergeEffectPlayer.PlayMergeBurst(GridManager.Instance.GridToWorld(ownerCell.GridPosition));
    }

    private void Update()
    {
        CheckAndSuckItem(outputDirection.RotateCCW(), true);
        CheckAndSuckItem(outputDirection.RotateCW(), false);

        // 詰まりで待機していた場合、排出先が空いたら再試行
        if (!string.IsNullOrEmpty(leftSlot) && !string.IsNullOrEmpty(rightSlot))
        {
            TryMerge();
        }
    }

    private void CheckAndSuckItem(ConveyorDirection dir, bool isLeft)
    {
        if (isLeft && !string.IsNullOrEmpty(leftSlot)) return;
        if (!isLeft && !string.IsNullOrEmpty(rightSlot)) return;

        Vector2Int neighborPos = ownerCell.GridPosition + dir.ToVector2Int();
        if (GridManager.Instance == null || !GridManager.Instance.IsInBounds(neighborPos)) return;

        GridCell neighborCell = GridManager.Instance.GetCell(neighborPos);
        if (neighborCell != null && neighborCell.HasItem)
        {
            Item item = neighborCell.CurrentItem;
            if (item.Data == null || item.Data.IsClog || item.IsSucked || item.IsMoving) return;

            item.IsSucked = true;
            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.UnregisterItem(item);
            }

            StartCoroutine(SuckRoutine(item, isLeft));
        }
    }

    private System.Collections.IEnumerator SuckRoutine(Item item, bool isLeft)
    {
        Vector3 startPos = item.transform.position;
        Vector3 endPos = GridManager.Instance.GridToWorld(ownerCell.GridPosition);
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / 4.0f; // 4.0f is moveSpeed
        if (duration < 0.05f) duration = 0.05f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            if (item != null && item.transform != null)
            {
                item.transform.position = Vector3.Lerp(startPos, endPos, t);
            }
            yield return null;
        }

        if (item != null)
        {
            if (isLeft)
            {
                leftSlot = item.Data.ItemText;
                leftMergeCount = item.Data.MergeCount;
            }
            else
            {
                rightSlot = item.Data.ItemText;
                rightMergeCount = item.Data.MergeCount;
            }

            item.DestroyItem();
            UpdateUI();
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
