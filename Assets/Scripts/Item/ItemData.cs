using UnityEngine;

/// <summary>
/// Runtime data for grid items (text, color, merge depth, clog state).
/// </summary>
public class ItemData : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private string itemText = "";
    [SerializeField] private Color itemColor = Color.white;
    [SerializeField] private int mergeCount = 0;
    [SerializeField] private bool isClog = false;

    public string ItemText
    {
        get { return itemText; }
        set { itemText = value; }
    }

    public Color ItemColor
    {
        get { return itemColor; }
        set { itemColor = value; }
    }

    public int MergeCount
    {
        get { return mergeCount; }
        set { mergeCount = value; }
    }

    public bool IsClog
    {
        get { return isClog; }
        set { isClog = value; }
    }

    public void Setup(string text, Color color, bool clog = false)
    {
        itemText = text;
        itemColor = color;
        mergeCount = 0;
        isClog = clog;
    }
}
