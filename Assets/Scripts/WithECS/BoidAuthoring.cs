using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class BoidAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        BoidTagData tag = new BoidTagData { uid = Random.Range(0, 10000000) };
        BoidMoveData moveData = new BoidMoveData { speed = FlockManagerECS.instance.maxSpeed };

        dstManager.AddComponentData(entity, tag);
        dstManager.AddComponentData(entity, moveData);
        dstManager.AddBuffer<RigidBodyBufferElement>(entity);
    }
}
