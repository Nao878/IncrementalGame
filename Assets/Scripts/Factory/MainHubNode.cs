using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// マップに固定配置される回収用巨大ハブ。
/// ここに到達したブロックは回収され、コインを獲得する。
/// </summary>
public class MainHubNode : FacilityNode
{
    private List<GridCell> occupiedCells = new List<GridCell>();

    public void SetupHub(List<GridCell> cells)
    {
        occupiedCells = cells;
        ownerCell = cells[0];

        foreach (var c in cells)
        {
            c.SetCellType(CellType.Collector); // Or some new CellType if needed
            c.Facility = this;
        }

        // ビジュアル作成: 最初のセルの位置から全体の中心を計算
        if (cells.Count > 0)
        {
            Vector3 center = Vector3.zero;
            foreach (var c in cells)
            {
                center += c.transform.position;
            }
            center /= cells.Count;

            transform.position = center;
            
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.GetSquareSprite();
            sr.color = new Color(0.1f, 0.4f, 0.8f, 1f);
            sr.sortingOrder = 1;
            SpriteFactory.ApplyUnlitMaterial(sr);
            
            // Assuming 2x2 grid for now
            float size = Mathf.Sqrt(cells.Count); 
            transform.localScale = new Vector3(size * 0.95f, size * 0.95f, 1f);

            GameObject textObj = new GameObject("HubText");
            textObj.transform.SetParent(transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            textObj.transform.localScale = new Vector3(1f/size, 1f/size, 1f); // Reset scale for text
            var tmp = textObj.AddComponent<TMPro.TextMeshPro>();
            GameFontSettings.ApplyTo(tmp);
            tmp.text = "納品ハブ\n(MAIN HUB)";
            tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontSize = 4;
            tmp.rectTransform.sizeDelta = new Vector2(size * 1.8f, size * 1.8f);
            tmp.sortingOrder = 2;
        }
    }

    public override FacilityType GetFacilityType() => FacilityType.MainHub;

    public override bool OnItemArrived(Item item)
    {
        if (item.Data == null) return true; // Safe check

        if (item.Data.IsClog)
        {
            // Clog block goes into hub? We can destroy without points or allow it.
            ItemManager.Instance.UnregisterAndDestroy(item);
            return true;
        }

        int stroke = 1;
        KanjiDatabaseManager db = KanjiDatabaseManager.Instance;
        if (db != null)
        {
            stroke = db.GetStrokeCount(item.Data.ItemText);
        }

        int reward = Mathf.Max(1, stroke) * 10;
        
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddCoins(reward);

        // Visual feedback
        ShowFloatingText("+" + reward, item.transform.position);

        ItemManager.Instance.UnregisterAndDestroy(item);
        return true;
    }

    private void ShowFloatingText(string text, Vector3 pos)
    {
        GameObject floater = new GameObject("Floater");
        floater.transform.position = pos + new Vector3(0f, 0.5f, -0.5f);
        var tmp = floater.AddComponent<TMPro.TextMeshPro>();
        GameFontSettings.ApplyTo(tmp);
        tmp.text = text;
        tmp.color = Color.yellow;
        tmp.fontSize = 18;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.sortingOrder = 15;
        
        // Simple float and destroy
        MonoBehaviour mb = ScoreManager.Instance; // Just need a runner
        if (mb != null) mb.StartCoroutine(FloatAndDestroy(floater));
    }

    private System.Collections.IEnumerator FloatAndDestroy(GameObject obj)
    {
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            obj.transform.position += Vector3.up * Time.deltaTime * 0.5f;
            yield return null;
        }
        Destroy(obj);
    }
}
