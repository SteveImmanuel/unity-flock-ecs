using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;

public class FlockManagerECS : MonoBehaviour
{
    [System.Serializable]
    public struct BoidType
    {
        public float maxSpeed;
        public float neighborRadius;
        public float alignmentWeight;
        public float separationWeight;
        public float targetWeight;
        public float minPredatorDistance;
        public GameObject boidPrefab;
        public int total;
        public Vector3 centerSpawnPoint;
        public float spawnRadius;
    }

    public BoidType[] boidTypes;

    public static FlockManagerECS instance;

    private EntityManager manager;
    private BlobAssetStore blobAssetStore;
    private GameObjectConversionSettings settings;

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
        settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
    }

    private void Start()
    {
        for (int i = 0; i < boidTypes.Length; i++)
        {
            Spawn(boidTypes[i]);
        }
    }

    private void Spawn(BoidType boidType)
    {
        Entity boidEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(boidType.boidPrefab, settings);
        NativeArray<Entity> entities = new NativeArray<Entity>(boidType.total, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        manager.Instantiate(boidEntity, entities);

        BoidData sharedBoidData = new BoidData
        {
            alignmentWeight = boidType.alignmentWeight,
            separationWeight = boidType.separationWeight,
            maxSpeed = boidType.maxSpeed,
            neighborRadius = boidType.neighborRadius,
            targetWeight = boidType.targetWeight,
            minPredatorDistance = boidType.minPredatorDistance
        };

        for (int i = 0; i < entities.Length; i++)
        {
            Vector3 randomNormalizedVector = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            Vector3 randomPos = boidType.centerSpawnPoint + randomNormalizedVector * Random.Range(0f, boidType.spawnRadius);
            Vector3 heading = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

            manager.SetComponentData(entities[i], new Translation { Value = randomPos });
            manager.AddComponentData(entities[i], new HeadingData { Value = heading });
            manager.AddSharedComponentData(entities[i], sharedBoidData);
        }

        entities.Dispose();
    }

    private void OnDestroy()
    {
        blobAssetStore.Dispose();
    }


}
