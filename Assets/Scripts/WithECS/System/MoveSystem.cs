using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Physics;

[UpdateAfter(typeof(FlockBehaviourSystem))]
public class MoveSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float deltaTime = Time.DeltaTime;
        return Entities.ForEach((ref Translation translation, in SpeedData speedData) =>
        {
            translation.Value += speedData.velocity * deltaTime;
        }).Schedule(inputDeps);
    }
}
