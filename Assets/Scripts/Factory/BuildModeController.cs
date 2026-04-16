using UnityEngine;

/// <summary>
/// Handles build mode state, ghost preview, rotation, and placement.
/// </summary>
public class BuildModeController : MonoBehaviour
{
    public static BuildModeController Instance { get; private set; }

    [SerializeField] private int conveyorCost = 20;
    [SerializeField] private int generatorCost = 80;
    [SerializeField] private int combinerCost = 40;
    [SerializeField] private int incineratorCost = 30;

    private FacilityType selectedType = FacilityType.None;
    private ConveyorDirection previewDirection = ConveyorDirection.Up;
    private GameObject ghost;
    private SpriteRenderer ghostSprite;
    private SpriteRenderer ghostArrow;
    private GameObject ghostGenPivot;
    private GameObject ghostCombinerPivot;

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
        if (ScoreManager.Instance != null && !ScoreManager.Instance.HasEarnedFirstIncome) return 0;
        
        if (type == FacilityType.Conveyor) return conveyorCost;
        if (type == FacilityType.Generator) return generatorCost;
        if (type == FacilityType.Combiner) return combinerCost;
        if (type == FacilityType.Incinerator) return incineratorCost;
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

        // Generator Preview UI
        ghostGenPivot = new GameObject("GhostGenPivot");
        ghostGenPivot.transform.SetParent(ghost.transform, false);
        ghostGenPivot.transform.localPosition = new Vector3(0f, 0f, -0.2f);
        GameObject gA = new GameObject("GenArrow");
        gA.transform.SetParent(ghostGenPivot.transform, false);
        gA.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        var genTmp = gA.AddComponent<TMPro.TextMeshPro>();
        GameFontSettings.ApplyTo(genTmp);
        genTmp.text = "▲";
        genTmp.color = new Color(0.9f, 0.9f, 0.2f, 0.6f);
        genTmp.alignment = TMPro.TextAlignmentOptions.Center;
        genTmp.fontSize = 4;
        genTmp.rectTransform.sizeDelta = new Vector2(1f, 1f);
        genTmp.sortingOrder = 32;

        // Combiner Preview UI
        ghostCombinerPivot = new GameObject("GhostCombinerPivot");
        ghostCombinerPivot.transform.SetParent(ghost.transform, false);
        
        GameObject outA = new GameObject("Out");
        outA.transform.SetParent(ghostCombinerPivot.transform, false);
        var outSr = outA.AddComponent<SpriteRenderer>();
        outSr.sprite = SpriteFactory.GetArrowSprite();
        outSr.sortingOrder = 31; 
        outSr.color = new Color(0.9f, 0.9f, 0.2f, 0.6f);
        SpriteFactory.ApplyUnlitMaterial(outSr);

        CreateGhostPin("LeftPin", ConveyorDirection.Up.RotateCCW(), new Color(0.8f, 0.4f, 0.4f, 0.5f), ghostCombinerPivot.transform);
        CreateGhostPin("RightPin", ConveyorDirection.Up.RotateCW(), new Color(0.4f, 0.4f, 0.8f, 0.5f), ghostCombinerPivot.transform);

        ghostGenPivot.SetActive(false);
        ghostCombinerPivot.SetActive(false);
    }

    private void CreateGhostPin(string name, ConveyorDirection dir, Color color, Transform parent)
    {
        GameObject pin = new GameObject(name);
        pin.transform.SetParent(parent, false);
        SpriteRenderer sr = pin.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.GetSquareSprite();
        sr.color = color;
        sr.sortingOrder = 32;
        pin.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        pin.transform.localPosition = new Vector3(dir.ToVector2Int().x * 0.35f, dir.ToVector2Int().y * 0.35f, 0f);
        SpriteFactory.ApplyUnlitMaterial(sr);

        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(pin.transform, false);
        SpriteRenderer arrowSr = arrowObj.AddComponent<SpriteRenderer>();
        arrowSr.sprite = SpriteFactory.GetArrowSprite();
        arrowSr.color = new Color(1f, 1f, 1f, 0.6f);
        arrowSr.sortingOrder = 33;
        arrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, dir.ToZRotation() + 180f);
        SpriteFactory.ApplyUnlitMaterial(arrowSr);
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
        
        float zRot = previewDirection.ToZRotation();
        if (ghostArrow != null)
            ghostArrow.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
        if (ghostGenPivot != null) 
            ghostGenPivot.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
        if (ghostCombinerPivot != null) 
            ghostCombinerPivot.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);

        if (ghostSprite != null)
        {
            if (selectedType == FacilityType.Conveyor)
                ghostSprite.color = new Color(0.65f, 0.9f, 0.65f, 0.45f);
            else if (selectedType == FacilityType.Generator)
                ghostSprite.color = new Color(0.9f, 0.6f, 0.3f, 0.45f); // 発生機はオレンジがかった色
            else if (selectedType == FacilityType.Combiner)
                ghostSprite.color = new Color(0.4f, 0.4f, 0.5f, 0.45f); // 合成機
            else if (selectedType == FacilityType.Incinerator)
                ghostSprite.color = new Color(0.2f, 0.2f, 0.2f, 0.45f); // 焼却炉
            else if (selectedType == FacilityType.Delete)
                ghostSprite.color = new Color(0.95f, 0.25f, 0.25f, 0.5f);
        }

        // Toggle Specific UI
        bool isConveyor = selectedType == FacilityType.Conveyor;
        bool isGen = selectedType == FacilityType.Generator;
        bool isComb = selectedType == FacilityType.Combiner;
        
        if (ghostArrow != null) ghostArrow.gameObject.SetActive(isConveyor);
        if (ghostGenPivot != null) ghostGenPivot.gameObject.SetActive(isGen);
        if (ghostCombinerPivot != null) ghostCombinerPivot.gameObject.SetActive(isComb);

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
            if (ghostArrow != null) ghostArrow.gameObject.SetActive(false);
            if (ghostGenPivot != null) ghostGenPivot.gameObject.SetActive(false);
            if (ghostCombinerPivot != null) ghostCombinerPivot.gameObject.SetActive(false);
        }
        else
        {
            if (deleteMarkerTmp != null)
                deleteMarkerTmp.gameObject.SetActive(false);
            // Re-apply facility specific display logic since we left delete mode
            bool isConveyor = selectedType == FacilityType.Conveyor;
            bool isGen = selectedType == FacilityType.Generator;
            bool isComb = selectedType == FacilityType.Combiner;
            
            if (ghostArrow != null) ghostArrow.gameObject.SetActive(isConveyor);
            if (ghostGenPivot != null) ghostGenPivot.gameObject.SetActive(isGen);
            if (ghostCombinerPivot != null) ghostCombinerPivot.gameObject.SetActive(isComb);
        }
    }
}
