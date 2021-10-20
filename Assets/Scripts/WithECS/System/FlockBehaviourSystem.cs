using Unity.Entities;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using System.Collections.Generic;

public class FlockBehaviourSystem : SystemBase
{
    private EntityQuery boidQueries;
    private EntityQuery targetBoidQueries;
    private EntityQuery predatorBoidQueries;
    private List<BoidData> boidTypes;

    private struct BoidInfo
    {
        public int entityIndex;
        public float3 position;
        public float3 velocity;
    }

    private struct DistanceInfo
    {
        public float distanceSq;
        public float3 vector;
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
                alignmentArray[boidInfo.entityIndex] = math.normalizesafe(localAlignment);
                separationArray[boidInfo.entityIndex] = math.normalizesafe(localSeparation);
            }

        }
    }

    protected override void OnCreate()
    {
        boidQueries = GetEntityQuery(typeof(Translation), typeof(HeadingData), typeof(BoidData));
        targetBoidQueries = GetEntityQuery(typeof(TargetTagData));
        predatorBoidQueries = GetEntityQuery(typeof(PredatorTagData));
        boidTypes = new List<BoidData>();
    }


    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        float INFINITE = 99999999;
        int totalTarget = targetBoidQueries.CalculateEntityCount();
        int totalPredator = predatorBoidQueries.CalculateEntityCount();

        EntityManager.GetAllUniqueSharedComponentData(boidTypes);
        for (int typeIndex = 0; typeIndex < boidTypes.Count; typeIndex++)
        {
            BoidData boidType = boidTypes[typeIndex];
            boidQueries.AddSharedComponentFilter(boidType);
            int totalBoid = boidQueries.CalculateEntityCount();

            if (totalBoid == 0)
            {
                boidQueries.ResetFilter();
                continue;
            }

            NativeMultiHashMap<uint, BoidInfo> multiHashMap = new NativeMultiHashMap<uint, BoidInfo>(totalBoid, Allocator.TempJob);
            NativeArray<float3> alignment = new NativeArray<float3>(totalBoid, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<float3> separation = new NativeArray<float3>(totalBoid, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<DistanceInfo> towardTargetVector = new NativeArray<DistanceInfo>(totalBoid, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<DistanceInfo> avoidPredatorVector = new NativeArray<DistanceInfo>(totalBoid, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<float3> targetPos = new NativeArray<float3>(totalTarget, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<float3> predatorPos = new NativeArray<float3>(totalPredator, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<uint> hashBoidPos = new NativeArray<uint>(totalBoid, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            JobHandle populateTargetPosHandle = Entities
                .WithAll<TargetTagData>()
                .WithName("PopulateTargetPosition")
                .ForEach((int entityInQueryIndex, in Translation pos) =>
                {
                    targetPos[entityInQueryIndex] = pos.Value;
                })
                .ScheduleParallel(Dependency);

            JobHandle populatePredatorPosHandle = Entities
                .WithAll<PredatorTagData>()
                .WithName("PopulatePredatorPosition")
                .ForEach((int entityInQueryIndex, in Translation pos) =>
                {
                    predatorPos[entityInQueryIndex] = pos.Value;
                })
                .ScheduleParallel(Dependency);

            JobHandle populateHashHandle = Entities
                .WithSharedComponentFilter(boidType)
                .WithName("PopulateHashPositionValue")
                .ForEach((int entityInQueryIndex, in Translation pos) =>
                {
                    hashBoidPos[entityInQueryIndex] = math.hash(new int3(math.floor(pos.Value / boidType.neighborRadius)));
                })
                .ScheduleParallel(Dependency);

            JobHandle findNearestTargetHandle = Entities
                .WithSharedComponentFilter(boidType)
                .WithName("FindNearestTarget")
                .ForEach((int entityInQueryIndex, in Translation pos) =>
                {
                    float nearestDistanceSq = INFINITE;
                    int nearestIdx = -1;

                    for (int i = 0; i < targetPos.Length; i++)
                    {
                        float tempLengthSq = math.lengthsq(pos.Value - targetPos[i]);
                        if (tempLengthSq < nearestDistanceSq)
                        {
                            nearestIdx = i;
                            nearestDistanceSq = tempLengthSq;
                        }
                    }
                    towardTargetVector[entityInQueryIndex] = new DistanceInfo
                    {
                        distanceSq = nearestDistanceSq,
                        vector = math.normalizesafe(targetPos[nearestIdx] - pos.Value)
                    };

                })
                .WithReadOnly(targetPos)
                .ScheduleParallel(populateTargetPosHandle);

            JobHandle findNearestPredatorHandle = Entities
                .WithSharedComponentFilter(boidType)
                .WithName("FindNearestPredator")
                .ForEach((int entityInQueryIndex, in Translation pos) =>
                {
                    float nearestDistanceSq = INFINITE;
                    int nearestIdx = -1;

                    for (int i = 0; i < predatorPos.Length; i++)
                    {
                        float tempLengthSq = math.lengthsq(pos.Value - predatorPos[i]);
                        if (tempLengthSq < nearestDistanceSq)
                        {
                            nearestIdx = i;
                            nearestDistanceSq = tempLengthSq;
                        }
                    }
                    avoidPredatorVector[entityInQueryIndex] = new DistanceInfo
                    {
                        distanceSq = nearestDistanceSq,
                        vector = math.normalizesafe(pos.Value - predatorPos[nearestIdx])
                    };
                })
                .WithReadOnly(predatorPos)
                .ScheduleParallel(populatePredatorPosHandle);

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

            JobHandle synchcronizePoint = JobHandle.CombineDependencies(calculateFlockVectorHandle, findNearestPredatorHandle, findNearestTargetHandle);

            JobHandle steerBoidHandle = Entities
                .WithSharedComponentFilter(boidType)
                .WithName("SteerBoid")
                .ForEach((int entityInQueryIndex, ref HeadingData heading, ref Translation pos, ref Rotation rot) =>
                {
                    float3 vectDir = boidType.alignmentWeight * alignment[entityInQueryIndex] + boidType.separationWeight * separation[entityInQueryIndex]
                                   + boidType.targetWeight * towardTargetVector[entityInQueryIndex].vector;

                    if (avoidPredatorVector[entityInQueryIndex].distanceSq <= boidType.minPredatorDistance * boidType.minPredatorDistance)
                    {
                        vectDir = avoidPredatorVector[entityInQueryIndex].vector;
                    }

                    heading.Value += vectDir;
                    heading.Value = math.normalizesafe(heading.Value);

                    rot.Value = quaternion.LookRotationSafe(heading.Value, math.up());
                    pos.Value += heading.Value * boidType.maxSpeed * deltaTime;
                })
                .WithReadOnly(alignment)
                .WithReadOnly(separation)
                .WithReadOnly(towardTargetVector)
                .WithReadOnly(avoidPredatorVector)
                .ScheduleParallel(synchcronizePoint);

            Dependency = steerBoidHandle;
            JobHandle disposeJobHandle = multiHashMap.Dispose(Dependency);
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, multiHashMapKey.Item1.Dispose(Dependency));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, targetPos.Dispose(Dependency));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, predatorPos.Dispose(Dependency));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, towardTargetVector.Dispose(Dependency));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, avoidPredatorVector.Dispose(Dependency));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, alignment.Dispose(Dependency));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, separation.Dispose(Dependency));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, hashBoidPos.Dispose(Dependency));
            Dependency = disposeJobHandle;

            boidQueries.ResetFilter();
        }
        boidTypes.Clear();
    }
}
