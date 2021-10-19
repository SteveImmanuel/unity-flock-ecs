using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;

[UpdateBefore(typeof(GetNeighborSystem))]
public class CopyVelocitySystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle handler = Entities.ForEach((ref BoidMoveData moveData, in PhysicsVelocity vel) =>
        {
            moveData.velocity = vel.Linear;
        }).Schedule(inputDeps);

        return handler;
    }
}
