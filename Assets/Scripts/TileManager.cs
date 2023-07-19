using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    private static TileManager _instance;

    public GameObject startTile;
    public TileObject[] tiles;
    public float frontOffsetFromPlayer = 20f;
    public float backwardOffsetFromPlayer = 20f;
    private List<GameObject> spawnedTiles = new List<GameObject>();
    private GameObject player;
    // Start is called before the first frame update

    public GameObject[] obstacleList;

    public static TileManager getInstance(){
        return _instance ? _instance : null;
    }

    void Awake(){
        _instance = this;
    }

    void Start()
    {
        player = Camera.main.gameObject;
    }

    void FixedUpdate()
    {
        CreateTile();
        DeleteTile();
    }

    public void CreateTile()
    {
        if (spawnedTiles.Count > 0 && spawnedTiles[spawnedTiles.Count - 1].transform.position.z - frontOffsetFromPlayer > player.transform.position.z)
            return;
        var state = GameManager.getInstance().GetState();
        var randomPercent = Random.Range(0, 100);
        var tileObj = tiles.FirstOrDefault(i => i.minChance <= randomPercent && i.maxChance > randomPercent);
        var go = Instantiate(
            state == GameState.PLAYING && tileObj != null ? tileObj.tile : startTile,
            spawnedTiles.Count > 0
                ? spawnedTiles[spawnedTiles.Count - 1].transform.Find("EndPoint").position
                : Vector3.zero,
            Quaternion.identity
        );
        spawnedTiles.Add(go);

        var obstaclePoint = go.transform.Find("ObstacleSpawnPoint");
        if (obstaclePoint)
        {
            var obstacleObj = Instantiate(obstacleList[Random.Range(0, obstacleList.Length)], obstaclePoint.position, Quaternion.identity);
            obstacleObj.transform.SetParent(go.transform);
        }

    }

    void DeleteTile(){
        if (spawnedTiles.Count > 0 && spawnedTiles[0].transform.Find("EndPoint").position.z + backwardOffsetFromPlayer > player.transform.position.z)
            return;
        Destroy(spawnedTiles[0]);
        spawnedTiles.RemoveAt(0);
    }

    public void DeleteAllSpawned(){
        var currentSpawnedLength = spawnedTiles.Count;
        for (int i = 0; i < currentSpawnedLength; i++)
        {
            Destroy(spawnedTiles[0]);
            spawnedTiles.RemoveAt(0);
        }
    }
    public void ChangeStateAllSpawned(bool isEnabled){
        var currentSpawnedLength = spawnedTiles.Count;
        for (int i = 0; i < currentSpawnedLength; i++)
        {
            spawnedTiles[i].SetActive(isEnabled);
        }
    }
}

[System.Serializable]
public class TileObject {
    [Range(0, 100)]
    public int minChance = 0;
    [Range(0, 100)]
    public int maxChance = 0;
    public GameObject tile;
}
