using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [Header("References")]
    public GameObject groundTilePrefab;
    public Transform player;

    [Header("Settings")]
    public int tileSize = 25;

    // Aktif alan (4x4)
    public int renderSize = 4;

    // Silinme mesafesi
    public int unloadDistance = 6;

    private Dictionary<Vector2Int, GameObject> spawnedTiles = new();

    [SerializeField] private Transform tilesParent; // Tüm GroundTile'ların toplanacağı klasör obje

    private void Start()
    {
        UpdateWorld();
    }

    private void Update()
    {
        UpdateWorld();
    }

    void UpdateWorld()
    {
        // Oyuncu öldüyse/yoksa yol oluşturmayı durdur!
        if (player == null) return;
        int playerTileX = Mathf.FloorToInt(player.position.x / tileSize);
        int playerTileZ = Mathf.FloorToInt(player.position.z / tileSize);


       
        for (int x = playerTileX - renderSize; x <= playerTileX + renderSize; x++)
        {
            for (int z = playerTileZ - renderSize; z <= playerTileZ + renderSize; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);

                if (!spawnedTiles.ContainsKey(coord))
                {
                    Vector3 pos = new Vector3(
                        x * tileSize,
                        0,
                        z * tileSize
                    );

                    GameObject tile = Instantiate(
                        groundTilePrefab,
                        pos,
                        Quaternion.identity,
                        tilesParent
                    );

                    spawnedTiles.Add(coord, tile);
                }
            }
        }


        // Uzak tile silme
        List<Vector2Int> tilesToRemove = new();

        foreach (var tile in spawnedTiles)
        {
            int distanceX = Mathf.Abs(tile.Key.x - playerTileX);
            int distanceZ = Mathf.Abs(tile.Key.y - playerTileZ);

            if (distanceX > unloadDistance || distanceZ > unloadDistance)
            {
                Destroy(tile.Value);
                tilesToRemove.Add(tile.Key);
            }
        }


        foreach (var coord in tilesToRemove)
        {
            spawnedTiles.Remove(coord);
        }
    }
}