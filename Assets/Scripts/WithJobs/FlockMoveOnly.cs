using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlockMoveOnly : MonoBehaviour
{
    [Header("Barell Roll Config")]
    public AnimationCurve rotationCurve;
    public int totalRotation = 1;
    public float rotationFrames = 300;

    [Header("Move Config")]
    public float maxSpeed = 10f;

    private Vector3 velocity;
    private Rigidbody rb;

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
        // contraint to always look at velocity direction
        LookAtWithoutRoll(rb.velocity);

        // clamp speed
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
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
