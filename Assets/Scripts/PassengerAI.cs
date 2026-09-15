using UnityEngine;

public class PassengerAI : MonoBehaviour
{
    private Vector3 target;
    private bool hasTarget = false;

    public float walkSpeed = 2f;

    private Animator animator;
    private Collider passengerCollider;


    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        passengerCollider = GetComponent<Collider>();
    }


    public void SetTarget(Vector3 position)
    {
        target = position;
        hasTarget = true;

        EnablePassengerCollision(true);
        SetWalking(true);
    }


    public void WalkTo(Vector3 position)
    {
        target = position;
        hasTarget = true;

        EnablePassengerCollision(false);

        SetWalking(true);
    }


    public bool HasReachedTarget()
    {
        return !hasTarget;
    }


    public void Sit()
    {
        SetWalking(false);

        if(animator != null)
        {
            animator.SetBool("IsSitting", true);
        }
    }


    public void StandUp()
    {
        if(animator != null)
        {
            animator.SetBool("IsSitting", false);
        }
    }


    void Update()
    {
        if(!hasTarget)
            return;


        Vector3 direction =
            (target - transform.position).normalized;


        // Rotate passenger toward walking direction
        if(direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * 8f
                );
        }


        // Move passenger
        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                walkSpeed * Time.deltaTime
            );


        if(Vector3.Distance(transform.position, target) < 0.1f)
        {
            hasTarget = false;

            SetWalking(false);
        }
    }


    public void EnablePassengerCollision(bool enabled)
    {
        if(passengerCollider != null)
        {
            passengerCollider.enabled = enabled;
        }
    }


    void SetWalking(bool walking)
    {
        if(animator != null)
        {
            animator.SetBool("IsWalking", walking);
        }
    }
}