using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(100)]
public struct RigidBodyBufferElement : IBufferElementData
{
    public float3 position;
    public float3 velocity;
}
