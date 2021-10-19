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
        float3 flockWeights = new float3(FlockManagerECS.instance.alignmentWeight, FlockManagerECS.instance.cohesionWeight, FlockManagerECS.instance.separationWeight);
        JobHandle handler = Entities.ForEach((ref Rotation rot, ref PhysicsVelocity vel, in DynamicBuffer<RigidBodyBufferElement> neighbors, in Translation pos, in MaxSpeed maxSpeed) =>
        {
            // normalize lambda function, local inside job
            float3 normalize(float3 vect)
            {
                float3 squared = math.pow(vect, 2);
                return vect / (math.sqrt(math.csum(squared)) + 1e-7f);
            }

            float3 flockVector = float3.zero;

            if (neighbors.Length > 0)
            {
                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;
                float3 separation = float3.zero;

                for (int i = 0; i < neighbors.Length; i++)
                {
                    alignment += neighbors[i].velocity;
                    cohesion += neighbors[i].position;
                    separation += pos.Value - neighbors[i].position;

                }
                alignment /= neighbors.Length;
                cohesion /= neighbors.Length;
                separation /= neighbors.Length;
                cohesion -= pos.Value;

                flockVector = flockWeights.x * normalize(alignment) + flockWeights.y * normalize(cohesion) + flockWeights.z * normalize(separation);
            }

            vel.Linear += flockVector;
            vel.Linear = normalize(vel.Linear) * maxSpeed.Value;
        }).Schedule(inputDeps);

        return handler;
    }
}
