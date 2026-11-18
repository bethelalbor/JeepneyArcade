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
    private bool walkingToQueue;

    [Tooltip("Cull off-screen idle animation after reaching the pickup queue. Movement and boarding keep animating.")]
    public bool cullWaitingAnimation = true;

    private Collider passengerCollider;



    void Awake()
    {
        animator =
            GetComponentInChildren<Animator>();

        passengerCollider =
            GetComponent<Collider>();
    }



    public void WaitAtQueue()
    {
        if(hasTarget)
            return;

        walkingToQueue = true;
        SetMovementAnimation(false);
    }

    public void WalkTo(Vector3 position)
    {
        walkingToQueue = false;
        followTransform = false;

        targetPosition = position;

        hasTarget = true;

        EnablePassengerCollision(false);

        SetMovementAnimation(true);
    }

    public void SetTarget(Vector3 position)
    {
        walkingToQueue = true;
        followTransform = false;

        targetPosition = position;

        hasTarget = true;

        EnablePassengerCollision(true);

        SetMovementAnimation(true);
    }



    public void FollowTarget(Transform target)
    {
        walkingToQueue = false;
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
        walkingToQueue = false;
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
            // Only queue-idle animation can stop off-screen. AI transforms and
            // coroutine completion never depend on renderer visibility.
            animator.cullingMode = cullWaitingAnimation && walkingToQueue && !moving && !animator.applyRootMotion
                ? AnimatorCullingMode.CullCompletely
                : AnimatorCullingMode.AlwaysAnimate;

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