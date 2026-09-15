using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Framing")]
    public Vector3 localOffset = new Vector3(0, 300, -300); // distance/height relative to target's facing direction
    public float followSmoothness = 8f;   // higher = snappier position tracking
    public float rotationSmoothness = 8f; // higher = snappier rotation tracking

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleMask;
    public float collisionRadius = 0.3f;
    public float minDistance = 100f;

    void Start()
    {
        // If you'd rather keep the exact offset you already had in the editor,
        // convert it into target-local space once at start instead of the line above:
        // localOffset = Quaternion.Inverse(target.rotation) * (transform.position - target.position);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Rotate the offset with the target's current facing direction,
        // so turning left/right keeps the jeepney centered instead of drifting out of frame
        Vector3 desiredOffset = target.rotation * localOffset;
        Vector3 desiredPosition = target.position + desiredOffset;

        // Obstacle check so buildings don't block the view
        Vector3 direction = desiredOffset.normalized;
        float distance = desiredOffset.magnitude;

        if (Physics.SphereCast(target.position, collisionRadius, direction, out RaycastHit hit, distance, obstacleMask))
        {
            distance = Mathf.Clamp(hit.distance, minDistance, distance);
            desiredPosition = target.position + direction * distance;
        }

        // Smoothly move + rotate to always look at the target
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSmoothness);

        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSmoothness);
    }
}