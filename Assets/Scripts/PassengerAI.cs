using UnityEngine;

public class PassengerAI : MonoBehaviour
{
    private Vector3 target;
    private bool hasTarget = false;

    public float walkSpeed = 2f;


    public void SetTarget(Vector3 position)
    {
        target = position;
        hasTarget = true;
    }


    void Update()
    {
        if (!hasTarget)
            return;


        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                walkSpeed * Time.deltaTime
            );


        if (Vector3.Distance(
            transform.position,
            target
        ) < 0.1f)
        {
            hasTarget = false;
        }
    }
}