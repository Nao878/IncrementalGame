using UnityEngine;

/// <summary>
/// コンベアの配置と削除を管理する。
/// </summary>
public class ConveyorManager : MonoBehaviour
{
    public static ConveyorManager Instance { get; private set; }

    [Header("Default Settings")]
    [SerializeField] private ConveyorDirection defaultDirection = ConveyorDirection.Right;

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
    /// 指定したグリッド位置にコンベアを配置する
    /// </summary>
    public bool PlaceConveyor(Vector2Int pos)
    {
        GridCell cell = GridManager.Instance.GetCell(pos);
        if (cell == null) return false;

        // ジェネレーターのマスには配置不可
        if (cell.Type == CellType.Generator) return false;

        // 既にコンベアがある場合は回転させる
        if (cell.HasConveyor)
        {
            cell.Conveyor.RotateDirection();
            return true;
        }

        // 新しいコンベアを作成
        GameObject conveyorObj = new GameObject("Conveyor_" + pos.x + "_" + pos.y);
        conveyorObj.transform.position = GridManager.Instance.GridToWorld(pos);
        conveyorObj.transform.SetParent(cell.transform);

        ConveyorBelt conveyor = conveyorObj.AddComponent<ConveyorBelt>();
        conveyor.Initialize(cell, defaultDirection);

        cell.Conveyor = conveyor;
        cell.SetCellType(CellType.Conveyor);

        return true;
    }

    /// <summary>
    /// 指定したグリッド位置のコンベアを削除する
    /// </summary>
    public bool RemoveConveyor(Vector2Int pos)
    {
        GridCell cell = GridManager.Instance.GetCell(pos);
        if (cell == null) return false;
        if (!cell.HasConveyor) return false;
        if (cell.Type == CellType.Generator) return false;

        Destroy(cell.Conveyor.gameObject);
        cell.Conveyor = null;
        cell.SetCellType(CellType.Empty);

        return true;
    }

    /// <summary>
    /// 指定したグリッド位置のコンベアの方向を回転させる
    /// </summary>
    public bool RotateConveyor(Vector2Int pos)
    {
        GridCell cell = GridManager.Instance.GetCell(pos);
        if (cell == null) return false;
        if (!cell.HasConveyor) return false;

        cell.Conveyor.RotateDirection();
        return true;
    }
}
