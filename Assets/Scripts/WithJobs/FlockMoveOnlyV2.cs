using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockMoveOnlyV2 : MonoBehaviour
{
    [Header("Barell Roll Config")]
    public AnimationCurve rotationCurve;
    public int totalRotation = 1;
    public float rotationFrames = 300;

    [Header("Move Config")]
    public float maxSpeed = 10f;

    private Vector3 velocity;

    void Start()
    {
        velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * maxSpeed;
    }

    void Update()
    {
        // contraint to always look at velocity direction
        LookAtWithoutRoll(velocity);

        // clamp speed
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        transform.position += velocity * Time.deltaTime;
    }

    public void AddVelocity(Vector3 vel)
    {
        velocity += vel;
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
