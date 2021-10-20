using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Physics;

[UpdateAfter(typeof(FlockBehaviourSystem))]
public class LookForwardSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return Entities.ForEach((ref Rotation rot, in SpeedData speedData) =>
        {
            rot.Value = quaternion.LookRotationSafe(speedData.velocity, math.up());
        }).Schedule(inputDeps);
    }
}
