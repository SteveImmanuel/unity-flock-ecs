using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class SpecialBoidAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float speed;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        float3 randomRot = new float3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
        randomRot = math.normalizesafe(randomRot);

        dstManager.SetComponentData(entity, new Rotation { Value = quaternion.LookRotationSafe(randomRot, math.up()) });
        dstManager.AddComponentData(entity, new HeadingData { Value = randomRot });
        dstManager.AddComponentData(entity, new MaxSpeedData { Value = speed });
    }
}
