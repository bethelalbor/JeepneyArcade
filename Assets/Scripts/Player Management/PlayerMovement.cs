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

        rb.constraints |=
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
    }



    void Update()
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
                currentSpeed +=
                    throttle *
                    acceleration *
                    Time.deltaTime;
            }
        }
        else
        {
            currentSpeed =
                Mathf.Lerp(
                    currentSpeed,
                    0f,
                    deceleration *
                    Time.deltaTime
                );
        }


        currentSpeed =
            Mathf.Clamp(
                currentSpeed,
                -maxSpeed / 2f,
                maxSpeed
            );

    }



    void FixedUpdate()
    {
        if(movementLocked)
            return;


        // Keep forward drive responsive while allowing lateral momentum to carry
        // through a turn. Grip gradually brings that sideways motion back in line.
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
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

        Vector3 drivenVelocity = transform.TransformDirection(localVelocity);
        rb.linearVelocity = new Vector3(
            drivenVelocity.x,
            rb.linearVelocity.y,
            drivenVelocity.z
        );


        float speedMagnitude = Mathf.Abs(currentSpeed);
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


        // Scale yaw by the vehicle's actual drive speed instead of allowing
        // full steering authority as soon as it starts moving.
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


        rb.MoveRotation(
            rb.rotation *
            turn
        );
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



    public void SetMobileThrottle(bool accelerating, bool isPressed)
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
        controls.Disable();
        mobileSteerLeft = false;
        mobileSteerRight = false;
        mobileAccelerating = false;
        mobileBraking = false;
        activeSteering = 0f;
    }
}
