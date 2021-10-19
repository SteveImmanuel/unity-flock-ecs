using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;
using Unity.Transforms;
using System;

public class FlockBehaviourSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityQuery queries = GetEntityQuery(typeof(Translation), typeof(BoidTagData), typeof(PhysicsVelocity));
        NativeArray<Translation> positions = queries.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<PhysicsVelocity> velocities = queries.ToComponentDataArray<PhysicsVelocity>(Allocator.TempJob);
        NativeArray<BoidTagData> tags = queries.ToComponentDataArray<BoidTagData>(Allocator.TempJob);
        float neighborRadius = FlockManagerECS.instance.neighborRadius;
        float maxNeighbor = FlockManagerECS.instance.maxNeighbor;
        float3 flockWeights = new float3(FlockManagerECS.instance.alignmentWeight, FlockManagerECS.instance.cohesionWeight, FlockManagerECS.instance.separationWeight);


        JobHandle handler = Entities.ForEach((ref Rotation rot, ref PhysicsVelocity vel, in Translation pos, in BoidTagData tag, in MaxSpeed maxSpeed) =>
        {
            // normalize lambda function, local inside job
            float3 normalize(float3 vect)
            {
                float3 squared = math.pow(vect, 2);
                return vect / (math.sqrt(math.csum(squared)) + 1e-7f);
            }

            int totalNeighbor = 0;
            float3 alignment = float3.zero;
            float3 cohesion = float3.zero;
            float3 separation = float3.zero;

            for (int i = 0; i < positions.Length; i++)
            {
                if (totalNeighbor == maxNeighbor) break;
                if (tag.uid == tags[i].uid) continue;

                if (math.distancesq(pos.Value, positions[i].Value) < neighborRadius * neighborRadius)
                {
                    alignment += velocities[i].Linear;
                    cohesion += positions[i].Value;
                    separation += pos.Value - positions[i].Value;
                    totalNeighbor++;
                }
            }

            if (totalNeighbor > 0)
            {
                alignment /= totalNeighbor;
                cohesion /= totalNeighbor;
                separation /= totalNeighbor;
                cohesion -= pos.Value;
            }

            float3 flockVector = flockWeights.x * normalize(alignment) + flockWeights.y * normalize(cohesion) + flockWeights.z * normalize(separation);
            vel.Linear += flockVector;
            vel.Linear = normalize(vel.Linear) * maxSpeed.Value;
            rot.Value = quaternion.LookRotation(vel.Linear, math.up());
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
