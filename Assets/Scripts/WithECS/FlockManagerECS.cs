using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

public class FlockManagerECS : MonoBehaviour
{
    [Header("Flock Config")]
    public GameObject boidPrefab;
    public float maxSpeed = 5f;
    public float maxNeighbor;
    public float neighborRadius = 2f;
    public float alignmentWeight = 1;
    public float cohesionWeight = 1;
    public float separationWeight = 2;
    public float spawnRadius = 10f;
    public int totalBoids = 100;

    public static FlockManagerECS instance;

    private EntityManager manager;
    private Entity boidEntity;
    private BlobAssetStore blobAssetStore;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        blobAssetStore = new BlobAssetStore();
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        boidEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(boidPrefab, settings);
    }

    private void Start()
    {
        Spawn(totalBoids);
    }

    private void Spawn(int n)
    {
        NativeArray<Entity> entities = new NativeArray<Entity>(n, Allocator.TempJob);
        manager.Instantiate(boidEntity, entities);

        for (int i = 0; i < n; i++)
        {
            Vector3 randomNormalizedVector = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            Vector3 randomPos = transform.position + randomNormalizedVector * Random.Range(0f, spawnRadius);
            Quaternion randomRot = Quaternion.LookRotation(randomNormalizedVector);

            manager.SetComponentData(entities[i], new Translation { Value = randomPos });
            manager.SetComponentData(entities[i], new Rotation { Value = randomRot });
        }

        entities.Dispose();
    }

    private void OnDestroy()
    {
        blobAssetStore.Dispose();
    }


}
