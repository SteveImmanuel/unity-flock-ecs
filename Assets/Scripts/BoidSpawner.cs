using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    public GameObject boidPrefab;
    public float spawnRadius;
    public int totalBoids;

    private void Awake()
    {
        for (int i = 0; i < totalBoids; i++)
        {
            Vector3 randomNormalizedVector = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            Vector3 randomPos = transform.position + randomNormalizedVector * Random.Range(0f, spawnRadius);
            Quaternion randomRot = Quaternion.LookRotation(randomNormalizedVector);
            GameObject boid = Instantiate(boidPrefab, randomPos, randomRot);
            boid.transform.parent = transform;
        }
    }

}
