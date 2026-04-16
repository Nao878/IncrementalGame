using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Mouse / keyboard input: build mode placement and clogged-block cleanup.
/// </summary>
public class InputHandler : MonoBehaviour
{
    private Camera mainCamera;
    private GridCell hoveredCell;
    private GridCell lastHoveredCell;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        UpdateHover();
        HandleInput();
    }

    private void UpdateHover()
    {
        if (GridManager.Instance == null)
            return;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2Int gridPos = GridManager.Instance.WorldToGrid(mouseWorldPos);
        hoveredCell = GridManager.Instance.GetCell(gridPos);

        if (lastHoveredCell != null && lastHoveredCell != hoveredCell)
        {
            lastHoveredCell.SetHighlight(false);
        }

        if (hoveredCell != null)
        {
            hoveredCell.SetHighlight(true);
        }

        lastHoveredCell = hoveredCell;
    }

    private void HandleInput()
    {
        if (hoveredCell == null) return;

        Vector2Int pos = hoveredCell.GridPosition;
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        if (Input.GetMouseButtonDown(1))
        {
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit != null)
            {
                Item item = hit.GetComponent<Item>();
                if (item != null && item.Data != null && item.Data.IsClog)
                {
                    item.TryRemoveClogByPlayer();
                    return;
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (BuildModeController.Instance != null && BuildModeController.Instance.IsDeleteMode)
            {
                bool deleted = false;
                // 先にコライダーを直接クリックしていないかチェック (合成機などを確実に削除するため)
                Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorld);
                foreach (var hit in hits)
                {
                    FacilityNode node = hit.GetComponentInParent<FacilityNode>();
                    if (node != null && node.OwnerCell != null)
                    {
                        BuildModeController.Instance.TryDeleteAt(node.OwnerCell.GridPosition);
                        deleted = true;
                        break;
                    }
                }

                if (!deleted)
                {
                    BuildModeController.Instance.TryDeleteAt(pos);
                }
            }
            else if (BuildModeController.Instance != null && BuildModeController.Instance.IsBuildMode)
            {
                BuildModeController.Instance.TryPlaceAt(pos);
            }
            else
            {
                if (hoveredCell != null && hoveredCell.HasFacility && hoveredCell.Facility is ItemGenerator gen)
                {
                    gen.CycleKanji();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (BuildModeController.Instance != null && BuildModeController.Instance.IsBuildMode)
                BuildModeController.Instance.RotatePreview();
        }
    }
}
