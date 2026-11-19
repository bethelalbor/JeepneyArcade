using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 4f;
    public float acceleration = 3f;
    public float deceleration = 3f;
    [Tooltip("How quickly the vehicle slows when reverse is pressed while moving forward, or vice versa.")]
    public float brakeDeceleration = 10f;
    [Tooltip("Opposite-direction input stops the vehicle immediately at or below this speed before reversing.")]
    public float brakeToReverseSpeedThreshold = 0.25f;
    public float rotationSpeed = 25f;
    [Tooltip("How quickly sideways momentum is reduced while driving.")]
    public float grip = 6f;
    [Tooltip("Grip multiplier applied when driving at maximum speed.")]
    public float highSpeedGripMultiplier = 3f;

    [Header("Steering Settings")]
    [Tooltip("The vehicle must reach this speed before it can begin turning. This prevents a tiny nudge from causing a large rotation.")]
    public float minimumSteeringSpeed = 1.5f;
    [Tooltip("Makes steering build up more gradually at low speed. Values greater than 1 make slow-speed turns harder.")]
    [Min(1f)] public float lowSpeedSteeringExponent = 1.5f;


    [Header("Input Settings")]
    public float inputDeadzone = 0.15f;
    public float steeringBuildUpSpeed = 8f;

    [Header("Crash Response")]
    [Tooltip("Visual model only; keep Rigidbody and colliders outside this hierarchy.")]
    [SerializeField] private Transform crashVisual;
    [SerializeField, Range(0f, 8f)] private float maximumCrashTilt = 4f;
    [SerializeField, Min(0.1f)] private float crashSettleDuration = 0.5f;
    [SerializeField, Min(0f)] private float minimumCrashSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float fullCrashSpeed = 10f;

    private readonly List<Vector3> obstacleNormals = new List<Vector3>(8);
    private Vector3 visualRestPosition;
    private Quaternion visualRestRotation;
    private Vector3 crashPivot;
    private Vector3 crashAxis;
    private float crashAngle;
    private float crashStartedAt = float.NegativeInfinity;
    private bool visualReady;


    private float currentSpeed = 0f;
    private float turnInput = 0f;
    private float activeSteering = 0f;


    private Rigidbody rb;
    private PlayerControls controls;
    private Vector2 moveInput;
    private bool mobileSteerLeft;
    private bool mobileSteerRight;
    private bool mobileAccelerating;
    private bool mobileBraking;


    private bool movementLocked = false;

    // Read-only startup status for the loading screen; driving behavior is unchanged.
    public bool IsReadyForGameplay => isActiveAndEnabled && rb != null &&
        !rb.isKinematic && !movementLocked && controls != null && controls.Vehicle.Move.enabled;
    public int PhysicsUpdateCount { get; private set; }



    void Awake()
    {
        controls = new PlayerControls();

        controls.Vehicle.Move.performed += ctx =>
        {
            moveInput = ctx.ReadValue<Vector2>();
        };


        controls.Vehicle.Move.canceled += ctx =>
        {
            moveInput = Vector2.zero;
        };
    }



    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Physics cannot turn the vehicle; heading changes come from steering only.
        rb.constraints |= RigidbodyConstraints.FreezeRotation;
        rb.angularVelocity = Vector3.zero;

        if (crashVisual != null && crashVisual.parent == transform &&
            crashVisual.GetComponentInChildren<Collider>(true) == null &&
            crashVisual.GetComponentInChildren<Rigidbody>(true) == null)
        {
            visualRestPosition = crashVisual.localPosition;
            visualRestRotation = crashVisual.localRotation;
            visualReady = true;
        }
    }



    void Update() //eto naghandle ng input at movement logic
    {
        if(movementLocked)
            return;


        float mobileThrottle = mobileAccelerating == mobileBraking
            ? 0f
            : mobileAccelerating ? 1f : -1f;
        float throttle = Mathf.Clamp(moveInput.y + mobileThrottle + GetGamepadThrottle(), -1f, 1f);
        float mobileTurn = mobileSteerLeft == mobileSteerRight
            ? 0f
            : mobileSteerLeft ? -1f : 1f;
        float rawTurn = Mathf.Clamp(moveInput.x + mobileTurn + GetGamepadSteering(), -1f, 1f);


        turnInput =
            Mathf.Abs(rawTurn) < inputDeadzone
            ? 0f
            : rawTurn;


        if(throttle != 0)
        {
            bool opposingDirection = currentSpeed != 0f &&
                Mathf.Sign(throttle) != Mathf.Sign(currentSpeed);

            if (opposingDirection)
            {
                if (Mathf.Abs(currentSpeed) <= brakeToReverseSpeedThreshold)
                {
                    currentSpeed = 0f;
                }
                else
                {
                    currentSpeed = Mathf.MoveTowards(
                        currentSpeed,
                        0f,
                        brakeDeceleration * Time.deltaTime
                    );
                }
            }
            else
            {
                currentSpeed += //eto yung sa acceleration
                    throttle *
                    acceleration *
                    Time.deltaTime;
            }
        }
        else
        {
            currentSpeed = //reduce ng speed gradually kapag walang input
                Mathf.Lerp(
                    currentSpeed,
                    0f,
                    deceleration *
                    Time.deltaTime
                );
        }


        currentSpeed = //restrict speed to max and half non pag nagreverse
            Mathf.Clamp(
                currentSpeed,
                -maxSpeed / 2f,
                maxSpeed
            );

    }



    void FixedUpdate() //eto nagapply ng physics sa vehicle movement
    {
        if(movementLocked)
        {
            obstacleNormals.Clear();
            return;
        }


        // Keep forward drive responsive while allowing lateral momentum to carry
        // through a turn. Grip gradually brings that sideways motion back in line.
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float actualForwardSpeed = localVelocity.z;
        PhysicsUpdateCount++;
        // Do not continually restore motor speed into a surface that stopped us.
        foreach (Vector3 normal in obstacleNormals)
        {
            if (Vector3.Dot(transform.forward * currentSpeed, normal) < -0.001f)
            {
                currentSpeed = 0f;
                break;
            }
        }
        localVelocity.z = currentSpeed;
        float speedPercent = maxSpeed > 0f
            ? Mathf.Clamp01(Mathf.Abs(localVelocity.z) / maxSpeed)
            : 0f;
        float lateralGrip = Mathf.Lerp(
            grip,
            grip * highSpeedGripMultiplier,
            speedPercent
        );
        localVelocity.x = Mathf.MoveTowards(
            localVelocity.x,
            0f,
            lateralGrip * Time.fixedDeltaTime
        );

        Vector3 drivenVelocity = transform.TransformDirection(localVelocity); //moving without changing vertical velocity or magtip over
        // Remove inward lateral motion as well, including contacts at corners.
        for (int pass = 0; pass < 2; pass++)
        {
            foreach (Vector3 normal in obstacleNormals)
            {
                float inwardSpeed = Vector3.Dot(drivenVelocity, normal);
                if (inwardSpeed < 0f)
                    drivenVelocity -= normal * inwardSpeed;
            }
        }
        // Collision callbacks repopulate this after each simulation step.
        obstacleNormals.Clear();
        rb.linearVelocity = new Vector3(
            drivenVelocity.x,
            rb.linearVelocity.y,
            drivenVelocity.z
        );


        float speedMagnitude = Mathf.Min(Mathf.Abs(currentSpeed), Mathf.Abs(actualForwardSpeed));
        if(speedMagnitude <= minimumSteeringSpeed)
        {
            activeSteering = 0f;
            return;
        }


        if(turnInput == 0f)
        {
            activeSteering = 0f;
        }
        else
        {
            if(Mathf.Sign(activeSteering) != Mathf.Sign(turnInput))
                activeSteering = 0f;


            activeSteering = Mathf.MoveTowards(
                activeSteering,
                turnInput,
                steeringBuildUpSpeed * Time.fixedDeltaTime
            );
        }

        float speedRange = Mathf.Max(maxSpeed - minimumSteeringSpeed, 0.001f);
        float normalizedSteeringSpeed = Mathf.Clamp01(
            (speedMagnitude - minimumSteeringSpeed) / speedRange
        );
        float steeringSpeedFactor = Mathf.Pow(
            normalizedSteeringSpeed,
            lowSpeedSteeringExponent
        );
        float directionFactor = Mathf.Sign(currentSpeed);


        Quaternion turn =
            Quaternion.Euler(
                Vector3.up *
                activeSteering *
                directionFactor *
                steeringSpeedFactor *
                rotationSpeed *
                Time.fixedDeltaTime
            );


        rb.MoveRotation( //pang steer
            rb.rotation *
            turn
        );
    }

    //eto yung tilt ng vehicle kapag nag crash 
    private void OnCollisionEnter(Collision collision)
    {
        RecordObstacleContacts(collision, true);
    }

    private void OnCollisionStay(Collision collision)
    {
        RecordObstacleContacts(collision, false);
    }

    private void RecordObstacleContacts(Collision collision, bool newImpact)
    {
        float strongestImpact = 0f;
        ContactPoint strongestContact = default;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            // Roads and driveable slopes must not act like walls or trigger rocking.
            if (Mathf.Abs(contact.normal.y) > 0.5f)
                continue;

            Vector3 normal = Vector3.ProjectOnPlane(contact.normal, Vector3.up).normalized;
            obstacleNormals.Add(normal);
            // Use the normal component's magnitude; Unity's callback pair ordering
            // can reverse the relative-velocity direction. Tangential scrapes stay small.
            float impactSpeed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal));
            if (impactSpeed > strongestImpact)
            {
                strongestImpact = impactSpeed;
                strongestContact = contact;
            }
        }

        if (!newImpact || movementLocked || !visualReady || strongestImpact < minimumCrashSpeed ||
            Time.time < crashStartedAt + crashSettleDuration)
            return;

        // Pivot on the contact side at the body's bottom so the opposite side rises.
        Bounds bodyBounds = strongestContact.thisCollider.bounds;
        Vector3 pivotWorld = bodyBounds.ClosestPoint(strongestContact.point);
        pivotWorld.y = bodyBounds.min.y;
        crashPivot = transform.InverseTransformPoint(pivotWorld);
        Vector3 contactDirection = transform.InverseTransformDirection(
            Vector3.ProjectOnPlane(strongestContact.point - bodyBounds.center, Vector3.up));
        if (contactDirection.sqrMagnitude < 0.001f)
            contactDirection = transform.InverseTransformDirection(-strongestContact.normal);
        contactDirection.y = 0f;
        crashAxis = Vector3.Cross(Vector3.up, contactDirection.normalized);
        crashAngle = maximumCrashTilt * Mathf.Clamp01(strongestImpact / Mathf.Max(fullCrashSpeed, 0.1f));
        crashStartedAt = Time.time;
    }

    private void LateUpdate()
    {
        if (!visualReady)
            return;

        float progress = Mathf.Clamp01((Time.time - crashStartedAt) / Mathf.Max(crashSettleDuration, 0.1f));
        // Quick lift, then a soft return with zero velocity at either end.
        float envelope = progress < 0.2f
            ? Mathf.SmoothStep(0f, 1f, progress / 0.2f)
            : 1f - Mathf.SmoothStep(0f, 1f, (progress - 0.2f) / 0.8f);
        Quaternion tilt = Quaternion.AngleAxis(crashAngle * envelope, crashAxis);
        crashVisual.localRotation = tilt * visualRestRotation;
        crashVisual.localPosition = crashPivot + tilt * (visualRestPosition - crashPivot);
    }

    private void ResetCrashVisual()
    {
        crashStartedAt = float.NegativeInfinity;
        if (!visualReady)
            return;
        crashVisual.localPosition = visualRestPosition;
        crashVisual.localRotation = visualRestRotation;
    }


    public void DisableMovement()
    {
        movementLocked = true;

        currentSpeed = 0f;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }



    public void EnableMovement()
    {
        movementLocked = false;
        obstacleNormals.Clear();
        ResetCrashVisual();


        currentSpeed = 0f;
        turnInput = 0f;
        activeSteering = 0f;


        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;


        // Force input system to refresh
        controls.Vehicle.Move.Enable();


        Debug.Log("Movement restored");
    }



    public void SetMobileSteering(bool steeringLeft, bool isPressed)
    {
        if (steeringLeft)
            mobileSteerLeft = isPressed;
        else
            mobileSteerRight = isPressed;
    }



    public void SetMobileThrottle(bool accelerating, bool isPressed) //check if nakapindot sa accelerate or brake button sa mobile
    {
        if (accelerating)
            mobileAccelerating = isPressed;
        else
            mobileBraking = isPressed;
    }



    private float GetGamepadThrottle()
    {
        if (Gamepad.current == null)
            return 0f;

        Gamepad gamepad = Gamepad.current;
        float accelerate = Mathf.Max(
            gamepad.buttonSouth.isPressed ? 1f : 0f,
            gamepad.rightTrigger.ReadValue());
        float brake = Mathf.Max(
            gamepad.buttonEast.isPressed ? 1f : 0f,
            gamepad.leftTrigger.ReadValue());

        return Mathf.Clamp(accelerate - brake, -1f, 1f);
    }



    private float GetGamepadSteering()
    {
        if (Gamepad.current == null)
            return 0f;

        bool steeringLeft = Gamepad.current.dpad.left.isPressed;
        bool steeringRight = Gamepad.current.dpad.right.isPressed;

        return steeringLeft == steeringRight
            ? 0f
            : steeringLeft ? -1f : 1f;
    }



    void OnEnable()
    {
        controls.Enable();
    }



    void OnDisable()
    {
        obstacleNormals.Clear();
        ResetCrashVisual();
        controls.Disable();
        mobileSteerLeft = false;
        mobileSteerRight = false;
        mobileAccelerating = false;
        mobileBraking = false;
        activeSteering = 0f;
    }
}
