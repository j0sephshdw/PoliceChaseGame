using UnityEngine;

public class RoadSideGenerator : MonoBehaviour
{
    [Header("Road Side Prefabı")]
    public GameObject roadSidePrefab;

    [Header("Plane")]
    public Transform plane;

    [Header("Grid Ayarları")]
    public int rows = 10;
    public int columns = 10;

    [Header("Spawn Noktaları Arası Mesafe")]
    public float spacing = 10f;

    void Start()
    {
        GenerateRoadSides();
    }

    void GenerateRoadSides()
    {
        float planeWidth = plane.localScale.x * 10f;
        float planeLength = plane.localScale.z * 10f;

        Vector3 startPos = plane.position -
                           new Vector3(planeWidth / 2f, 0, planeLength / 2f);

        // -------------------------
        // YATAY ROAD SIDE
        // -------------------------
        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < columns - 1; x++)
            {
                Vector3 pos = startPos + new Vector3(
                    (x + 1f) * (planeWidth / columns),
                    0,
                    (z + 0.5f) * (planeLength / rows)
                );

                Instantiate(
                    roadSidePrefab,
                    pos,
                    Quaternion.identity,
                    transform
                );
            }
        }

        // -------------------------
        // DİKEY ROAD SIDE
        // -------------------------
        for (int z = 0; z < rows - 1; z++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 pos = startPos + new Vector3(
                    (x + 0.5f) * (planeWidth / columns),
                    0,
                    (z + 1f) * (planeLength / rows)
                );

                Instantiate(
                    roadSidePrefab,
                    pos,
                    Quaternion.Euler(0, 90, 0),
                    transform
                );
            }
        }
    }
}