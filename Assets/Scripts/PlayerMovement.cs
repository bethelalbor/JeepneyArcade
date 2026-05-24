using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float maxSpeed = 10f;
    public float acceleration = 5f;
    public float deceleration = 3f;
    public float rotationSpeed = 100f;

    private float currentSpeed = 0f;

    void Update()
    {
        // Get player input
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        // Accelerate
        if (moveInput != 0)
        {
            currentSpeed += moveInput * acceleration * Time.deltaTime;
        }
        else
        {
            // Gradually slow down
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        // Clamp speed
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed / 2f, maxSpeed);

        // Move vehicle
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        // Rotate vehicle
        transform.Rotate(Vector3.up * turnInput * rotationSpeed * Time.deltaTime);
    }
}