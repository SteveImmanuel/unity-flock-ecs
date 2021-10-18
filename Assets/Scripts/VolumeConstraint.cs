using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public class VolumeConstraint : MonoBehaviour
{
    public float forceMagnitude;

    private float maxX, minX;
    private float maxY, minY;
    private float maxZ, minZ;

    [Header("Wall Settings")]
    public bool xPositive = true;
    public bool xNegative = true;
    public bool yPositive = true;
    public bool yNegative = true;
    public bool zPositive = true;
    public bool zNegative = true;

    private void Start()
    {
        maxX = transform.position.x + transform.localScale.x / 2;
        maxY = transform.position.y + transform.localScale.y / 2;
        maxZ = transform.position.z + transform.localScale.z / 2;
        minX = transform.position.x - transform.localScale.x / 2;
        minY = transform.position.y - transform.localScale.y / 2;
        minZ = transform.position.z - transform.localScale.z / 2;
    }

    private float InverseSquared(float value)
    {
        return 1 / Mathf.Pow(value, 2);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Boid")) return;

        Vector3 position = other.gameObject.transform.position;

        float xForce = 0, yForce = 0, zForce = 0;
        if (xPositive)
        {
            xForce -= InverseSquared(maxX - position.x);
        }
        if (xNegative)
        {
            xForce += InverseSquared(position.x - minX);
        }
        if (yPositive)
        {
            yForce -= InverseSquared(maxY - position.y);
        }
        if (yNegative)
        {
            yForce += InverseSquared(position.y - minY);
        }
        if (zPositive)
        {
            zForce -= InverseSquared(maxZ - position.z);
        }
        if (zNegative)
        {
            zForce += InverseSquared(position.z - minZ);
        }

        xForce *= forceMagnitude;
        yForce *= forceMagnitude;
        zForce *= forceMagnitude;
        other.attachedRigidbody.AddForce(new Vector3(xForce, yForce, zForce), ForceMode.Acceleration);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
