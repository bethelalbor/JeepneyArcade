using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 6f;
    public float acceleration = 2f;
    public float deceleration = 3f;
    public float rotationSpeed = 50f;

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
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        float throttle = moveInput.y;
        float rawTurn = moveInput.x;

        // Apply deadzone so tiny stick drift doesn't register as steering input
        turnInput = Mathf.Abs(rawTurn) < inputDeadzone ? 0f : rawTurn;

        if (throttle != 0)
        {
            currentSpeed += throttle * acceleration * Time.deltaTime;
        }
        else
        {
            currentSpeed = Mathf.Lerp(
                currentSpeed,
                0f,
                deceleration * Time.deltaTime
            );
        }

        currentSpeed = Mathf.Clamp(
            currentSpeed,
            -maxSpeed / 2f,
            maxSpeed
        );

        // Move the "steering wheel" toward the target angle at equal speed both ways
        float targetSteerAngle = turnInput * maxSteerAngle;
        steerAngle = Mathf.MoveTowards(
            steerAngle,
            targetSteerAngle,
            steeringSensitivity * Time.deltaTime
        );
    }

    void FixedUpdate()
    {
        // Apply forward/backward movement
        Vector3 horizontalVelocity = transform.forward * currentSpeed;
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);

        // Only steer if the vehicle is actually moving (totoong kotse logic)
        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            float speedFactor = Mathf.Sign(currentSpeed);
            float normalizedSteer = steerAngle / maxSteerAngle; // -1 to 1, symmetric

            Quaternion turn = Quaternion.Euler(
                Vector3.up * normalizedSteer * speedFactor * rotationSpeed * Time.fixedDeltaTime
            );
            rb.MoveRotation(rb.rotation * turn);
        }
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }
}