using UnityEngine;

public class PassengerAI : MonoBehaviour
{
    private Vector3 targetPosition;

    private Transform followTarget;

    private bool hasTarget = false;

    private bool followTransform = false;


    public float walkSpeed = 2f;
    public float runSpeed = 4f;


    [Range(0f,1f)]
    public float runChance = 0.35f;


    private bool isRunning;


    private Animator animator;

    private Collider passengerCollider;



    void Awake()
    {
        animator =
            GetComponentInChildren<Animator>();

        passengerCollider =
            GetComponent<Collider>();
    }



    public void WalkTo(Vector3 position)
    {
        followTransform = false;

        targetPosition = position;

        hasTarget = true;

        EnablePassengerCollision(false);

        SetMovementAnimation(true);
    }

    public void SetTarget(Vector3 position)
    {
        followTransform = false;

        targetPosition = position;

        hasTarget = true;

        EnablePassengerCollision(true);

        SetMovementAnimation(true);
    }



    public void FollowTarget(Transform target)
    {
        followTarget = target;

        followTransform = true;

        hasTarget = true;

        EnablePassengerCollision(false);

        SetMovementAnimation(true);
    }



    public void CancelMovement()
    {
        hasTarget = false;

        followTarget = null;

        followTransform = false;

        SetMovementAnimation(false);
    }



    public bool HasReachedTarget()
    {
        return !hasTarget;
    }




    void Update()
    {
        if(!hasTarget)
            return;



        Vector3 target;


        if(followTransform && followTarget != null)
        {
            target = followTarget.position;
        }
        else
        {
            target = targetPosition;
        }



        Vector3 direction =
            (target - transform.position).normalized;



        if(direction.sqrMagnitude > 0.01f)
        {
            Quaternion rotation =
                Quaternion.LookRotation(direction);


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    rotation,
                    Time.deltaTime * 8f
                );
        }



        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                (isRunning ? runSpeed : walkSpeed)
                *
                Time.deltaTime
            );



        if(Vector3.Distance(transform.position,target) < 0.1f)
        {
            hasTarget = false;

            SetMovementAnimation(false);
        }
    }





    public void Sit()
    {
        SetMovementAnimation(false);

        if(animator != null)
        {
            animator.SetBool(
                "IsSitting",
                true
            );
        }
    }





    public void StandUp()
    {
        if(animator != null)
        {
            animator.SetBool(
                "IsSitting",
                false
            );
        }
    }





    public void EnablePassengerCollision(bool enabled)
    {
        if(passengerCollider != null)
        {
            passengerCollider.enabled = enabled;
        }
    }





    void SetMovementAnimation(bool moving)
    {
        isRunning =
            moving &&
            Random.value < runChance;


        if(animator != null)
        {
            animator.SetBool(
                "IsWalking",
                moving && !isRunning
            );


            animator.SetBool(
                "IsRunning",
                isRunning
            );
        }
    }
}