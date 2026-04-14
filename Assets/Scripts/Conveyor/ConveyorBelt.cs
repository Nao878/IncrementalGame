using UnityEngine;

/// <summary>
/// ベルトコンベアのコンポーネント。
/// 方向の管理と矢印の表示を担当する。
/// </summary>
public class ConveyorBelt : MonoBehaviour
{
    [Header("Conveyor Settings")]
    [SerializeField] private ConveyorDirection direction = ConveyorDirection.Up;
    [SerializeField] private float speed = 2.0f;

    private SpriteRenderer arrowRenderer;
    private GridCell parentCell;

    /// <summary>コンベアの方向</summary>
    public ConveyorDirection Direction
    {
        get { return direction; }
    }

    /// <summary>搬送速度</summary>
    public float Speed { get { return speed; } }

    /// <summary>親セル</summary>
    public GridCell ParentCell { get { return parentCell; } }

    /// <summary>
    /// コンベアの初期化
    /// </summary>
    public void Initialize(GridCell cell, ConveyorDirection dir)
    {
        parentCell = cell;
        direction = dir;

        // 矢印スプライトを子オブジェクトとして作成
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(transform);
        arrowObj.transform.localPosition = Vector3.zero;

        arrowRenderer = arrowObj.AddComponent<SpriteRenderer>();
        arrowRenderer.sprite = SpriteFactory.GetArrowSprite();
        arrowRenderer.sortingOrder = 2;
        arrowRenderer.color = new Color(0.3f, 0.85f, 0.4f, 0.9f);
        SpriteFactory.ApplyUnlitMaterial(arrowRenderer);

        RefreshArrowRotation();
    }

    /// <summary>
    /// 方向を特定の方向に設定
    /// </summary>
    public void SetDirection(ConveyorDirection dir)
    {
        direction = dir;
        RefreshArrowRotation();
    }

    /// <summary>
    /// 方向を時計回りに回転
    /// </summary>
    public void RotateDirection()
    {
        direction = direction.RotateCW();
        RefreshArrowRotation();
    }

    /// <summary>
    /// 搬送先のグリッド座標を取得
    /// </summary>
    public Vector2Int GetNextGridPosition()
    {
        if (parentCell == null) return Vector2Int.zero;
        return parentCell.GridPosition + direction.ToVector2Int();
    }

    /// <summary>
    /// 矢印のZ回転を方向に合わせて更新
    /// </summary>
    private void RefreshArrowRotation()
    {
        if (arrowRenderer == null) return;
        arrowRenderer.transform.localRotation = Quaternion.Euler(0, 0, direction.ToZRotation());
    }
}
