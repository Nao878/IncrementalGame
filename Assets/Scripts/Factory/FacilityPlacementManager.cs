using UnityEngine;

/// <summary>
/// Places and rotates Conveyor/Combiner/Collector nodes on grid.
/// </summary>
public class FacilityPlacementManager : MonoBehaviour
{
    public static FacilityPlacementManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool PlaceFacility(FacilityType type, Vector2Int pos, ConveyorDirection direction)
    {
        GridCell cell = GridManager.Instance.GetCell(pos);
        if (cell == null) return false;
        if (cell.Type == CellType.Generator) return false;
        if (cell.HasFacility || cell.HasConveyor) return false;

        if (type == FacilityType.Conveyor)
        {
            GameObject conveyorObj = new GameObject("Conveyor_" + pos.x + "_" + pos.y);
            conveyorObj.transform.position = GridManager.Instance.GridToWorld(pos);
            conveyorObj.transform.SetParent(cell.transform);
            ConveyorBelt conveyor = conveyorObj.AddComponent<ConveyorBelt>();
            conveyor.Initialize(cell, direction);
            cell.Conveyor = conveyor;
            cell.SetCellType(CellType.Conveyor);
            return true;
        }

        GameObject facilityObj = new GameObject(type + "_" + pos.x + "_" + pos.y);
        facilityObj.transform.position = GridManager.Instance.GridToWorld(pos);
        facilityObj.transform.SetParent(cell.transform);

        FacilityNode node = null;
        if (type == FacilityType.Generator)
        {
            node = facilityObj.AddComponent<ItemGenerator>();
            cell.SetCellType(CellType.Generator);
        }
        else if (type == FacilityType.Combiner)
        {
            node = facilityObj.AddComponent<CombinerNode>();
            // 独自のCellTypeにするか空き地扱いにするか。とりあえずCollector同様の扱いやEmptyで良いが、便宜上そのまま
            // CellTypeに該当するものがなければ変更しなくてよい
        }
        else if (type == FacilityType.Incinerator)
        {
            node = facilityObj.AddComponent<IncineratorNode>();
        }
        else if (type == FacilityType.Collector)
        {
            node = facilityObj.AddComponent<CollectorNode>();
            cell.SetCellType(CellType.Collector);
        }

        if (node == null)
        {
            Destroy(facilityObj);
            return false;
        }

        node.Initialize(cell, direction);
        cell.Facility = node;
        return true;
    }
}
