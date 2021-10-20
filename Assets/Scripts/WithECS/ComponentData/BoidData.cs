using Unity.Entities;
using Unity.Mathematics;

public struct BoidData : ISharedComponentData
{
    public float maxSpeed;
    public float neighborRadius;
    public float separationWeight;
    public float alignmentWeight;
}
