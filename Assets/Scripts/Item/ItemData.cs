using UnityEngine;

/// <summary>
/// アイテムが持つデータコンポーネント。
/// 将来の漢字合成システムに拡張するためのString型フィールドを含む。
/// </summary>
public class ItemData : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private string itemText = "";
    [SerializeField] private Color itemColor = Color.white;
    [SerializeField] private int mergeCount = 0;

    /// <summary>文字データ（将来の漢字合成用）</summary>
    public string ItemText
    {
        get { return itemText; }
        set { itemText = value; }
    }

    /// <summary>アイテムの色</summary>
    public Color ItemColor
    {
        get { return itemColor; }
        set { itemColor = value; }
    }

    /// <summary>合成回数</summary>
    public int MergeCount
    {
        get { return mergeCount; }
        set { mergeCount = value; }
    }

    /// <summary>
    /// アイテムデータの初期化
    /// </summary>
    public void Setup(string text, Color color)
    {
        itemText = text;
        itemColor = color;
        mergeCount = 0;
    }
}
