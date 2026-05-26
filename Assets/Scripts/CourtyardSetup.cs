// Assets/Scripts/CourtyardSetup.cs
using UnityEngine;

public class CourtyardSetup : MonoBehaviour
{
    // Simple enemy prefab (capsule) created at runtime if none assigned
    public GameObject enemyPrefab;
    // Number of enemies to spawn
    [Range(0, 20)]
    public int enemyCount = 3;

    void Start()
    {
        CreateGround();
        CreateBoundary();
        SpawnEnemies();
    }

    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "CourtyardGround";
        ground.transform.localScale = new Vector3(5, 1, 5); // 10x10 units
        ground.GetComponent<Renderer>().material.color = new Color(0.3f, 0.6f, 0.3f); // grassy green
    }

    void CreateBoundary()
    {
        // Simple fence made of cubes around the plane
        float size = 5f * 10f; // plane scale * 10 (plane default is 10 units)
        float height = 2f;
        Vector3[] positions = new Vector3[] {
            new Vector3(0, height/2, size/2), // north
            new Vector3(0, height/2, -size/2), // south
            new Vector3(size/2, height/2, 0), // east
            new Vector3(-size/2, height/2, 0) // west
        };
        Vector3[] scales = new Vector3[] {
            new Vector3(size, height, 0.1f), // north/south
            new Vector3(size, height, 0.1f), // north/south
            new Vector3(0.1f, height, size), // east/west
            new Vector3(0.1f, height, size)  // east/west
        };
        for (int i = 0; i < 4; i++)
        {
            GameObject fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fence.name = "Fence" + i;
            fence.transform.position = positions[i];
            fence.transform.localScale = scales[i];
            fence.GetComponent<Renderer>().material.color = new Color(0.5f, 0.3f, 0.1f);
        }
    }

    void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            // Create a simple capsule as a placeholder enemy
            enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyPrefab.name = "EnemyPlaceholder";
            enemyPrefab.GetComponent<Renderer>().material.color = Color.red;
            // Add a simple script to simulate enemy behavior (optional)
        }
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
            Instantiate(enemyPrefab, pos, Quaternion.identity);
        }
    }
}
