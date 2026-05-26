using System.Collections;
using UnityEngine;

public class TrashStation : BaseStation
{
    public override void SetOccupancy(Vector2Int pivot, Vector2Int[] shapeOffsets, Draggable3DItem occupantItem)
    {
        // If it's being cleared (null), process normally
        if (occupantItem == null)
        {
            base.SetOccupancy(pivot, shapeOffsets, null);
            return;
        }

        StartCoroutine(VaporizeItemRoutine(pivot, shapeOffsets));
    }

    private IEnumerator VaporizeItemRoutine(Vector2Int pivot, Vector2Int[] shapeOffsets)
    {
        yield return null;

        Vector3 tilePosition = tileVisuals[pivot.x, pivot.y].transform.position;
        Collider[] detectedColliders = Physics.OverlapSphere(tilePosition + new Vector3(0, 0.1f, 0), 0.4f);

        foreach (Collider col in detectedColliders)
        {
            Draggable3DItem item = col.GetComponent<Draggable3DItem>();
            if (item != null)
            {
                Destroy(item.gameObject);
                break;
            }
        }

        base.SetOccupancy(pivot, shapeOffsets, null);
    }
}