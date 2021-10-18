using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlockMove : MonoBehaviour
{
    [Header("Barell Roll Config")]
    public AnimationCurve rotationCurve;
    public int totalRotation = 1;
    public float rotationFrames = 300;

    [Header("Move Config")]
    public float maxSpeed = 10f;
    public float neighborRadius = 2f;
    public float alignmentWeight = 1;
    public float cohesionWeight = 1;
    public float separationWeight = 2;
    public LayerMask boidLayer;

    private Vector3 velocity;
    private Rigidbody rb;
    private Collider[] neighbors;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {

        velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * maxSpeed;
        rb.velocity = velocity;
    }

    void Update()
    {
        neighbors = GetNeighbors();

        rb.velocity += CalculateFlockVector();

        // contraint to always look at velocity direction
        LookAtWithoutRoll(rb.velocity);

        // clamp speed
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
    }

    private Collider[] GetNeighbors()
    {
        return Physics.OverlapSphere(transform.position, neighborRadius, boidLayer);
    }

    private Vector3 CalculateFlockVector()
    {
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;

        foreach (Collider neighbor in neighbors)
        {


            if (neighbor.transform == transform) continue;
            alignment += neighbor.attachedRigidbody.velocity;
            cohesion += neighbor.transform.position;
            separation += transform.position - neighbor.transform.position;
        }

        if (neighbors.Length > 1) // min 1 from self
        {
            alignment /= neighbors.Length - 1;
            cohesion /= neighbors.Length - 1;
            separation /= neighbors.Length - 1;
            cohesion = cohesion - transform.position;
        } 

        return alignment.normalized * alignmentWeight + cohesion.normalized * cohesionWeight + separation.normalized * separationWeight;
    }

    private void LookAtWithoutRoll(Vector3 direction)
    {
        float zDegree = transform.localEulerAngles.z;
        transform.rotation = Quaternion.LookRotation(direction);
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, zDegree);
    }

    private IEnumerator BarrelRoll()
    {
        float sum = 0;
        for (int i = 1; i <= rotationFrames; i++)
        {
            sum += rotationCurve.Evaluate(i / rotationFrames);
        }

        for (int i = 1; i <= rotationFrames; i++)
        {
            float degree = rotationCurve.Evaluate(i / rotationFrames) / sum * 360 * totalRotation;
            transform.rotation *= Quaternion.Euler(0, 0, degree);
            yield return null;
        }
    }

}
