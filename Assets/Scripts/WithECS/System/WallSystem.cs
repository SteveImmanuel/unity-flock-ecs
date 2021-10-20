using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;

public class WallSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float outerMax = VolumeConstrainECS.instance.outerScale / 2;
        float outerMin = -outerMax;
        float innerMax = VolumeConstrainECS.instance.innerScale / 2;
        float innerMin = -innerMax;
        float forceMagnitude = VolumeConstrainECS.instance.forceMagnitude;
        float deltaTime = Time.DeltaTime;


        return Entities.ForEach((ref HeadingData heading, ref Translation pos, ref Rotation rot, in MaxSpeedData maxSpeed) =>
        {
            float xForce = 0, yForce = 0, zForce = 0;

            if (pos.Value.x > innerMax)
                xForce -= 1 / math.pow(outerMax - pos.Value.x, 2);
            if (pos.Value.x < innerMin)
                xForce += 1 / math.pow(pos.Value.x - outerMin, 2);
            if (pos.Value.y > innerMax)
                yForce -= 1 / math.pow(outerMax - pos.Value.y, 2);
            if (pos.Value.y < innerMin)
                yForce += 1 / math.pow(pos.Value.y - outerMin, 2);
            if (pos.Value.z > innerMax)
                zForce -= 1 / math.pow(outerMax - pos.Value.z, 2);
            if (pos.Value.z < innerMin)
                zForce += 1 / math.pow(pos.Value.z - outerMin, 2);

            float3 force = math.normalizesafe(new float3(xForce, yForce, zForce)) * 0.01f * maxSpeed.Value;
            heading.Value += force;
            heading.Value = math.normalizesafe(heading.Value);

            rot.Value = quaternion.LookRotationSafe(heading.Value, math.up());
            pos.Value += heading.Value * maxSpeed.Value * deltaTime;

        }).Schedule(inputDeps);
    }
}