using UnityEngine;
using System.Collections;

/// <summary>
/// アイテム（ブロック）本体のコンポーネント。
/// コルーチンによる滑らかな移動を実装する。
/// </summary>
public class Item : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.0f;

    private ItemData data;
    private SpriteRenderer spriteRenderer;
    private Vector2Int currentGridPos;
    private bool isMoving = false;
    private bool isMarkedForMerge = false;

    /// <summary>アイテムデータ</summary>
    public ItemData Data { get { return data; } }

    /// <summary>現在のグリッド座標</summary>
    public Vector2Int CurrentGridPos { get { return currentGridPos; } }

    /// <summary>移動中かどうか</summary>
    public bool IsMoving { get { return isMoving; } }

    /// <summary>合成予約済みかどうか</summary>
    public bool IsMarkedForMerge
    {
        get { return isMarkedForMerge; }
        set { isMarkedForMerge = value; }
    }

    /// <summary>
    /// アイテムの初期化
    /// </summary>
    public void Initialize(Vector2Int gridPos, string text, Color color)
    {
        currentGridPos = gridPos;

        // SpriteRenderer設定
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = SpriteFactory.GetBlockSprite();
        spriteRenderer.sortingOrder = 5;
        SpriteFactory.ApplyUnlitMaterial(spriteRenderer);

        // ItemDataコンポーネント
        data = GetComponent<ItemData>();
        if (data == null)
            data = gameObject.AddComponent<ItemData>();

        data.Setup(text, color);
        spriteRenderer.color = color;

        // ワールド座標を設定
        transform.position = GridManager.Instance.GridToWorld(gridPos);
        gameObject.name = "Item_" + text;

        // セルに登録
        GridCell cell = GridManager.Instance.GetCell(gridPos);
        if (cell != null)
            cell.CurrentItem = this;
    }

    /// <summary>
    /// 指定したグリッド位置へ滑らかに移動するコルーチンを開始
    /// </summary>
    public void MoveTo(Vector2Int targetPos)
    {
        if (isMoving) return;
        StartCoroutine(MoveCoroutine(targetPos));
    }

    /// <summary>
    /// 滑らかな移動コルーチン
    /// </summary>
    private IEnumerator MoveCoroutine(Vector2Int targetPos)
    {
        isMoving = true;

        // 元のセルから登録解除
        GridCell fromCell = GridManager.Instance.GetCell(currentGridPos);
        if (fromCell != null && fromCell.CurrentItem == this)
            fromCell.CurrentItem = null;

        Vector3 startPos = transform.position;
        Vector3 endPos = GridManager.Instance.GridToWorld(targetPos);
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // イージング（SmoothStep）で自然な動き
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
        currentGridPos = targetPos;

        // 移動先のセルに登録
        GridCell toCell = GridManager.Instance.GetCell(targetPos);
        if (toCell != null)
            toCell.CurrentItem = this;

        isMoving = false;

        // 合成チェック通知
        if (ItemManager.Instance != null)
            ItemManager.Instance.CheckMergeAtCell(targetPos);
    }

    /// <summary>
    /// 色を更新する
    /// </summary>
    public void UpdateColor(Color newColor)
    {
        if (data != null)
            data.ItemColor = newColor;
        if (spriteRenderer != null)
            spriteRenderer.color = newColor;
    }

    /// <summary>
    /// アイテムを破壊する
    /// </summary>
    public void DestroyItem()
    {
        GridCell cell = GridManager.Instance.GetCell(currentGridPos);
        if (cell != null && cell.CurrentItem == this)
            cell.CurrentItem = null;

        Destroy(gameObject);
    }
}
