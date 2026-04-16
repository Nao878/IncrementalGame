using UnityEngine;

/// <summary>
/// Handles build mode state, ghost preview, rotation, and placement.
/// </summary>
public class BuildModeController : MonoBehaviour
{
    public static BuildModeController Instance { get; private set; }

    [SerializeField] private int conveyorCost = 20;
    [SerializeField] private int combinerCost = 80;
    [SerializeField] private int collectorCost = 60;

    private FacilityType selectedType = FacilityType.None;
    private ConveyorDirection previewDirection = ConveyorDirection.Up;
    private GameObject ghost;
    private SpriteRenderer ghostSprite;
    private SpriteRenderer ghostArrow;

    public FacilityType SelectedType => selectedType;
    public ConveyorDirection PreviewDirection => previewDirection;
    public bool IsBuildMode => selectedType != FacilityType.None;
    public bool IsDeleteMode => selectedType == FacilityType.Delete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!IsBuildMode) return;
        UpdateGhostPosition();
    }

    public void SelectFacility(FacilityType type)
    {
        selectedType = type;
        if (type == FacilityType.None)
        {
            SetGhostVisible(false);
            return;
        }
        EnsureGhost();
        SetGhostVisible(true);
        UpdateGhostVisual();
    }

    /// <summary>
    /// 削除モード時に施設を撤去する
    /// </summary>
    public bool TryDeleteAt(Vector2Int pos)
    {
        if (!IsDeleteMode) return false;
        GridCell cell = GridManager.Instance?.GetCell(pos);
        if (cell == null) return false;
        if (cell.Type == CellType.Generator) return false;
        if (cell.Type == CellType.Empty) return false;

        // セル上のアイテムがあれば破壊
        if (cell.CurrentItem != null)
        {
            ItemManager.Instance?.UnregisterAndDestroy(cell.CurrentItem);
            cell.CurrentItem = null;
        }

        // コンベアを削除
        if (cell.Conveyor != null)
        {
            Destroy(cell.Conveyor.gameObject);
            cell.Conveyor = null;
        }

        // 施設を削除
        if (cell.Facility != null)
        {
            Destroy(cell.Facility.gameObject);
            cell.Facility = null;
        }

        cell.SetCellType(CellType.Empty);
        return true;
    }

    public void RotatePreview()
    {
        previewDirection = previewDirection.RotateCW();
        UpdateGhostVisual();
    }

    public bool TryPlaceAt(Vector2Int pos)
    {
        if (!IsBuildMode) return false;
        int cost = GetCost(selectedType);
        if (ScoreManager.Instance != null && ScoreManager.Instance.Coins < cost)
            return false;

        bool placed = FacilityPlacementManager.Instance != null &&
                      FacilityPlacementManager.Instance.PlaceFacility(selectedType, pos, previewDirection);
        if (!placed) return false;
        ScoreManager.Instance?.SpendCoins(cost);
        return true;
    }

    public int GetCost(FacilityType type)
    {
        if (type == FacilityType.Conveyor) return conveyorCost;
        if (type == FacilityType.Combiner) return combinerCost;
        if (type == FacilityType.Collector) return collectorCost;
        return 0;
    }

    void EnsureGhost()
    {
        if (ghost != null) return;
        ghost = new GameObject("BuildPreviewGhost");
        ghostSprite = ghost.AddComponent<SpriteRenderer>();
        ghostSprite.sortingOrder = 30;
        ghostSprite.sprite = SpriteFactory.GetSquareSprite();
        ghostSprite.color = new Color(1f, 1f, 1f, 0.45f);
        SpriteFactory.ApplyUnlitMaterial(ghostSprite);

        GameObject arrow = new GameObject("GhostArrow");
        arrow.transform.SetParent(ghost.transform, false);
        ghostArrow = arrow.AddComponent<SpriteRenderer>();
        ghostArrow.sprite = SpriteFactory.GetArrowSprite();
        ghostArrow.sortingOrder = 31;
        ghostArrow.color = new Color(1f, 1f, 1f, 0.65f);
        SpriteFactory.ApplyUnlitMaterial(ghostArrow);
    }

    void UpdateGhostPosition()
    {
        Camera cam = Camera.main;
        if (cam == null || GridManager.Instance == null) return;
        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        Vector2Int gp = GridManager.Instance.WorldToGrid(world);
        if (!GridManager.Instance.IsInBounds(gp))
        {
            SetGhostVisible(false);
            return;
        }
        SetGhostVisible(true);
        ghost.transform.position = GridManager.Instance.GridToWorld(gp);
    }

    void UpdateGhostVisual()
    {
        if (ghost == null) return;
        if (ghostArrow != null)
            ghostArrow.transform.localRotation = Quaternion.Euler(0f, 0f, previewDirection.ToZRotation());

        if (ghostSprite != null)
        {
            if (selectedType == FacilityType.Conveyor)
                ghostSprite.color = new Color(0.65f, 0.9f, 0.65f, 0.45f);
            else if (selectedType == FacilityType.Combiner)
                ghostSprite.color = new Color(0.7f, 0.5f, 0.95f, 0.45f);
            else if (selectedType == FacilityType.Collector)
                ghostSprite.color = new Color(0.35f, 0.9f, 0.95f, 0.45f);
            else if (selectedType == FacilityType.Delete)
                ghostSprite.color = new Color(0.95f, 0.25f, 0.25f, 0.5f);
        }

        // 削除モードの「×」マーク表示切替
        UpdateDeleteMarker();
    }

    void SetGhostVisible(bool visible)
    {
        if (ghost != null)
            ghost.SetActive(visible);
    }

    private TMPro.TextMeshPro deleteMarkerTmp;

    void UpdateDeleteMarker()
    {
        if (ghost == null) return;

        if (selectedType == FacilityType.Delete)
        {
            if (deleteMarkerTmp == null)
            {
                GameObject markerObj = new GameObject("DeleteMarker");
                markerObj.transform.SetParent(ghost.transform, false);
                markerObj.transform.localPosition = new Vector3(0f, 0f, -0.2f);
                deleteMarkerTmp = markerObj.AddComponent<TMPro.TextMeshPro>();
                GameFontSettings.ApplyTo(deleteMarkerTmp);
                deleteMarkerTmp.text = "×";
                deleteMarkerTmp.color = new Color(1f, 0.2f, 0.2f, 0.85f);
                deleteMarkerTmp.fontSize = 10;
                deleteMarkerTmp.alignment = TMPro.TextAlignmentOptions.Center;
                deleteMarkerTmp.sortingOrder = 35;

                RectTransform rt = deleteMarkerTmp.rectTransform;
                rt.sizeDelta = new Vector2(1f, 1f);
            }
            deleteMarkerTmp.gameObject.SetActive(true);
            // 削除モードでは方向矢印を非表示に
            if (ghostArrow != null) ghostArrow.gameObject.SetActive(false);
        }
        else
        {
            if (deleteMarkerTmp != null)
                deleteMarkerTmp.gameObject.SetActive(false);
            if (ghostArrow != null) ghostArrow.gameObject.SetActive(true);
        }
    }
}
