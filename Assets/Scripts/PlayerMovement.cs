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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        float moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");

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
    }

    void FixedUpdate()
    {
        Vector3 horizontalVelocity = transform.forward * currentSpeed;
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);

        Quaternion turn = Quaternion.Euler(Vector3.up * turnInput * rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(rb.rotation * turn);
    }
}
