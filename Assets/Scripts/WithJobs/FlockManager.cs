using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

public class FlockManager : MonoBehaviour
{
    [Header("Move Config")]
    public float neighborRadius = 2f;
    public float alignmentWeight = 1;
    public float cohesionWeight = 1;
    public float separationWeight = 2;

    [Header("Job Config")]
    public int batchSize = 100;

    private Rigidbody[] rbs;
    private NativeArray<float3> positionArray;
    private NativeArray<float3> velocityArray;
    private NativeArray<float3> flockVector;
    private float3 flockWeights;

    void Start()
    {
        rbs = transform.GetComponentsInChildren<Rigidbody>();
        positionArray = new NativeArray<float3>(rbs.Length, Allocator.Persistent);
        velocityArray = new NativeArray<float3>(rbs.Length, Allocator.Persistent);
        flockVector = new NativeArray<float3>(rbs.Length, Allocator.Persistent);
    }

    void Update()
    {
        flockWeights = new float3(alignmentWeight, cohesionWeight, separationWeight);

        GetCurrentPositionAndVelocity();

        FlockUpdateJob flockUpdateJob = new FlockUpdateJob
        {
            positionArray = positionArray,
            velocityArray = velocityArray,
            flockVector = flockVector,
            flockWeights = flockWeights,
            neighborRadius = neighborRadius
        };
        JobHandle handler = flockUpdateJob.Schedule(rbs.Length, batchSize);
        handler.Complete();

        UpdateFlockVelocity();
    }

    private void GetCurrentPositionAndVelocity()
    {
        for (int i = 0; i < positionArray.Length; i++)
        {
            positionArray[i] = rbs[i].position;
            velocityArray[i] = rbs[i].velocity;
        }
    }

    private void UpdateFlockVelocity()
    {
        for (int i = 0; i < positionArray.Length; i++)
        {
            rbs[i].velocity += new Vector3(flockVector[i].x, flockVector[i].y, flockVector[i].z);
        }
    }

    [BurstCompile]
    private struct FlockUpdateJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> positionArray;
        [ReadOnly] public NativeArray<float3> velocityArray;
        public NativeArray<float3> flockVector;
        public float3 flockWeights;
        public float neighborRadius;

        public void Execute(int index)
        {
            float3 alignment = float3.zero;
            float3 cohesion = float3.zero;
            float3 separation = float3.zero;

            float3 currentPosition = positionArray[index];
            int totalNeighbor = 0;

            for (int i = 0; i < positionArray.Length; i++)
            {
                if (i == index) continue;

                if (CalculateDistanceSquared(currentPosition, positionArray[i]) <= math.pow(neighborRadius, 2))
                {
                    totalNeighbor++;
                    alignment += velocityArray[i];
                    cohesion += positionArray[i];
                    separation += currentPosition - positionArray[i];
                }

            }
            if (totalNeighbor > 0)
            {
                alignment /= totalNeighbor;
                cohesion /= totalNeighbor;
                separation /= totalNeighbor;
                cohesion -= currentPosition;
            }


            flockVector[index] = flockWeights.x * Normalize(alignment) + flockWeights.y * Normalize(cohesion) + flockWeights.z * Normalize(separation);
        }

        private float3 Normalize(float3 vect)
        {
            float3 squared = math.pow(vect, 2);
            return vect / (math.sqrt((squared.x + squared.y + squared.z)) + 1e-7f);
        }

        private float CalculateDistanceSquared(float3 pos1, float3 pos2)
        {
            float3 diffSquared = math.pow(pos1 - pos2, 2);
            return diffSquared.x + diffSquared.y + diffSquared.z;
        }
    }

    private void OnDestroy()
    {
        positionArray.Dispose();
        velocityArray.Dispose();
        flockVector.Dispose();
    }
}
