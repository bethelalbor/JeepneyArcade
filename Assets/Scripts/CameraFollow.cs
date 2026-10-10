using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Look Around")]
    [Tooltip("Degrees the camera moves for each pixel dragged on a touch screen.")]
    public float touchPanSensitivity = 0.15f;
    [Tooltip("Degrees the camera moves for each mouse movement unit while the primary button is held.")]
    public float mousePanSensitivity = 6f;
    [Tooltip("Degrees per second the camera orbits at full right-stick deflection.")]
    public float gamepadPanSpeed = 90f;
    [Tooltip("Ignores minor right-stick movement to prevent camera drift.")]
    [Range(0f, 1f)] public float gamepadDeadzone = 0.1f;
    [Tooltip("Maximum horizontal orbit away from the normal behind-the-vehicle view.")]
    public float maxPanAngle = 60f;
    [Tooltip("Maximum vertical orbit away from the normal behind-the-vehicle view.")]
    public float maxVerticalPanAngle = 30f;
    [Tooltip("Seconds after the last pan before returning to the normal view.")]
    public float returnDelay = 2f;
    [Tooltip("Higher values return the camera to its normal view more quickly.")]
    public float returnSmoothness = 4f;

    private float panAngle;
    private float verticalPanAngle;
    private float lastPanTime;
    private bool hasPanned;

    void Start()
    {
        // If you'd rather keep the exact offset you already had in the editor,
        // convert it into target-local space once at start instead of the line above:
        // localOffset = Quaternion.Inverse(target.rotation) * (transform.position - target.position);
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleLookInput();
        ReturnToFollowViewWhenIdle();

        // Rotate the offset with the target's current facing direction,
        // so turning left/right keeps the jeepney centered instead of drifting out of frame
        Vector3 desiredOffset = target.rotation * Quaternion.Euler(verticalPanAngle, panAngle, 0f) * localOffset;
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

    private void HandleLookInput()
    {
        Vector2 panDelta = Vector2.zero;

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Moved)
                panDelta = touch.deltaPosition * touchPanSensitivity;
        }
        else if (Input.touchCount == 0 && Input.GetMouseButton(0))
        {
            panDelta = new Vector2(
                Input.GetAxisRaw("Mouse X"),
                Input.GetAxisRaw("Mouse Y")) * mousePanSensitivity;
        }

        if (Gamepad.current != null)
        {
            Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
            if (Mathf.Abs(rightStick.x) >= gamepadDeadzone)
                panDelta.x += rightStick.x * gamepadPanSpeed * Time.deltaTime;
            if (Mathf.Abs(rightStick.y) >= gamepadDeadzone)
                panDelta.y += rightStick.y * gamepadPanSpeed * Time.deltaTime;
        }

        if (panDelta == Vector2.zero)
            return;

        panAngle = Mathf.Clamp(panAngle + panDelta.x, -maxPanAngle, maxPanAngle);
        verticalPanAngle = Mathf.Clamp(verticalPanAngle + panDelta.y, -maxVerticalPanAngle, maxVerticalPanAngle);
        lastPanTime = Time.time;
        hasPanned = true;
    }

    private void ReturnToFollowViewWhenIdle()
    {
        if (!hasPanned || Time.time < lastPanTime + returnDelay)
            return;

        panAngle = Mathf.Lerp(panAngle, 0f, Time.deltaTime * returnSmoothness);
        verticalPanAngle = Mathf.Lerp(verticalPanAngle, 0f, Time.deltaTime * returnSmoothness);

        if (Mathf.Abs(panAngle) < 0.01f && Mathf.Abs(verticalPanAngle) < 0.01f)
        {
            panAngle = 0f;
            verticalPanAngle = 0f;
            hasPanned = false;
        }
    }
}
