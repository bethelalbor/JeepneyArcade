using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    private Vector3 offset;
    void Start()
    {
        // Store the original distance between camera and player
        offset = transform.position - target.position;
    }
    void LateUpdate()
    {
        // Follow player while keeping same camera angle and distance
        transform.position = target.position + offset;
    }
}