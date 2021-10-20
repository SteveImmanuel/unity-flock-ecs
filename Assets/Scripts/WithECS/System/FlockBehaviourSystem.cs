using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

[AlwaysSynchronizeSystem]
public class FlockBehaviourSystem : SystemBase
{
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



    protected override void OnUpdate()
    {
        EntityQuery queries = GetEntityQuery(typeof(Translation), typeof(BoidTagData), typeof(SpeedData));
        int totalBoid = queries.CalculateEntityCount();

        float neighborRadius = FlockManagerECS.instance.neighborRadius;
        float maxNeighbor = FlockManagerECS.instance.maxNeighbor;
        float3 flockWeights = new float3(FlockManagerECS.instance.alignmentWeight, FlockManagerECS.instance.cohesionWeight, FlockManagerECS.instance.separationWeight);

        NativeMultiHashMap<uint, BoidInfo> multiHashMap = new NativeMultiHashMap<uint, BoidInfo>(totalBoid, Allocator.TempJob);
        NativeArray<float3> alignment = new NativeArray<float3>(totalBoid, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<float3> separation = new NativeArray<float3>(totalBoid, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<uint> hashBoidPos = new NativeArray<uint>(totalBoid, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        JobHandle populateHashHandle = Entities
            .WithAll<BoidTagData>()
            .WithName("PopulateHashPositionValue")
            .ForEach((int entityInQueryIndex, in Translation pos) =>
            {
                hashBoidPos[entityInQueryIndex] = math.hash(new int3(math.floor(pos.Value / neighborRadius)));
            })
            .ScheduleParallel(Dependency);

        var parallelHashMap = multiHashMap.AsParallelWriter();
        JobHandle populateMultiHashMapHandle = Entities
            .WithAll<BoidTagData>()
            .WithName("PopulateMultiHashMapInfo")
            .ForEach((int entityInQueryIndex, in SpeedData speedData, in Translation pos) =>
            {
                parallelHashMap.Add(hashBoidPos[entityInQueryIndex], new BoidInfo { entityIndex = entityInQueryIndex, position = pos.Value, velocity = speedData.velocity });
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

        JobHandle updateVelocityHandle = Entities
            .WithAll<BoidTagData>()
            .WithName("UpdateVelocity")
            .ForEach((int entityInQueryIndex, ref SpeedData speedData) =>
            {
                float3 flockVector = flockWeights.x * alignment[entityInQueryIndex] + flockWeights.z * separation[entityInQueryIndex];
                speedData.velocity += flockVector;

                if (math.lengthsq(speedData.velocity) > speedData.maximum * speedData.maximum)
                {
                    speedData.velocity = math.normalizesafe(speedData.velocity, float3.zero) * speedData.maximum;
                }
            })
            .WithReadOnly(alignment)
            .WithReadOnly(separation)
            .ScheduleParallel(calculateFlockVectorHandle);

        Dependency = updateVelocityHandle;
        JobHandle disposeJobHandle = multiHashMap.Dispose(Dependency);
        disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, multiHashMapKey.Item1.Dispose(Dependency));
        disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, alignment.Dispose(Dependency));
        disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, separation.Dispose(Dependency));
        disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, hashBoidPos.Dispose(Dependency));
        Dependency = disposeJobHandle;
    }
}
