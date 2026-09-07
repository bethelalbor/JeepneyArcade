using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float maxSpeed = 10f;
    public float acceleration = 5f;
    public float deceleration = 3f;
    public float rotationSpeed = 100f;

    private float currentSpeed = 0f;
    private float turnInput = 0f;
    private Rigidbody rb;

    private PlayerControls controls;
    private Vector2 moveInput;

    [Header("Input Settings")]
    public float inputDeadzone = 0.15f;
    public float steeringSensitivity = 1f;
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
        turnInput = moveInput.x;


        if(throttle != 0)
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
    }

    void FixedUpdate()
    {
        Vector3 horizontalVelocity = transform.forward * currentSpeed;
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);

        Quaternion turn = Quaternion.Euler(Vector3.up * turnInput * rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(rb.rotation * turn);
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

