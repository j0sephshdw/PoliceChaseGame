using UnityEngine;

public class BuildingSpawner : MonoBehaviour
{
    [Header("Spawn Noktalarının Bulunduğu Parent")]
    public Transform spawnPointParent;

    [Header("Bina Prefabları")]
    public GameObject[] buildingPrefabs;

    [Header("Bina Oluşturma Olasılığı")]
    [Range(0, 100)]
    public int spawnChance = 70;

    private int lastIndex = -1;

    // Inspector -> Sağ Tık -> Generate Buildings
    [ContextMenu("Generate Buildings")]
    public void GenerateBuildings()
    {
        ClearBuildings();

        if (spawnPointParent == null)
        {
            Debug.LogError("Spawn Point Parent atanmadı!");
            return;
        }

        if (buildingPrefabs == null || buildingPrefabs.Length == 0)
        {
            Debug.LogError("Building Prefabs boş!");
            return;
        }

        foreach (Transform spawnPoint in spawnPointParent)
        {
            if (Random.Range(0, 100) >= spawnChance)
                continue;

            int randomIndex;

            do
            {
                randomIndex = Random.Range(0, buildingPrefabs.Length);
            }
            while (randomIndex == lastIndex && buildingPrefabs.Length > 1);

            lastIndex = randomIndex;

            Instantiate(
                buildingPrefabs[randomIndex],
                spawnPoint.position,
                spawnPoint.rotation,
                transform
            );
        }

        Debug.Log("Binalar oluşturuldu.");
    }

    // Inspector -> Sağ Tık -> Clear Buildings
    [ContextMenu("Clear Buildings")]
    public void ClearBuildings()
    {
        while (transform.childCount > 0)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(0).gameObject);
#else
            Destroy(transform.GetChild(0).gameObject);
#endif
        }

        Debug.Log("Binalar temizlendi.");
    }
}