using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeConstrainECS : MonoBehaviour
{
    public float forceMagnitude = 5;
    public float outerScale = 30;
    public float innerScale = 20;

    public static VolumeConstrainECS instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0);
        Gizmos.DrawWireCube(transform.position, new Vector3(outerScale, outerScale, outerScale));
        Gizmos.color = new Color(0, 0, 1);
        Gizmos.DrawWireCube(transform.position, new Vector3(innerScale, innerScale, innerScale));
    }
}
