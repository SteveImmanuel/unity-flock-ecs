using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

public struct BoidMoveData : IComponentData
{
    public float speed;
    public float3 velocity;
}
