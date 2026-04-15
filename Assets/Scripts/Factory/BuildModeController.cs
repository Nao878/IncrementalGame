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
        }
    }

    void SetGhostVisible(bool visible)
    {
        if (ghost != null)
            ghost.SetActive(visible);
    }
}
