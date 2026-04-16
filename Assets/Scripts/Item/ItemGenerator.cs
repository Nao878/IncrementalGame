using UnityEngine;

/// <summary>
/// 定期的にランダムな基本漢字パーツを生成する施設。
/// </summary>
public class ItemGenerator : FacilityNode
{
    [Header("Generator Settings")]
    [SerializeField] private float spawnInterval = 3.0f;
    private bool isActive = true;
    private float spawnTimer = 0f;
    private float initialDelay = 1.0f;
    private bool initialDelayPassed = false;

    private readonly string[] basicKanjis = new string[] { "木", "日", "火", "水", "月" };
    private readonly Color[] basicColors = new Color[] {
        new Color(0.9f, 0.4f, 0.3f, 1f),
        new Color(0.8f, 0.3f, 0.3f, 1f),
        new Color(0.9f, 0.5f, 0.2f, 1f),
        new Color(0.3f, 0.5f, 0.9f, 1f),
        new Color(0.8f, 0.8f, 0.3f, 1f)
    };

    public override void Initialize(GridCell cell, ConveyorDirection direction)
    {
        base.Initialize(cell, direction);
        spawnTimer = 0f;
        initialDelayPassed = false;

        // 見た目を作る (Indicator ▲)
        GameObject pivot = new GameObject("GenPivot");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localRotation = Quaternion.Euler(0f, 0f, direction.ToZRotation());
        pivot.transform.localPosition = new Vector3(0f, 0f, -0.2f);

        GameObject arrowObj = new GameObject("GenArrow");
        arrowObj.transform.SetParent(pivot.transform, false);
        arrowObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        var tmp = arrowObj.AddComponent<TMPro.TextMeshPro>();
        GameFontSettings.ApplyTo(tmp);
        tmp.text = "▲";
        tmp.color = new Color(0.9f, 0.9f, 0.2f, 1f);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 4;
        tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);
        tmp.sortingOrder = 3;
    }

    public override FacilityType GetFacilityType() => FacilityType.Generator;

    public override bool OnItemArrived(Item item)
    {
        // 発生機はアイテムを通過させるかブロックするか。
        // 基本的にはジェネレーターの上は通過可能にしてもよいが、ここでは何もしない（通過を許容）
        return false; 
    }

    private void Update()
    {
        if (!isActive || ownerCell == null) return;
        
        // 稼働モードかどうか？ 稼働モードじゃなければ（TimeScale=0なら）UpdateでdeltaTimeが進まないので気にしない
        
        // 初回ディレイ
        if (!initialDelayPassed)
        {
            initialDelay -= Time.deltaTime;
            if (initialDelay <= 0f)
                initialDelayPassed = true;
            return;
        }

        // 生成タイマー
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnItem();
        }
    }

    /// <summary>
    /// アイテムを1つ生成して排出方向へ移動させる
    /// </summary>
    private void SpawnItem()
    {
        if (ownerCell == null) return;

        // セル上にアイテムがあれば生成しない（詰まり防止）
        if (ownerCell.HasItem) return;

        if (ItemManager.Instance != null)
        {
            Vector2Int nextPos = ownerCell.GridPosition + outputDirection.ToVector2Int();
            GridCell nextCell = GridManager.Instance.GetCell(nextPos);
            
            // 排出先が詰まっていれば待機
            if (nextCell != null && nextCell.HasItem && !nextCell.CurrentItem.IsMoving) return;

            int r = Random.Range(0, basicKanjis.Length);
            string text = basicKanjis[r];
            Color color = basicColors[r];
            Item item = ItemManager.Instance.CreateItem(ownerCell.GridPosition, text, color);
            
            // 自動的に排出方向へ動かす
            if (nextCell != null)
            {
                item.MoveTo(nextPos);
            }
        }
    }

    public void SetGeneratorActive(bool active)
    {
        isActive = active;
    }
}
