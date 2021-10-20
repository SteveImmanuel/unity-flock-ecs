using Unity.Entities;
using Unity.Mathematics;

public struct BoidData : ISharedComponentData
{
    public float maxSpeed;
    public float neighborRadius;
    public float alignmentWeight;
    public float separationWeight;
    public float targetWeight;
    public float minPredatorDistance;
}
