using UnityEngine;
using System.Collections.Generic;


public class PassengerQueuePoint : MonoBehaviour
{
    public float spacing = 1.2f;

    [Tooltip("Maximum waiting passengers, including those walking to the queue or boarding until seated.")]
    [Min(1)] public int maxWaitingPassengers = 5;

    private List<Transform> passengers =
        new List<Transform>();

    public int WaitingCount
    {
        get
        {
            RemoveDestroyedPassengers();
            int count = 0;
            foreach(Transform passenger in passengers)
                if(passenger != null)
                    count++;
            return count;
        }
    }

    public bool HasCapacity => WaitingCount < Mathf.Max(1, maxWaitingPassengers);

    private int GetAvailableSlot()
    {
        RemoveDestroyedPassengers();
        int index = passengers.FindIndex(passenger => passenger == null);
        return index < 0 ? passengers.Count : index;
    }

    private void RemoveDestroyedPassengers()
    {
        // Keep holes so existing passengers retain their physical queue positions.
        for(int i = 0; i < passengers.Count; i++)
            if(passengers[i] == null)
                passengers[i] = null;

        while(passengers.Count > 0 && passengers[passengers.Count - 1] == null)
            passengers.RemoveAt(passengers.Count - 1);
    }


    public Vector3 GetQueuePosition()
    {
        int index = GetAvailableSlot();

        Vector3 offset =
            transform.right *
            ((index - 1) * spacing);


        return transform.position + offset;
    }


    public bool RegisterPassenger(Transform passenger)
    {
        if(passenger == null)
            return false;

        PassengerData data = passenger.GetComponent<PassengerData>();
        if(data == null || data.isOnVehicle)
            return false;
        if(passengers.Contains(passenger))
            return true;
        if(!HasCapacity)
            return false;

        data.LeavePickupQueue();
        int slot = GetAvailableSlot();
        if(slot == passengers.Count)
            passengers.Add(passenger);
        else
            passengers[slot] = passenger;
        data.PickupQueue = this;
        return true;
    }


    public void RemovePassenger(Transform passenger)
    {
        int index = passengers.IndexOf(passenger);
        if(index >= 0)
            passengers[index] = null;

        if(passenger != null)
        {
            PassengerData data = passenger.GetComponent<PassengerData>();
            if(data != null && data.PickupQueue == this)
                data.PickupQueue = null;
        }
        RemoveDestroyedPassengers();
    }
}