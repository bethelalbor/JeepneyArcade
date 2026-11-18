using UnityEngine;

public class PassengerData : MonoBehaviour
{
    public DestinationPoint destination;
    public int fareAmount = 10;

    public bool isOnVehicle = false;

    [Tooltip("Pickup queue for a passenger already placed in the scene. Spawned passengers are registered by the spawner.")]
    public PassengerQueuePoint initialPickupQueue;

    public PassengerQueuePoint PickupQueue { get; internal set; }

    private void Start()
    {
        if(isOnVehicle || PickupQueue != null || initialPickupQueue == null)
            return;

        if(!initialPickupQueue.RegisterPassenger(transform))
        {
            Debug.LogWarning("Initial passenger could not join its pickup queue; check the waiting capacity.", this);
            return;
        }

        GetComponent<PassengerAI>()?.WaitAtQueue();
    }

    public void LeavePickupQueue()
    {
        PassengerQueuePoint queue = PickupQueue;
        PickupQueue = null;
        if(queue != null)
            queue.RemovePassenger(transform);
    }

    private void OnDestroy()
    {
        LeavePickupQueue();
    }
}