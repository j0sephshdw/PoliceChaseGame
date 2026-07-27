using UnityEngine;

public class EnvironmentSpawner : MonoBehaviour
{
    [Header("Buildings")]
    public GameObject[] buildingPrefabs;

    [Range(0, 100)]
    public int buildingSpawnChance = 70;

    private void Start()
    {
        SpawnBuildings();
    }

    private void SpawnBuildings()
    {
        Transform city = transform.Find("City");

        if (city == null)
        {
            Debug.LogError("City objesi bulunamadı!");
            return;
        }

        Transform buildingParent = city.Find("BuildingSpawnPoints");

        if (buildingParent == null)
        {
            Debug.LogError("BuildingSpawnPoints bulunamadı!");
            return;
        }

        foreach (Transform spawnPoint in buildingParent)
        {
            if (Random.Range(0, 100) > buildingSpawnChance)
                continue;

            GameObject randomBuilding =  buildingPrefabs[Random.Range(0, buildingPrefabs.Length)]; //buildingPrefabs[11];

            Instantiate(
                randomBuilding,
                spawnPoint.position,
                spawnPoint.rotation,
                city
            );
        }
    }
}