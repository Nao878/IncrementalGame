using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Tick-driven conveyor movement and left/right merge resolution.
/// </summary>
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("Tick Settings")]
    [SerializeField] private float tickInterval = 0.8f;

    [Header("Merge Colors")]
    [SerializeField] private Color[] mergeColors = new Color[]
    {
        new Color(0.9f, 0.4f, 0.3f, 1f),
        new Color(0.3f, 0.5f, 0.9f, 1f),
        new Color(0.3f, 0.85f, 0.4f, 1f),
        new Color(0.9f, 0.8f, 0.2f, 1f),
        new Color(0.7f, 0.3f, 0.9f, 1f),
        new Color(1f, 0.6f, 0.2f, 1f),
        new Color(0.2f, 0.9f, 0.9f, 1f),
        new Color(0.95f, 0.95f, 0.95f, 1f),
    };

    [Header("Clog")]
    [SerializeField] private Color clogBlockColor = new Color(0.5f, 0.22f, 0.22f, 1f);

    private List<Item> allItems = new List<Item>();
    private float tickTimer = 0f;
    private bool isTicking = false;

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
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval && !isTicking)
        {
            tickTimer = 0f;
            ProcessTick();
        }
    }

    public Item CreateItem(Vector2Int gridPos, string text, Color color, bool isClog = false)
    {
        GameObject itemObj = new GameObject("Item_" + text);
        Item item = itemObj.AddComponent<Item>();
        item.Initialize(gridPos, text, color, isClog);

        allItems.Add(item);
        return item;
    }

    public void UnregisterAndDestroy(Item item)
    {
        if (item == null) return;
        allItems.Remove(item);
        item.DestroyItem();
    }

    private void ProcessTick()
    {
        isTicking = true;

        allItems.RemoveAll(item => item == null);

        Dictionary<Vector2Int, List<Item>> moveTargets = new Dictionary<Vector2Int, List<Item>>();

        foreach (Item item in allItems)
        {
            if (item == null || item.IsMoving) continue;
            if (item.Data != null && item.Data.IsClog) continue;

            GridCell currentCell = GridManager.Instance.GetCell(item.CurrentGridPos);
            if (currentCell == null || !currentCell.HasConveyor) continue;

            ConveyorBelt conveyor = currentCell.Conveyor;
            Vector2Int nextPos = conveyor.GetNextGridPosition();

            if (!GridManager.Instance.IsInBounds(nextPos)) continue;

            GridCell nextCell = GridManager.Instance.GetCell(nextPos);
            if (nextCell == null) continue;

            // 施設の場合は受け入れ可能かチェック
            if (nextCell.HasFacility)
            {
                if (!nextCell.Facility.CanAcceptItem(item))
                    continue; // 施設がいっぱいなどの理由で拒否
            }
            else
            {
                if (nextCell.HasItem && !nextCell.CurrentItem.IsMoving)
                    continue; // コンベア上が詰まっている

                if (!nextCell.HasConveyor)
                    continue; // コンベアがないマスには進めない
            }

            if (!moveTargets.ContainsKey(nextPos))
                moveTargets[nextPos] = new List<Item>();
            moveTargets[nextPos].Add(item);
        }

        foreach (KeyValuePair<Vector2Int, List<Item>> kvp in moveTargets)
        {
            Vector2Int targetPos = kvp.Key;
            List<Item> movingItems = kvp.Value;

            GridCell targetCell = GridManager.Instance.GetCell(targetPos);
            if (targetCell == null) continue;

            if (targetCell.HasFacility && targetCell.Facility.GetFacilityType() == FacilityType.Combiner)
            {
                // 合成機へは複数方向からの同時進入を許可
                foreach(var it in movingItems)
                    it.MoveTo(targetPos);
            }
            else
            {
                // 通常のコンベアマスには1つだけ進入許可（複数重なるのを防ぐ）
                movingItems[0].MoveTo(targetPos);
            }
        }

        isTicking = false;
    }

    public void NotifyItemArrivedAtCell(Item item)
    {
        GridCell cell = GridManager.Instance.GetCell(item.CurrentGridPos);
        if (cell == null) return;
        if (!cell.HasFacility) return; // Only process arrival for facilities
        
        bool consumed = cell.Facility.OnItemArrived(item);
    }

    public Color GetMergeColor(int mergeCount)
    {
        int index = Mathf.Min(mergeCount, mergeColors.Length - 1);
        return mergeColors[index];
    }
}
