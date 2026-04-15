using UnityEngine;

/// <summary>
/// Base class for placeable factory facilities.
/// </summary>
public abstract class FacilityNode : MonoBehaviour
{
    protected GridCell ownerCell;
    protected ConveyorDirection outputDirection;

    public GridCell OwnerCell => ownerCell;
    public ConveyorDirection OutputDirection => outputDirection;

    public virtual void Initialize(GridCell cell, ConveyorDirection direction)
    {
        ownerCell = cell;
        outputDirection = direction;
    }

    public virtual void RotateOutput()
    {
        outputDirection = outputDirection.RotateCW();
    }

    public abstract FacilityType GetFacilityType();

    /// <summary>
    /// Called when an item reaches this cell.
    /// Return true if the facility consumed/processed the item.
    /// </summary>
    public abstract bool OnItemArrived(Item item);
}
