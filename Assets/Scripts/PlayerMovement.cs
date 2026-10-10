using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 4f;
    public float acceleration = 3f;
    public float deceleration = 3f;
    public float rotationSpeed = 25f;


    [Header("Input Settings")]
    public float inputDeadzone = 0.15f;
    public float steeringSensitivity = 3f;
    public float maxSteerAngle = 30f;


    private float currentSpeed = 0f;
    private float turnInput = 0f;
    private float steerAngle = 0f;


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
            currentSpeed +=
                throttle *
                acceleration *
                Time.deltaTime;
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


        float targetSteerAngle =
            turnInput * maxSteerAngle;


        steerAngle =
            Mathf.MoveTowards(
                steerAngle,
                targetSteerAngle,
                steeringSensitivity *
                Time.deltaTime
            );
    }



    void FixedUpdate()
    {
        if(movementLocked)
            return;


        Vector3 horizontalVelocity =
            transform.forward *
            currentSpeed;


        rb.linearVelocity =
            new Vector3(
                horizontalVelocity.x,
                rb.linearVelocity.y,
                horizontalVelocity.z
            );


        if(Mathf.Abs(currentSpeed) > 0.1f)
        {
            float speedFactor =
                Mathf.Sign(currentSpeed);


            float normalizedSteer =
                steerAngle / maxSteerAngle;


            Quaternion turn =
                Quaternion.Euler(
                    Vector3.up *
                    normalizedSteer *
                    speedFactor *
                    rotationSpeed *
                    Time.fixedDeltaTime
                );


            rb.MoveRotation(
                rb.rotation *
                turn
            );
        }
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
        steerAngle = 0f;


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
    }
}
