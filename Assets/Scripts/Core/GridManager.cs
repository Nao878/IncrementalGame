using UnityEngine;

/// <summary>
/// グリッドの生成と管理を行うシングルトン。
/// 座標変換やセル取得のAPIを提供する。
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float cellSize = 1.0f;

    private GridCell[,] grid;

    /// <summary>グリッド幅</summary>
    public int Width { get { return gridWidth; } }

    /// <summary>グリッド高さ</summary>
    public int Height { get { return gridHeight; } }

    /// <summary>セルサイズ</summary>
    public float CellSize { get { return cellSize; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// グリッドを生成する
    /// </summary>
    public void GenerateGrid()
    {
        // 既存のセルを削除
        if (grid != null)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    if (grid[x, y] != null)
                        Destroy(grid[x, y].gameObject);
                }
            }
        }

        grid = new GridCell[gridWidth, gridHeight];

        // グリッドの親オブジェクト
        Transform gridParent = transform.Find("GridCells");
        if (gridParent == null)
        {
            GameObject parentObj = new GameObject("GridCells");
            parentObj.transform.SetParent(transform);
            gridParent = parentObj.transform;
        }

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = GridToWorld(gridPos);

                GameObject cellObj = new GameObject();
                cellObj.transform.SetParent(gridParent);
                cellObj.transform.position = worldPos;

                GridCell cell = cellObj.AddComponent<GridCell>();
                cell.Initialize(gridPos);

                grid[x, y] = cell;
            }
        }

        Debug.Log("Grid generated: " + gridWidth + " x " + gridHeight);
    }

    /// <summary>
    /// グリッド座標からセルを取得。範囲外ならnullを返す。
    /// </summary>
    public GridCell GetCell(Vector2Int pos)
    {
        if (!IsInBounds(pos)) return null;
        return grid[pos.x, pos.y];
    }

    /// <summary>
    /// グリッド座標が範囲内かチェック
    /// </summary>
    public bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth &&
               pos.y >= 0 && pos.y < gridHeight;
    }

    /// <summary>
    /// ワールド座標からグリッド座標に変換
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / cellSize);
        int y = Mathf.RoundToInt(worldPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// グリッド座標からワールド座標に変換
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0f);
    }
}
