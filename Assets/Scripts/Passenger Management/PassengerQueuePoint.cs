using UnityEngine;
using System.Collections.Generic;


public class PassengerQueuePoint : MonoBehaviour
{
    public float spacing = 1.2f;

    private List<Transform> passengers =
        new List<Transform>();


    public Vector3 GetQueuePosition()
    {
        int index = passengers.Count;

        Vector3 offset =
            transform.right *
            ((index - 1) * spacing);


        return transform.position + offset;
    }


    public void RegisterPassenger(Transform passenger)
    {
        passengers.Add(passenger);
    }


    public void RemovePassenger(Transform passenger)
    {
        passengers.Remove(passenger);
    }
}