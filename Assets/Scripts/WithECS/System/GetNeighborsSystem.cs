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
        EntityQuery queries = GetEntityQuery(typeof(Translation), typeof(BoidTagData), typeof(PhysicsVelocity));
        NativeArray<Translation> positions = queries.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<PhysicsVelocity> velocities = queries.ToComponentDataArray<PhysicsVelocity>(Allocator.TempJob);
        NativeArray<BoidTagData> tags = queries.ToComponentDataArray<BoidTagData>(Allocator.TempJob);
        float neighborRadius = FlockManagerECS.instance.neighborRadius;
        float maxNeighbor = FlockManagerECS.instance.maxNeighbor;

        JobHandle handler = Entities.ForEach((ref DynamicBuffer<RigidBodyBufferElement> neighbors, in Translation pos, in BoidTagData tag) =>
        {
            neighbors.Clear();
            for (int i = 0; i < positions.Length; i++)
            {
                if (tag.uid == tags[i].uid) continue;
                if (neighbors.Length == maxNeighbor) break;
                if (math.distancesq(pos.Value, positions[i].Value) < neighborRadius * neighborRadius)
                {
                    neighbors.Add(new RigidBodyBufferElement { position = positions[i].Value, velocity = velocities[i].Linear });
                }
            }
        })
            .WithReadOnly(positions)
            .WithReadOnly(tags)
            .WithReadOnly(velocities)
            .WithDisposeOnCompletion(positions)
            .WithDisposeOnCompletion(tags)
            .WithDisposeOnCompletion(velocities)
            .Schedule(inputDeps);

        return handler;
    }
}
