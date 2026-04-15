using UnityEngine;

/// <summary>
/// グリッドの各セルの種類
/// </summary>
public enum CellType
{
    Empty,
    Conveyor,
    Generator,
    Combiner,
    Collector
}

/// <summary>
/// グリッドの1マスを表すコンポーネント。
/// セルの種類、配置されたコンベアやアイテムの参照を管理する。
/// </summary>
public class GridCell : MonoBehaviour
{
    [Header("Grid Position")]
    [SerializeField] private Vector2Int gridPosition;
    
    [Header("Cell State")]
    [SerializeField] private CellType cellType = CellType.Empty;

    private SpriteRenderer spriteRenderer;
    private ConveyorBelt conveyor;
    private Item currentItem;
    private FacilityNode facility;

    /// <summary>グリッド座標</summary>
    public Vector2Int GridPosition
    {
        get { return gridPosition; }
        set { gridPosition = value; }
    }

    /// <summary>セルの種類</summary>
    public CellType Type
    {
        get { return cellType; }
    }

    /// <summary>配置されたコンベア</summary>
    public ConveyorBelt Conveyor
    {
        get { return conveyor; }
        set { conveyor = value; }
    }

    /// <summary>このマス上にあるアイテム</summary>
    public Item CurrentItem
    {
        get { return currentItem; }
        set { currentItem = value; }
    }

    /// <summary>このマスに配置された施設</summary>
    public FacilityNode Facility
    {
        get { return facility; }
        set { facility = value; }
    }

    /// <summary>このセルにアイテムが存在するか</summary>
    public bool HasItem { get { return currentItem != null; } }

    /// <summary>このセルにコンベアが存在するか</summary>
    public bool HasConveyor { get { return conveyor != null; } }

    /// <summary>このセルに施設が存在するか</summary>
    public bool HasFacility { get { return facility != null; } }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    /// <summary>
    /// セルの種類を設定し、見た目を更新する
    /// </summary>
    public void SetCellType(CellType newType)
    {
        cellType = newType;
        RefreshVisual();
    }

    /// <summary>
    /// セルの初期化
    /// </summary>
    public void Initialize(Vector2Int pos)
    {
        gridPosition = pos;
        cellType = CellType.Empty;
        gameObject.name = "Cell_" + pos.x + "_" + pos.y;
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = SpriteFactory.GetSquareSprite();
        spriteRenderer.sortingOrder = 0;
        SpriteFactory.ApplyUnlitMaterial(spriteRenderer);
    }

    /// <summary>
    /// セルの見た目を種類に応じて更新
    /// </summary>
    public void RefreshVisual()
    {
        if (spriteRenderer == null) return;

        if (cellType == CellType.Empty)
        {
            spriteRenderer.sprite = SpriteFactory.GetSquareSprite();
            spriteRenderer.color = Color.white;
        }
        else if (cellType == CellType.Conveyor)
        {
            spriteRenderer.sprite = SpriteFactory.GetSquareSprite();
            spriteRenderer.color = new Color(0.85f, 0.85f, 0.9f, 1f);
        }
        else if (cellType == CellType.Generator)
        {
            spriteRenderer.sprite = SpriteFactory.GetGeneratorSprite();
            spriteRenderer.color = Color.white;
        }
        else if (cellType == CellType.Combiner)
        {
            spriteRenderer.sprite = SpriteFactory.GetSquareSprite();
            spriteRenderer.color = new Color(0.45f, 0.36f, 0.75f, 1f);
        }
        else if (cellType == CellType.Collector)
        {
            spriteRenderer.sprite = SpriteFactory.GetSquareSprite();
            spriteRenderer.color = new Color(0.25f, 0.6f, 0.65f, 1f);
        }
    }

    /// <summary>
    /// ハイライト表示
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        if (spriteRenderer == null) return;
        
        if (highlighted)
        {
            spriteRenderer.color = new Color(0.5f, 0.7f, 1f, 1f);
        }
        else
        {
            RefreshVisual();
        }
    }
}
