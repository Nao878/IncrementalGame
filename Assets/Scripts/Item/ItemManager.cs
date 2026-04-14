using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// アイテムの移動ティック管理と合流判定を担当するシングルトン。
/// 一定間隔でコンベア上のアイテムを次のマスへ移動させる。
/// </summary>
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("Tick Settings")]
    [SerializeField] private float tickInterval = 0.8f;

    [Header("Merge Colors")]
    [SerializeField] private Color[] mergeColors = new Color[]
    {
        new Color(0.9f, 0.4f, 0.3f, 1f),   // 赤系（初期）
        new Color(0.3f, 0.5f, 0.9f, 1f),   // 青系
        new Color(0.3f, 0.85f, 0.4f, 1f),  // 緑系
        new Color(0.9f, 0.8f, 0.2f, 1f),   // 黄系
        new Color(0.7f, 0.3f, 0.9f, 1f),   // 紫系
        new Color(1f, 0.6f, 0.2f, 1f),     // オレンジ系
        new Color(0.2f, 0.9f, 0.9f, 1f),   // シアン系
        new Color(0.95f, 0.95f, 0.95f, 1f), // 白
    };

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

    /// <summary>
    /// アイテムを生成する
    /// </summary>
    public Item CreateItem(Vector2Int gridPos, string text, Color color)
    {
        GameObject itemObj = new GameObject("Item_" + text);
        Item item = itemObj.AddComponent<Item>();
        item.Initialize(gridPos, text, color);

        allItems.Add(item);
        return item;
    }

    /// <summary>
    /// ティック処理：全コンベア上のアイテムを次のマスへ移動
    /// </summary>
    private void ProcessTick()
    {
        isTicking = true;

        // 破棄済みアイテムをクリーンアップ
        allItems.RemoveAll(item => item == null);

        // 移動先の計算
        Dictionary<Vector2Int, List<Item>> moveTargets = new Dictionary<Vector2Int, List<Item>>();

        foreach (Item item in allItems)
        {
            if (item == null || item.IsMoving || item.IsMarkedForMerge) continue;

            GridCell currentCell = GridManager.Instance.GetCell(item.CurrentGridPos);
            if (currentCell == null || !currentCell.HasConveyor) continue;

            ConveyorBelt conveyor = currentCell.Conveyor;
            Vector2Int nextPos = conveyor.GetNextGridPosition();

            // 範囲外チェック
            if (!GridManager.Instance.IsInBounds(nextPos)) continue;

            GridCell nextCell = GridManager.Instance.GetCell(nextPos);
            if (nextCell == null) continue;

            // 移動先にまだ動いていないアイテムがいたら待機
            if (nextCell.HasItem && !nextCell.CurrentItem.IsMoving)
            {
                // 移動先のアイテムもコンベアで動く予定かチェック
                if (nextCell.HasConveyor)
                {
                    // 移動先のアイテムが同ティックで移動するなら許可
                    // → moveTargetsに追加して後で判定
                }
                else
                {
                    continue; // コンベアなしの場所で動けない
                }
            }

            // 移動先ターゲットに追加
            if (!moveTargets.ContainsKey(nextPos))
                moveTargets[nextPos] = new List<Item>();
            moveTargets[nextPos].Add(item);
        }

        // 移動実行
        foreach (KeyValuePair<Vector2Int, List<Item>> kvp in moveTargets)
        {
            Vector2Int targetPos = kvp.Key;
            List<Item> items = kvp.Value;

            if (items.Count == 1)
            {
                // 単体移動
                GridCell targetCell = GridManager.Instance.GetCell(targetPos);
                if (targetCell != null && !targetCell.HasItem)
                {
                    items[0].MoveTo(targetPos);
                }
            }
            else if (items.Count >= 2)
            {
                // 複数アイテムが同じマスへ → 合流処理
                StartCoroutine(MergeSequence(targetPos, items));
            }
        }

        isTicking = false;
    }

    /// <summary>
    /// 合流シーケンス：複数アイテムを一点に集め、合成する
    /// </summary>
    private IEnumerator MergeSequence(Vector2Int mergePos, List<Item> items)
    {
        // 全アイテムを合成予約
        foreach (Item item in items)
        {
            item.IsMarkedForMerge = true;
            item.MoveTo(mergePos);
        }

        // 全アイテムが移動完了するまで待つ
        bool allDone = false;
        while (!allDone)
        {
            allDone = true;
            foreach (Item item in items)
            {
                if (item != null && item.IsMoving)
                {
                    allDone = false;
                    break;
                }
            }
            yield return null;
        }

        // 合成実行
        PerformMerge(mergePos, items);
    }

    /// <summary>
    /// 合成処理：複数アイテムから新しいアイテムを生成
    /// </summary>
    private void PerformMerge(Vector2Int pos, List<Item> items)
    {
        if (items.Count < 2) return;

        // 合成情報の計算
        int totalMergeCount = 0;
        string mergedText = "";
        foreach (Item item in items)
        {
            if (item == null) continue;
            if (item.Data != null)
            {
                totalMergeCount += item.Data.MergeCount + 1;
                if (mergedText.Length > 0) mergedText += "+";
                mergedText += item.Data.ItemText;
            }
        }

        // 新しい色の決定
        int colorIndex = Mathf.Min(totalMergeCount, mergeColors.Length - 1);
        Color newColor = mergeColors[colorIndex];

        // 元のアイテムを削除
        foreach (Item item in items)
        {
            if (item != null)
            {
                allItems.Remove(item);
                item.DestroyItem();
            }
        }

        // 新しいアイテムを生成
        Item newItem = CreateItem(pos, mergedText, newColor);
        newItem.Data.MergeCount = totalMergeCount;

        Debug.Log("Merged at (" + pos.x + "," + pos.y + "): " + mergedText + " (count: " + totalMergeCount + ")");
    }

    /// <summary>
    /// セルでの合成チェック（移動完了時に呼ばれる）
    /// </summary>
    public void CheckMergeAtCell(Vector2Int pos)
    {
        // 現在は ProcessTick 内で合流を処理するので、
        // 追加の合流チェックが必要な場合にここに実装
    }

    /// <summary>
    /// 合成色の取得
    /// </summary>
    public Color GetMergeColor(int mergeCount)
    {
        int index = Mathf.Min(mergeCount, mergeColors.Length - 1);
        return mergeColors[index];
    }
}
