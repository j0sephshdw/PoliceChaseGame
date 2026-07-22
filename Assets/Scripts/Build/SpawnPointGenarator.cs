using UnityEngine;

public class SpawnPointGenerator : MonoBehaviour
{
    [Header("Spawn Point Prefabı")]
    public GameObject spawnPointPrefab;

    [Header("Plane")]
    public Transform plane;

    [Header("Grid Ayarları")]
    public int rows = 10;
    public int columns = 10;

    [Header("İki Nokta Arasındaki Mesafe")]
    public float spacing = 5f;

    // Inspector > Sağ Tık > Generate Spawn Points
    [ContextMenu("Generate Spawn Points")]
    public void GenerateSpawnPoints()
    {
        ClearSpawnPoints();

        if (spawnPointPrefab == null)
        {
            Debug.LogError("Spawn Point Prefabı atanmadı!");
            return;
        }

        if (plane == null)
        {
            Debug.LogError("Plane atanmadı!");
            return;
        }

        Vector3 center = plane.position;

        // Grid'i plane'in ortasına hizala
        float startX = center.x - ((columns - 1) * spacing) / 2f;
        float startZ = center.z - ((rows - 1) * spacing) / 2f;

        int count = 0;

        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 position = new Vector3(
                    startX + x * spacing,
                    plane.position.y,
                    startZ + z * spacing
                );

                Instantiate(
                    spawnPointPrefab,
                    position,
                    Quaternion.identity,
                    transform
                );

                count++;
            }
        }

        Debug.Log("Oluşturulan Spawn Point Sayısı: " + count);
    }

    // Inspector > Sağ Tık > Clear Spawn Points
    [ContextMenu("Clear Spawn Points")]
    public void ClearSpawnPoints()
    {
        while (transform.childCount > 0)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(0).gameObject);
#else
            Destroy(transform.GetChild(0).gameObject);
#endif
        }

        Debug.Log("Spawn Point'ler temizlendi.");
    }
}