using UnityEngine;

/// <summary>
/// アイテムを定期的に生成するジェネレーター。
/// 特定のグリッドセルに配置される。
/// Updateベースのタイマーで安定的にアイテムを生成する。
/// </summary>
public class ItemGenerator : MonoBehaviour
{
    [Header("Generator Settings")]
    [SerializeField] private float spawnInterval = 3.0f;
    [SerializeField] private string defaultItemText = "木";
    [SerializeField] private Color itemColor = new Color(0.9f, 0.4f, 0.3f, 1f);
    [SerializeField] private ConveyorDirection outputDirection = ConveyorDirection.Right;

    private Vector2Int gridPosition;
    private GridCell cell;
    private ConveyorBelt conveyor;
    private bool isActive = true;
    private float spawnTimer = 0f;
    private float initialDelay = 1.0f;
    private bool initialDelayPassed = false;

    /// <summary>グリッド位置</summary>
    public Vector2Int GridPosition { get { return gridPosition; } }

    /// <summary>
    /// ジェネレーターの初期化
    /// </summary>
    public void Initialize(Vector2Int pos, string text, Color color, ConveyorDirection dir)
    {
        gridPosition = pos;
        defaultItemText = text;
        itemColor = color;
        outputDirection = dir;

        cell = GridManager.Instance.GetCell(pos);
        if (cell != null)
        {
            cell.SetCellType(CellType.Generator);

            // ジェネレーター用コンベアを配置（アイテムの出力方向）
            GameObject conveyorObj = new GameObject("GenConveyor_" + pos.x + "_" + pos.y);
            conveyorObj.transform.position = GridManager.Instance.GridToWorld(pos);
            conveyorObj.transform.SetParent(cell.transform);

            conveyor = conveyorObj.AddComponent<ConveyorBelt>();
            conveyor.Initialize(cell, outputDirection);
            cell.Conveyor = conveyor;
        }

        spawnTimer = 0f;
        initialDelayPassed = false;
    }

    private void Update()
    {
        if (!isActive || cell == null) return;

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
    /// アイテムを1つ生成
    /// </summary>
    private void SpawnItem()
    {
        if (cell == null) return;

        // セル上にアイテムがあれば生成しない（詰まり防止）
        if (cell.HasItem) return;

        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.CreateItem(gridPosition, defaultItemText, itemColor);
        }
    }

    /// <summary>
    /// ジェネレーターの有効/無効切り替え
    /// </summary>
    public void SetGeneratorActive(bool active)
    {
        isActive = active;
    }
}
