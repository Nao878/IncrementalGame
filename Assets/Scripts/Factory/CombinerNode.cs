using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 順不同・全方位対応の合成機。
/// 隣接するマスからアイテムを吸い込み、スロットが2つ埋まったら合体を試みる。
/// </summary>
public class CombinerNode : FacilityNode
{
    // スロット管理（最大2つ）
    private List<string> storedKanjis = new List<string>();
    private List<int> storedMergeCounts = new List<int>();

    private TMPro.TextMeshPro[] slotTexts = new TMPro.TextMeshPro[2];
    private GameObject alertGo;

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
        col.isTrigger = true;

        // 出力矢印
        GameObject outArrow = new GameObject("OutArrow");
        outArrow.transform.SetParent(transform, false);
        SpriteRenderer outSr = outArrow.AddComponent<SpriteRenderer>();
        outSr.sprite = SpriteFactory.GetArrowSprite();
        outSr.color = new Color(0.9f, 0.9f, 0.2f, 0.8f);
        outSr.sortingOrder = 2;
        outArrow.transform.localRotation = Quaternion.Euler(0f, 0f, direction.ToZRotation());
        SpriteFactory.ApplyUnlitMaterial(outSr);

        // 全方位の吸入インジケーター（出力方向以外）
        CreateInputPin("InputPin_Back", direction.Opposite(), new Color(0.6f, 0.6f, 0.8f, 0.6f));
        CreateInputPin("InputPin_Left", direction.RotateCCW(), new Color(0.6f, 0.6f, 0.8f, 0.6f));
        CreateInputPin("InputPin_Right", direction.RotateCW(), new Color(0.6f, 0.6f, 0.8f, 0.6f));

        // テキスト表示用UIの作成（汎用的な上下配置）
        slotTexts[0] = CreateSlotText("Slot1Text", new Vector3(0f, 0.22f, -0.1f));
        slotTexts[1] = CreateSlotText("Slot2Text", new Vector3(0f, -0.1f, -0.1f));

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
        GameFontSettings.ApplyTo(tmp);
        tmp.text = "?";
        tmp.color = Color.white;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 7; // 少し小さめに
        tmp.rectTransform.sizeDelta = new Vector2(0.8f, 0.4f);
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
        pin.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        pin.transform.localPosition = new Vector3(dir.ToVector2Int().x * 0.4f, dir.ToVector2Int().y * 0.4f, 0f);
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

    public override bool CanAcceptItem(Item item) => false; // 自動吸入のみ
    public override bool OnItemArrived(Item item) => false;

    private void UpdateUI()
    {
        for (int i = 0; i < 2; i++)
        {
            if (slotTexts[i] == null) continue;
            if (i < storedKanjis.Count)
            {
                slotTexts[i].text = storedKanjis[i];
                slotTexts[i].color = Color.white;
            }
            else
            {
                slotTexts[i].text = "?";
                slotTexts[i].color = new Color(1f, 1f, 1f, 0.3f);
            }
        }
    }

    private void TryMerge()
    {
        if (storedKanjis.Count < 2) return;
        
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
            return;
        }

        HideAlert();

        KanjiDatabaseManager db = KanjiDatabaseManager.Instance;
        string mergedKanji = null;
        bool merged = false;

        if (db != null)
        {
            // パターン1: A + B
            merged = db.TryMergeKanji(new List<string> { storedKanjis[0], storedKanjis[1] }, out mergedKanji);
            
            // パターン2: B + A (順不同対応)
            if (!merged)
            {
                merged = db.TryMergeKanji(new List<string> { storedKanjis[1], storedKanjis[0] }, out mergedKanji);
            }
        }

        if (!merged || string.IsNullOrEmpty(mergedKanji))
        {
            // 失敗パターン：ゴミ（廃棄物）として排出
            int totalMerge = 0;
            foreach (int m in storedMergeCounts) totalMerge += m;
            
            string combinedText = storedKanjis[0] + storedKanjis[1];
            if (combinedText.Length > 2) combinedText = combinedText.Substring(0, 2);
            
            storedKanjis.Clear();
            storedMergeCounts.Clear();
            UpdateUI();

            Item wasteItem = ItemManager.Instance.CreateItem(outPos, combinedText, Color.gray, false);
            wasteItem.Data.MergeCount = totalMerge;
            return;
        }

        // 成功パターン
        int finalMergeCount = 0;
        foreach (int m in storedMergeCounts) finalMergeCount += m;
        finalMergeCount += 2;
        
        storedKanjis.Clear();
        storedMergeCounts.Clear();
        UpdateUI();

        Color newColor = ItemManager.Instance.GetMergeColor(finalMergeCount);
        Item newItem = ItemManager.Instance.CreateItem(outPos, mergedKanji, newColor, false);
        newItem.Data.MergeCount = finalMergeCount;
        newItem.PlayMergeSuccessPop();

        MergeEffectPlayer.PlayMergeBurst(GridManager.Instance.GridToWorld(ownerCell.GridPosition));
    }

    private void Update()
    {
        if (storedKanjis.Count < 2)
        {
            // 出力方向以外の3方向をチェック
            CheckAndSuckItem(outputDirection.Opposite());
            CheckAndSuckItem(outputDirection.RotateCCW());
            CheckAndSuckItem(outputDirection.RotateCW());
        }

        if (storedKanjis.Count >= 2)
        {
            TryMerge();
        }
    }

    private void CheckAndSuckItem(ConveyorDirection dir)
    {
        if (storedKanjis.Count >= 2) return;

        Vector2Int neighborPos = ownerCell.GridPosition + dir.ToVector2Int();
        if (GridManager.Instance == null || !GridManager.Instance.IsInBounds(neighborPos)) return;

        GridCell neighborCell = GridManager.Instance.GetCell(neighborPos);
        if (neighborCell != null && neighborCell.HasItem)
        {
            Item item = neighborCell.CurrentItem;
            if (item == null || item.Data == null || item.IsSucked || item.IsMoving) return;

            item.IsSucked = true;
            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.UnregisterItem(item);
            }

            StartCoroutine(SuckRoutine(item));
        }
    }

    private System.Collections.IEnumerator SuckRoutine(Item item)
    {
        if (item == null) yield break;

        Vector3 startPos = item.transform.position;
        Vector3 endPos = GridManager.Instance.GridToWorld(ownerCell.GridPosition);
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / 4.0f;
        if (duration < 0.05f) duration = 0.05f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (item == null || item.transform == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            
            if (item != null && item.transform != null)
                item.transform.position = Vector3.Lerp(startPos, endPos, t);
                
            yield return null;
        }

        if (item != null && item.Data != null)
        {
            if (storedKanjis.Count < 2)
            {
                storedKanjis.Add(item.Data.ItemText);
                storedMergeCounts.Add(item.Data.MergeCount);
                item.DestroyItem();
                UpdateUI();
                TryMerge();
            }
            else
            {
                // まれに同時に吸い込もうとしてスロットが埋まってしまった場合
                item.IsSucked = false;
                // コンベアに戻す処理がないので、とりあえず破棄せずに放置するか、あるいは消去する。
                // 安全のため、ここではスロットを溢れた分は消去する（基本的には起こらない）。
                item.DestroyItem();
            }
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
