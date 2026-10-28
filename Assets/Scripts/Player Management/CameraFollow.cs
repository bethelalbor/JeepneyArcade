using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Framing")]
    public Vector3 localOffset = new Vector3(0, 300, -300); // distance/height relative to target's facing direction
    public float followSmoothness = 8f;   // higher = snappier position tracking
    public float rotationSmoothness = 8f; // higher = snappier rotation tracking
    [Tooltip("Seconds to wait after the vehicle starts turning before the follow offset turns with it.")]
    [Min(0f)] public float turnFollowDelay = 1f;
    [Tooltip("Seconds for the follow offset to ease into the vehicle's new direction after the turn delay.")]
    [Min(0f)] public float turnFollowCatchUpDuration = 0.75f;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleMask;
    public float collisionRadius = 0.3f;
    public float minDistance = 100f;

    [Header("Damage Shake")]
    [Tooltip("Maximum local camera offset caused by collision damage.")]
    [Min(0f)] public float maxDamageShakeStrength = 0.6f;
    [Tooltip("Maximum amount of time a collision shake can last.")]
    [Min(0f)] public float maxDamageShakeDuration = 0.35f;
    [Tooltip("Damage required to reach the maximum shake intensity.")]
    [Min(0.01f)] public float damageForMaxShake = 50f;
    [Tooltip("How quickly the shake strength returns to zero.")]
    [Min(0f)] public float shakeFadeSpeed = 2f;

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
    private float currentShakeStrength;
    private float shakeTimeRemaining;
    private Vector3 previousShakeOffset;
    private Quaternion delayedFollowRotation;
    private Quaternion currentFollowRotation;
    private Quaternion lastTargetRotation;
    private float lastTurnMotionTime;
    private float lastTurnDirection;
    private float turnFollowResumeTime;
    private bool turnFollowDelayed;
    private Quaternion turnCatchUpStartRotation;
    private float turnCatchUpStartTime;
    private bool turnFollowCatchingUp;
    private bool targetWasTurning;
    private bool turnFollowInitialized;

    void Start()
    {
        if (target != null)
        {
            delayedFollowRotation = target.rotation;
            currentFollowRotation = target.rotation;
            lastTargetRotation = target.rotation;
            lastTurnMotionTime = float.NegativeInfinity;
            turnFollowInitialized = true;
        }

        // If you'd rather keep the exact offset you already had in the editor,
        // convert it into target-local space once at start instead of the line above:
        // localOffset = Quaternion.Inverse(target.rotation) * (transform.position - target.position);
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleLookInput();
        ReturnToFollowViewWhenIdle();

        if (!turnFollowInitialized)
        {
            delayedFollowRotation = target.rotation;
            currentFollowRotation = target.rotation;
            lastTargetRotation = target.rotation;
            lastTurnMotionTime = float.NegativeInfinity;
            turnFollowInitialized = true;
        }

        float turnDelta = Mathf.DeltaAngle(
            lastTargetRotation.eulerAngles.y,
            target.rotation.eulerAngles.y
        );
        bool targetRotationChanged = Mathf.Abs(turnDelta) > 0.01f;

        if (targetRotationChanged)
        {
            lastTurnMotionTime = Time.time;
        }

        // Keep turn detection stable between physics updates, which can occur less
        // frequently than LateUpdate.
        bool targetIsTurning = Time.time <= lastTurnMotionTime + 0.1f;
        bool turnDirectionChanged = targetRotationChanged && targetWasTurning &&
            Mathf.Sign(turnDelta) != lastTurnDirection;

        if (targetIsTurning && (!targetWasTurning || turnDirectionChanged))
        {
            delayedFollowRotation = currentFollowRotation;
            turnFollowResumeTime = Time.time + turnFollowDelay;
            turnFollowDelayed = true;
            turnFollowCatchingUp = false;
        }

        if (targetRotationChanged)
            lastTurnDirection = Mathf.Sign(turnDelta);

        if (turnFollowDelayed && Time.time >= turnFollowResumeTime)
        {
            turnFollowDelayed = false;
            turnCatchUpStartRotation = delayedFollowRotation;
            turnCatchUpStartTime = Time.time;
            turnFollowCatchingUp = true;
        }

        // Hold the follow heading briefly when a turn begins to avoid camera jitter on small turns.
        Quaternion followRotation;

        if (turnFollowDelayed)
        {
            followRotation = delayedFollowRotation;
        }
        else if (turnFollowCatchingUp)
        {
            float catchUpProgress = turnFollowCatchUpDuration > 0f
                ? Mathf.Clamp01((Time.time - turnCatchUpStartTime) / turnFollowCatchUpDuration)
                : 1f;
            float easedProgress = catchUpProgress * catchUpProgress *
                (3f - 2f * catchUpProgress);

            followRotation = Quaternion.Slerp(
                turnCatchUpStartRotation,
                target.rotation,
                easedProgress
            );

            if (catchUpProgress >= 1f)
                turnFollowCatchingUp = false;
        }
        else
        {
            followRotation = target.rotation;
        }

        Vector3 desiredOffset = followRotation * Quaternion.Euler(verticalPanAngle, panAngle, 0f) * localOffset;

        lastTargetRotation = target.rotation;
        currentFollowRotation = followRotation;
        targetWasTurning = targetIsTurning;
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
        Vector3 cameraPosition = Vector3.Lerp(
            transform.position - previousShakeOffset,
            desiredPosition,
            Time.deltaTime * followSmoothness
        );

        previousShakeOffset = GetDamageShakeOffset();

        cameraPosition += previousShakeOffset;

        transform.position = cameraPosition;

        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSmoothness);
    }

    public void ShakeForDamage(float damage)
    {
        if(damage <= 0f)
            return;

        float damagePercentage = Mathf.Clamp01(
            damage / damageForMaxShake
        );

        currentShakeStrength = Mathf.Max(
            currentShakeStrength,
            maxDamageShakeStrength * damagePercentage
        );

        shakeTimeRemaining = Mathf.Max(
            shakeTimeRemaining,
            maxDamageShakeDuration * damagePercentage
        );
    }

    private Vector3 GetDamageShakeOffset()
    {
        if(shakeTimeRemaining <= 0f)
            return Vector3.zero;

        shakeTimeRemaining -= Time.unscaledDeltaTime;

        currentShakeStrength = Mathf.MoveTowards(
            currentShakeStrength,
            0f,
            shakeFadeSpeed * Time.unscaledDeltaTime
        );

        Vector2 shakeDirection =
            Random.insideUnitCircle * currentShakeStrength;

        return transform.right * shakeDirection.x +
               transform.up * shakeDirection.y;
    }

    private void HandleLookInput()
    {
        Vector2 panDelta = Vector2.zero;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
        {
            if (touchscreen.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved)
                panDelta = touchscreen.primaryTouch.delta.ReadValue() * touchPanSensitivity;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            panDelta = Mouse.current.delta.ReadValue() * mousePanSensitivity;
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
