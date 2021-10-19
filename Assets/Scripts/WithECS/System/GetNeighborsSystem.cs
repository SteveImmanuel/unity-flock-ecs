using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

[UpdateBefore(typeof(FlockBehaviourSystem))]
public class GetNeighborSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityQuery queries = GetEntityQuery(typeof(Translation), typeof(BoidTagData));
        NativeArray<Translation> positions = queries.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<BoidMoveData> moveData = queries.ToComponentDataArray<BoidMoveData>(Allocator.TempJob);
        NativeArray<BoidTagData> tags = queries.ToComponentDataArray<BoidTagData>(Allocator.TempJob);
        float neighborRadius = FlockManagerECS.instance.neighborRadius;

        JobHandle handler = Entities.ForEach((ref DynamicBuffer<RigidBodyBufferElement> neighbors, in Translation pos, in BoidTagData tag) =>
        {
            neighbors.Clear();
            for (int i = 0; i < positions.Length; i++)
            {
                if (tag.uid == tags[i].uid) continue;
                if (math.distancesq(pos.Value, positions[i].Value) < neighborRadius)
                {
                    neighbors.Add(new RigidBodyBufferElement { position = positions[i].Value, velocity = moveData[i].velocity });
                }
            }
        })
            .WithReadOnly(positions)
            .WithReadOnly(tags)
            .WithReadOnly(moveData)
            .WithDisposeOnCompletion(positions)
            .WithDisposeOnCompletion(tags)
            .WithDisposeOnCompletion(moveData)
            .Schedule(inputDeps);

        return handler;
    }
}
