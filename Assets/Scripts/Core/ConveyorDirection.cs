using UnityEngine;

/// <summary>
/// コンベアの方向を表すenum
/// </summary>
public enum ConveyorDirection
{
    Up = 0,
    Right = 1,
    Down = 2,
    Left = 3
}

/// <summary>
/// ConveyorDirectionに関するユーティリティ拡張メソッド
/// </summary>
public static class ConveyorDirectionExtensions
{
    /// <summary>
    /// 方向をVector2Intに変換
    /// </summary>
    public static Vector2Int ToVector2Int(this ConveyorDirection dir)
    {
        switch (dir)
        {
            case ConveyorDirection.Up:    return Vector2Int.up;
            case ConveyorDirection.Right: return Vector2Int.right;
            case ConveyorDirection.Down:  return Vector2Int.down;
            case ConveyorDirection.Left:  return Vector2Int.left;
            default: return Vector2Int.zero;
        }
    }

    /// <summary>
    /// 時計回りに90度回転
    /// </summary>
    public static ConveyorDirection RotateCW(this ConveyorDirection dir)
    {
        return (ConveyorDirection)(((int)dir + 1) % 4);
    }

    /// <summary>
    /// 反時計回りに90度回転
    /// </summary>
    public static ConveyorDirection RotateCCW(this ConveyorDirection dir)
    {
        return (ConveyorDirection)(((int)dir + 3) % 4);
    }

    /// <summary>
    /// Z軸回転角度を取得（Upが0度、時計回りで減少）
    /// </summary>
    public static float ToZRotation(this ConveyorDirection dir)
    {
        switch (dir)
        {
            case ConveyorDirection.Up:    return 0f;
            case ConveyorDirection.Right: return -90f;
            case ConveyorDirection.Down:  return 180f;
            case ConveyorDirection.Left:  return 90f;
            default: return 0f;
        }
    }

    /// <summary>
    /// 反対方向を取得
    /// </summary>
    public static ConveyorDirection Opposite(this ConveyorDirection dir)
    {
        return (ConveyorDirection)(((int)dir + 2) % 4);
    }
}
