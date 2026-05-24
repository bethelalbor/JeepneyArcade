using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float maxSpeed = 10f;
    public float acceleration = 5f;
    public float deceleration = 3f;
    public float rotationSpeed = 100f;

    private float currentSpeed = 0f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        // Acceleration
        if (moveInput != 0)
        {
            currentSpeed += moveInput * acceleration * Time.deltaTime;
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed / 2f, maxSpeed);

        // Rotate
        transform.Rotate(Vector3.up * turnInput * rotationSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = transform.forward * currentSpeed;
    }
}