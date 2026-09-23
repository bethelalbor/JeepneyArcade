using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 4f;
    public float acceleration = 2f;
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


        float throttle = moveInput.y;
        float rawTurn = moveInput.x;


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



    void OnEnable()
    {
        controls.Enable();
    }



    void OnDisable()
    {
        controls.Disable();
    }
}