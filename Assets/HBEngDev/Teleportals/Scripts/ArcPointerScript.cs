using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teleportals {

public class ArcPointerScript : MonoBehaviour
{
    public Vector3 startingPoint;
    public Vector3 endingPoint;
    public int segments;
    public float height;
    public LayerMask layerMask;

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segments + 1;
    }

    void Update()
    {
        DrawArc();
        RaycastHit hit;
        if(Physics.Raycast(endingPoint, Vector3.down, out hit, Mathf.Infinity, layerMask))
        {
            endingPoint = hit.point;
        }
    }

    void DrawArc()
    {
        lineRenderer.SetPositions(CalculateArcArray());
    }

    Vector3[] CalculateArcArray()
    {
        Vector3[] arcArray = new Vector3[segments + 1];
        float radian = Mathf.Deg2Rad * (360f / segments);
        for (int i = 0; i <= segments; i++)
        {
            float angle = radian * i;
            float x = Mathf.Sin(angle) * (endingPoint.x - startingPoint.x);
            float y = height * Mathf.Sin(angle) + (endingPoint.y - startingPoint.y);
            float z = Mathf.Cos(angle) * (endingPoint.z - startingPoint.z);
            arcArray[i] = new Vector3(x, y, z);
        }
        return arcArray;
    }
}
}