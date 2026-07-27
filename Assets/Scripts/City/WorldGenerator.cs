using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [Header("References")]
    public GameObject groundTilePrefab;
    public Transform player;

    [Header("Settings")]
    public int tileSize = 25;
    public int renderDistance = 1;

    private Dictionary<Vector2Int, GameObject> spawnedTiles = new();

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
        int playerTileX = Mathf.FloorToInt(player.position.x / tileSize);
        int playerTileZ = Mathf.FloorToInt(player.position.z / tileSize);

        // Oyuncunun etrafındaki tile'ları oluştur
        for (int x = playerTileX - renderDistance; x <= playerTileX + renderDistance; x++)
        {
            for (int z = playerTileZ - renderDistance; z <= playerTileZ + renderDistance; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);

                if (!spawnedTiles.ContainsKey(coord))
                {
                    Vector3 pos = new Vector3(x * tileSize, 0, z * tileSize);

                    GameObject tile = Instantiate(
                        groundTilePrefab,
                        pos,
                        Quaternion.identity);

                    spawnedTiles.Add(coord, tile);
                }
            }
        }

        // Oyuncudan uzak tile'ları sil
        List<Vector2Int> tilesToRemove = new List<Vector2Int>();

        foreach (var tile in spawnedTiles)
        {
            int distanceX = Mathf.Abs(tile.Key.x - playerTileX);
            int distanceZ = Mathf.Abs(tile.Key.y - playerTileZ);

            if (distanceX > renderDistance || distanceZ > renderDistance)
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