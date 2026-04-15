using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Tick-driven conveyor movement and left/right merge resolution.
/// </summary>
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    struct ConvergenceParticipant
    {
        public Item Item;
        public Vector2Int FromGrid;
    }

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

        Dictionary<Vector2Int, List<ConvergenceParticipant>> moveTargets = new Dictionary<Vector2Int, List<ConvergenceParticipant>>();

        foreach (Item item in allItems)
        {
            if (item == null || item.IsMoving || item.IsMarkedForMerge) continue;
            if (item.Data != null && item.Data.IsClog) continue;

            GridCell currentCell = GridManager.Instance.GetCell(item.CurrentGridPos);
            if (currentCell == null || !currentCell.HasConveyor) continue;

            ConveyorBelt conveyor = currentCell.Conveyor;
            Vector2Int nextPos = conveyor.GetNextGridPosition();

            if (!GridManager.Instance.IsInBounds(nextPos)) continue;

            GridCell nextCell = GridManager.Instance.GetCell(nextPos);
            if (nextCell == null) continue;

            if (nextCell.HasItem && !nextCell.CurrentItem.IsMoving)
            {
                Item occ = nextCell.CurrentItem;
                if (occ != null && occ.Data != null && occ.Data.IsClog)
                    continue;

                if (!nextCell.HasConveyor && !nextCell.HasFacility)
                    continue;
            }

            if (!moveTargets.ContainsKey(nextPos))
                moveTargets[nextPos] = new List<ConvergenceParticipant>();
            moveTargets[nextPos].Add(new ConvergenceParticipant
            {
                Item = item,
                FromGrid = item.CurrentGridPos
            });
        }

        foreach (KeyValuePair<Vector2Int, List<ConvergenceParticipant>> kvp in moveTargets)
        {
            Vector2Int targetPos = kvp.Key;
            List<ConvergenceParticipant> participants = kvp.Value;

            if (participants.Count == 1)
            {
                GridCell targetCell = GridManager.Instance.GetCell(targetPos);
                if (targetCell != null && !targetCell.HasItem)
                {
                    participants[0].Item.MoveTo(targetPos);
                }
            }
            else if (participants.Count >= 2)
            {
                StartCoroutine(MergeSequence(targetPos, participants));
            }
        }

        isTicking = false;
    }

    private IEnumerator MergeSequence(Vector2Int mergePos, List<ConvergenceParticipant> participants)
    {
        foreach (ConvergenceParticipant p in participants)
        {
            if (p.Item == null) continue;
            p.Item.IsMarkedForMerge = true;
            p.Item.MoveTo(mergePos);
        }

        bool allDone = false;
        while (!allDone)
        {
            allDone = true;
            foreach (ConvergenceParticipant p in participants)
            {
                if (p.Item != null && p.Item.IsMoving)
                {
                    allDone = false;
                    break;
                }
            }
            yield return null;
        }

        PerformMerge(mergePos, participants);
    }

    private void PerformMerge(Vector2Int pos, List<ConvergenceParticipant> participants)
    {
        if (participants.Count < 2) return;

        List<Item> alive = new List<Item>();
        foreach (ConvergenceParticipant p in participants)
        {
            if (p.Item != null)
                alive.Add(p.Item);
        }

        if (alive.Count < 2)
            return;

        foreach (Item it in alive)
            it.IsMarkedForMerge = false;

        if (alive.Count != 2)
        {
            CreateClogFromParticipants(pos, participants);
            return;
        }

        Vector2Int leftNeighbor = pos + Vector2Int.left;
        Vector2Int rightNeighbor = pos + Vector2Int.right;

        Item leftItem = null;
        Item rightItem = null;
        foreach (ConvergenceParticipant p in participants)
        {
            if (p.Item == null) continue;
            if (p.FromGrid == leftNeighbor)
                leftItem = p.Item;
            else if (p.FromGrid == rightNeighbor)
                rightItem = p.Item;
        }

        if (leftItem == null || rightItem == null)
        {
            CreateClogFromParticipants(pos, participants);
            return;
        }

        string leftK = leftItem.Data != null ? leftItem.Data.ItemText : "";
        string rightK = rightItem.Data != null ? rightItem.Data.ItemText : "";

        KanjiDatabaseManager db = KanjiDatabaseManager.Instance;
        string mergedKanji = string.Empty;
        bool merged = db != null && db.TryMergeKanji(new List<string> { leftK, rightK }, out mergedKanji);

        if (!merged || string.IsNullOrEmpty(mergedKanji))
        {
            CreateClogFromParticipants(pos, participants);
            return;
        }

        int totalMergeCount = 0;
        foreach (Item item in alive)
        {
            if (item == null || item.Data == null) continue;
            totalMergeCount += item.Data.MergeCount + 1;
        }

        foreach (Item item in alive)
        {
            allItems.Remove(item);
            item.DestroyItem();
        }

        int colorIndex = Mathf.Min(totalMergeCount, mergeColors.Length - 1);
        Color newColor = mergeColors[colorIndex];

        Vector3 worldPos = GridManager.Instance.GridToWorld(pos);
        MergeEffectPlayer.PlayMergeBurst(worldPos);

        Item newItem = CreateItem(pos, mergedKanji, newColor, false);
        newItem.Data.MergeCount = totalMergeCount;
        newItem.PlayMergeSuccessPop();

        Debug.Log("Merged at (" + pos.x + "," + pos.y + "): " + leftK + "+" + rightK + " => " + mergedKanji);
    }

    void CreateClogFromParticipants(Vector2Int pos, List<ConvergenceParticipant> participants)
    {
        string label = "";
        foreach (ConvergenceParticipant p in participants)
        {
            if (p.Item == null || p.Item.Data == null) continue;
            label += p.Item.Data.ItemText;
            allItems.Remove(p.Item);
            p.Item.DestroyItem();
        }

        if (string.IsNullOrEmpty(label))
            label = "?";

        CreateItem(pos, label, clogBlockColor, true);
        Debug.LogWarning("Clog at (" + pos.x + "," + pos.y + "): " + label);
    }

    public void CheckMergeAtCell(Vector2Int pos)
    {
        GridCell cell = GridManager.Instance.GetCell(pos);
        if (cell == null) return;
        if (!cell.HasFacility || cell.CurrentItem == null) return;
        cell.Facility.OnItemArrived(cell.CurrentItem);
    }

    public Color GetMergeColor(int mergeCount)
    {
        int index = Mathf.Min(mergeCount, mergeColors.Length - 1);
        return mergeColors[index];
    }
}
