using Unity.Entities;
using Unity.Mathematics;

public struct SpeedData : IComponentData
{
    public float maximum;
    public float3 velocity;
}
