using UnityEngine;

public class EnvironmentSpawner : MonoBehaviour
{
    [Header("Buildings")]
    public GameObject[] buildingPrefabs;

    [Range(0, 100)]
    public int buildingSpawnChance = 70;

    [Header("Parked Cars")]
    public GameObject[] parkedCarPrefabs;

    [Range(0, 100)]
    public int parkedCarSpawnChance = 40;

    private void Start()
    {
        SpawnBuildings();
        SpawnParkedCars();
    }

    private void SpawnBuildings()
    {
        Transform city = transform.Find("City");

        if (city == null)
            return;

        Transform buildingParent = city.Find("BuildingSpawnPoints");

        if (buildingParent == null)
            return;

        foreach (Transform spawnPoint in buildingParent)
        {
            if (Random.Range(0, 100) >= buildingSpawnChance)
                continue;

            GameObject randomBuilding =
                buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];

            Instantiate(randomBuilding,
                        spawnPoint.position,
                        spawnPoint.rotation,
                        city);
        }
    }

    private void SpawnParkedCars()
    {
        Transform city = transform.Find("City");

        if (city == null)
            return;

        foreach (Transform child in city)
        {
            // Sadece Park objeleri
            if (!child.name.StartsWith("Park"))
                continue;

            Transform spawnParent = child.Find("CarSpawnPoints");

            if (spawnParent == null)
                continue;

            foreach (Transform spawnPoint in spawnParent)
            {
                if (Random.Range(0, 100) >= parkedCarSpawnChance)
                    continue;

                GameObject randomCar =
                    parkedCarPrefabs[Random.Range(0, parkedCarPrefabs.Length)];

                // Aracı oluştur
                GameObject parkedCar = Instantiate(
                    randomCar,
                    spawnPoint.position,
                    spawnPoint.rotation,
                    child);

                // Park halindeki araç engel olarak davranacak
                parkedCar.tag = "Obstacle";
            }
        }
    }
}