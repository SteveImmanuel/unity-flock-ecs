using Unity.Entities;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using System.Collections.Generic;

[AlwaysSynchronizeSystem]
public class FlockBehaviourSystem : SystemBase
{
    private EntityQuery queries;
    private List<BoidData> boidTypes;

    private struct BoidInfo
    {
        public int entityIndex;
        public float3 position;
        public float3 velocity;
    }

    private struct CalculateFlockVectorJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<uint> uniqueKeys;
        [ReadOnly] public NativeMultiHashMap<uint, BoidInfo> multiHashMap;
        [NativeDisableContainerSafetyRestriction] public NativeArray<float3> alignmentArray;
        [NativeDisableContainerSafetyRestriction] public NativeArray<float3> separationArray;

        public void Execute(int index)
        {
            var allBoidInfo = multiHashMap.GetValuesForKey(uniqueKeys[index]);
            int totalBoid = 0;
            float3 alignment = float3.zero;
            float3 separation = float3.zero;

            foreach (BoidInfo boidInfo in allBoidInfo)
            {
                totalBoid++;
                alignment += boidInfo.velocity;
                separation -= boidInfo.position;
            }

            foreach (BoidInfo boidInfo in allBoidInfo)
            {
                float3 localAlignment = alignment - boidInfo.velocity;
                float3 localSeparation = separation + boidInfo.position;

                localAlignment /= (totalBoid - 1);
                localSeparation = boidInfo.position - (localSeparation / (totalBoid - 1));
                alignmentArray[boidInfo.entityIndex] = localAlignment;
                separationArray[boidInfo.entityIndex] = localSeparation;
            }

        }
    }

    protected override void OnCreate()
    {
        queries = GetEntityQuery(typeof(Translation), typeof(HeadingData), typeof(BoidData));
        boidTypes = new List<BoidData>();
    }


    protected override void OnUpdate()
    {
        Random random = new Random(13517039);
        float deltaTime = Time.DeltaTime;
        EntityManager.GetAllUniqueSharedComponentData(boidTypes);

        for (int typeIndex = 0; typeIndex < boidTypes.Count; typeIndex++)
        {
            BoidData boidType = boidTypes[typeIndex];
            queries.AddSharedComponentFilter(boidType);
            int totalBoid = queries.CalculateEntityCount();

            if (totalBoid == 0)
            {
                queries.ResetFilter();
                continue;
            }

            NativeMultiHashMap<uint, BoidInfo> multiHashMap = new NativeMultiHashMap<uint, BoidInfo>(totalBoid, Allocator.TempJob);
            NativeArray<float3> alignment = new NativeArray<float3>(totalBoid, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<float3> separation = new NativeArray<float3>(totalBoid, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<uint> hashBoidPos = new NativeArray<uint>(totalBoid, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            JobHandle populateHashHandle = Entities
                .WithSharedComponentFilter(boidType)
                .WithName("PopulateHashPositionValue")
                .ForEach((int entityInQueryIndex, in Translation pos) =>
                {
                    hashBoidPos[entityInQueryIndex] = math.hash(new int3(math.floor(pos.Value / boidType.neighborRadius)));
                })
                .ScheduleParallel(Dependency);

            var parallelHashMap = multiHashMap.AsParallelWriter();
            JobHandle populateMultiHashMapHandle = Entities
                .WithSharedComponentFilter(boidType)
                .WithName("PopulateMultiHashMapInfo")
                .ForEach((int entityInQueryIndex, in HeadingData heading, in Translation pos) =>
                {
                    parallelHashMap.Add(hashBoidPos[entityInQueryIndex], new BoidInfo { entityIndex = entityInQueryIndex, position = pos.Value, velocity = heading.Value });
                })
                .ScheduleParallel(populateHashHandle);

            populateMultiHashMapHandle.Complete();
            var multiHashMapKey = multiHashMap.GetUniqueKeyArray(Allocator.TempJob);

            CalculateFlockVectorJob calculateFlockVector = new CalculateFlockVectorJob
            {
                alignmentArray = alignment,
                separationArray = separation,
                multiHashMap = multiHashMap,
                uniqueKeys = multiHashMapKey.Item1
            };

            JobHandle calculateFlockVectorHandle = calculateFlockVector.Schedule(multiHashMapKey.Item2, 10, populateMultiHashMapHandle);
            JobHandle steerBoidHandle = Entities
                .WithSharedComponentFilter(boidType)
                .WithName("SteerBoid")
                .ForEach((int entityInQueryIndex, ref HeadingData heading, ref Translation pos, ref Rotation rot) =>
                {
                    heading.Value += boidType.alignmentWeight * alignment[entityInQueryIndex] + boidType.separationWeight * separation[entityInQueryIndex];
                    heading.Value = math.normalizesafe(heading.Value);

                    rot.Value = quaternion.LookRotationSafe(heading.Value, math.up());
                    pos.Value += heading.Value * (boidType.maxSpeed + random.NextFloat(-0.1f, 0.1f)) * deltaTime;
                })
                .WithReadOnly(alignment)
                .WithReadOnly(separation)
                .ScheduleParallel(calculateFlockVectorHandle);

            Dependency = steerBoidHandle;
            JobHandle disposeJobHandle = multiHashMap.Dispose(Dependency);
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, multiHashMapKey.Item1.Dispose(Dependency));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, alignment.Dispose(Dependency));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, separation.Dispose(Dependency));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, hashBoidPos.Dispose(Dependency));
            Dependency = disposeJobHandle;

            queries.ResetFilter();
        }
        boidTypes.Clear();
    }
}
