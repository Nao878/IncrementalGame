using UnityEngine;

/// <summary>
/// ゴミ箱/焼却炉施設。到達したブロックを破棄する。
/// コインは獲得できない。
/// </summary>
public class IncineratorNode : FacilityNode
{
    public override void Initialize(GridCell cell, ConveyorDirection direction)
    {
        base.Initialize(cell, direction);

        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.GetSquareSprite();
        sr.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark gray
        sr.sortingOrder = 1;
        SpriteFactory.ApplyUnlitMaterial(sr);

        GameObject fireObj = new GameObject("FireIcon");
        fireObj.transform.SetParent(transform, false);
        fireObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        var tmp = fireObj.AddComponent<TMPro.TextMeshPro>();
        GameFontSettings.ApplyTo(tmp);
        tmp.text = "✖";
        tmp.color = new Color(0.9f, 0.3f, 0.3f, 1f);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 20;
        tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);
        tmp.sortingOrder = 2;
    }

    public override FacilityType GetFacilityType() => FacilityType.Incinerator;

    public override bool OnItemArrived(Item item)
    {
        // 焼却エフェクトなどを出す
        MergeEffectPlayer.PlayMergeBurst(transform.position);

        ItemManager.Instance.UnregisterAndDestroy(item);
        return true;
    }
}
