using UnityEngine;

/// <summary>
/// マウス・キーボード入力を処理する。
/// 左クリック: コンベア配置、右クリック: コンベア削除、Rキー: 方向回転
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

    /// <summary>
    /// マウスホバーでセルをハイライト
    /// </summary>
    private void UpdateHover()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2Int gridPos = GridManager.Instance.WorldToGrid(mouseWorldPos);
        hoveredCell = GridManager.Instance.GetCell(gridPos);

        // 前回のハイライトを解除
        if (lastHoveredCell != null && lastHoveredCell != hoveredCell)
        {
            lastHoveredCell.SetHighlight(false);
        }

        // 新しいセルをハイライト
        if (hoveredCell != null)
        {
            hoveredCell.SetHighlight(true);
        }

        lastHoveredCell = hoveredCell;
    }

    /// <summary>
    /// 入力処理
    /// </summary>
    private void HandleInput()
    {
        if (hoveredCell == null) return;

        Vector2Int pos = hoveredCell.GridPosition;

        // 左クリック: コンベア配置（既存なら回転）
        if (Input.GetMouseButtonDown(0))
        {
            ConveyorManager.Instance.PlaceConveyor(pos);
        }

        // 右クリック: コンベア削除
        if (Input.GetMouseButtonDown(1))
        {
            ConveyorManager.Instance.RemoveConveyor(pos);
        }

        // Rキー: コンベアの方向回転
        if (Input.GetKeyDown(KeyCode.R))
        {
            ConveyorManager.Instance.RotateConveyor(pos);
        }
    }
}
