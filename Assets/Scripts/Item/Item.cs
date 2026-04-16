using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Conveyor block with TMP label, movement, and optional clog state.
/// </summary>
public class Item : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.0f;

    private ItemData data;
    private SpriteRenderer spriteRenderer;
    private TextMeshPro labelTmp;
    private TextMeshPro bangTmp;
    private BoxCollider2D hitBox;
    private Vector2Int currentGridPos;
    private bool isMoving = false;
    private bool isMarkedForMerge = false;
    private Vector3 baseScale = Vector3.one;

    public ItemData Data { get { return data; } }
    public Vector2Int CurrentGridPos { get { return currentGridPos; } }
    public Vector2Int PreviousGridPos { get; private set; }
    public bool IsMoving { get { return isMoving; } }

    public bool IsMarkedForMerge
    {
        get { return isMarkedForMerge; }
        set { isMarkedForMerge = value; }
    }

    public bool IsSucked { get; set; } = false;

    public void Initialize(Vector2Int gridPos, string text, Color color, bool isClog = false)
    {
        currentGridPos = gridPos;
        baseScale = Vector3.one;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = SpriteFactory.GetBlockSprite();
        spriteRenderer.sortingOrder = 5;
        SpriteFactory.ApplyUnlitMaterial(spriteRenderer);

        data = GetComponent<ItemData>();
        if (data == null)
            data = gameObject.AddComponent<ItemData>();

        data.Setup(text, color, isClog);
        spriteRenderer.color = color;

        EnsureCollider();
        EnsureLabel(text, isClog);

        Vector3 worldPos = (GridManager.Instance != null)
            ? GridManager.Instance.GridToWorld(gridPos)
            : new Vector3(gridPos.x, gridPos.y, 0f);
        transform.position = worldPos;
        transform.localScale = baseScale;
        gameObject.name = "Item_" + text;

        GridCell cell = GridManager.Instance != null ? GridManager.Instance.GetCell(gridPos) : null;
        if (cell != null)
            cell.CurrentItem = this;
    }

    void EnsureCollider()
    {
        hitBox = GetComponent<BoxCollider2D>();
        if (hitBox == null)
            hitBox = gameObject.AddComponent<BoxCollider2D>();
        float cellSize = (GridManager.Instance != null) ? GridManager.Instance.CellSize : 1.0f;
        float s = cellSize * 0.88f;
        hitBox.size = new Vector2(s, s);
    }

    void EnsureLabel(string text, bool isClog)
    {
        if (labelTmp == null)
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);
            // Bring text slightly toward camera to avoid z-fighting on the block.
            labelObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            // TextMeshPro world-space glyph scale is large by default.
            labelObj.transform.localScale = Vector3.one * 0.12f;
            labelTmp = labelObj.AddComponent<TextMeshPro>();
            GameFontSettings.ApplyTo(labelTmp);
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.enableAutoSizing = false;
            labelTmp.fontSize = 24;
            labelTmp.overflowMode = TextOverflowModes.Overflow;
            labelTmp.rectTransform.sizeDelta = new Vector2(6f, 6f);
            labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
            MeshRenderer mr = labelTmp.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 7;
                mr.enabled = true;
            }
        }

        labelTmp.text = text;
        labelTmp.color = GetReadableTextColor(spriteRenderer != null ? spriteRenderer.color : Color.white);

        if (isClog)
        {
            if (bangTmp == null)
            {
                GameObject bangObj = new GameObject("Bang");
                bangObj.transform.SetParent(transform, false);
                bangObj.transform.localPosition = new Vector3(0f, 0.38f, 0f);
                bangTmp = bangObj.AddComponent<TextMeshPro>();
                GameFontSettings.ApplyTo(bangTmp);
                bangTmp.text = "詰まり";
                bangTmp.color = new Color(1f, 0.25f, 0.2f, 1f);
                bangTmp.alignment = TextAlignmentOptions.Center;
                bangTmp.enableAutoSizing = true;
                bangTmp.fontSizeMin = 8;
                bangTmp.fontSizeMax = 24;
                bangTmp.fontStyle = FontStyles.Bold;
                bangTmp.rectTransform.sizeDelta = new Vector2(2.5f, 0.6f);
                MeshRenderer bmr = bangTmp.GetComponent<MeshRenderer>();
                if (bmr != null)
                    bmr.sortingOrder = 8;
            }
            bangTmp.gameObject.SetActive(true);
        }
        else if (bangTmp != null)
            bangTmp.gameObject.SetActive(false);
    }

    public void TryRemoveClogByPlayer()
    {
        if (data == null || !data.IsClog) return;
        if (ItemManager.Instance != null)
            ItemManager.Instance.UnregisterAndDestroy(this);
    }

    public void PlayMergeSuccessPop()
    {
        StartCoroutine(MergePopCoroutine());
    }

    IEnumerator MergePopCoroutine()
    {
        float dur = 0.22f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.38f;
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;
    }

    public void MoveTo(Vector2Int targetPos)
    {
        if (data != null && data.IsClog) return;
        if (isMoving) return;
        StartCoroutine(MoveCoroutine(targetPos));
    }

    private IEnumerator MoveCoroutine(Vector2Int targetPos)
    {
        isMoving = true;
        PreviousGridPos = currentGridPos;

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
            if (this == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            if (transform != null)
                transform.position = Vector3.Lerp(startPos, endPos, t);
                
            yield return null;
        }

        if (this == null) yield break;

        transform.position = endPos;
        currentGridPos = targetPos;

        GridCell toCell = GridManager.Instance.GetCell(targetPos);
        if (toCell != null)
            toCell.CurrentItem = this;

        isMoving = false;

        if (ItemManager.Instance != null)
            ItemManager.Instance.NotifyItemArrivedAtCell(this);
    }

    public void UpdateColor(Color newColor)
    {
        if (data != null)
            data.ItemColor = newColor;
        if (spriteRenderer != null)
            spriteRenderer.color = newColor;
    }

    public void DestroyItem()
    {
        GridCell cell = GridManager.Instance.GetCell(currentGridPos);
        if (cell != null && cell.CurrentItem == this)
            cell.CurrentItem = null;

        Destroy(gameObject);
    }

    Color GetReadableTextColor(Color blockColor)
    {
        float luma = (blockColor.r * 0.299f) + (blockColor.g * 0.587f) + (blockColor.b * 0.114f);
        return luma > 0.55f ? Color.black : Color.white;
    }

    // --- Deletion Mode Feedback ---

    private void OnMouseEnter()
    {
        if (BuildModeController.Instance != null && BuildModeController.Instance.IsDeleteMode)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0.5f);
        }
    }

    private void OnMouseExit()
    {
        // Restore opacity
        if (spriteRenderer != null && data != null)
        {
            spriteRenderer.color = new Color(data.ItemColor.r, data.ItemColor.g, data.ItemColor.b, 1.0f);
        }
    }
}
