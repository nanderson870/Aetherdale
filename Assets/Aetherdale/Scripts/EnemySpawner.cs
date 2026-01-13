using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;
using UnityEngine.SceneManagement;


public class EnemySpawner : NetworkBehaviour
{
    public Entity enemy;

    public int numEnemies = 10;
    public int respawnTimeSeconds = 10;
    public int radius = 30;

    int currentlySpawnedEnemies = 0;
    Queue<float> respawnTimers = new Queue<float>();

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Handles.color = Color.green;
        Handles.DrawWireDisc(transform.position, Vector3.up, radius);
    }
#endif

    [ServerCallback]
    void Start()
    {
        for(int i = 0; i < numEnemies; i++)
        {
            SpawnEnemy();
        }
    }

    // Update is called once per frame
    [ServerCallback]
    void Update()
    {
        if (respawnTimers.Count > 0)
        {
            while (respawnTimers.Count > 0 && respawnTimers.Peek() <= Time.time)
            {
                respawnTimers.Dequeue();
                SpawnEnemy();
            }
        }
    }

    [ServerCallback]
    void SpawnEnemy()
    {
        Vector2 randomCircularOffset = Random.insideUnitCircle * radius;
        Vector3 newEnemyOffset = new Vector3(randomCircularOffset.x, 0.0F, randomCircularOffset.y);

        Entity spawnedEnemy = Instantiate(enemy, transform.position + newEnemyOffset, Quaternion.Euler(0.0F, Random.Range(0.0F, 360.0F), 0));
        spawnedEnemy.OnDeath += OnSpawnedEnemyDeath;

        SceneManager.MoveGameObjectToScene(spawnedEnemy.gameObject, gameObject.scene);

        NetworkServer.Spawn(spawnedEnemy.gameObject);
        currentlySpawnedEnemies++;
    }

    [ServerCallback]
    public void OnSpawnedEnemyDeath(Entity entity, Entity killer)
    {
        currentlySpawnedEnemies--;
        respawnTimers.Enqueue(Time.time + respawnTimeSeconds);
    }
}
