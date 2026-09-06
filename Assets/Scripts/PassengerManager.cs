using UnityEngine;
using System.Collections.Generic;

public class PassengerManager : MonoBehaviour
{
    public List<Transform> seats;

    private Dictionary<Transform, GameObject> occupiedSeats 
        = new Dictionary<Transform, GameObject>();


    void Start()
    {
        foreach (Transform seat in seats)
        {
            occupiedSeats.Add(seat, null);
        }
    }


    public Transform GetAvailableSeat()
    {
        foreach (Transform seat in seats)
        {
            if (occupiedSeats[seat] == null)
            {
                return seat;
            }
        }

        return null;
    }


    public bool AddPassenger(GameObject passenger)
    {
        Transform seat = GetAvailableSeat();

        if(seat == null)
        {
            Debug.Log("NO SEAT AVAILABLE");
            return false;
        }

        Debug.Log("Assigning passenger to: " + seat.name);

        passenger.transform.SetParent(seat);
        passenger.transform.localPosition = Vector3.zero;
        passenger.transform.localRotation = Quaternion.identity;

        occupiedSeats[seat] = passenger;

        return true;
    }


    public void RemovePassenger(GameObject passenger)
    {
        foreach(var seat in occupiedSeats.Keys)
        {
            if(occupiedSeats[seat] == passenger)
            {
                occupiedSeats[seat] = null;
                break;
            }
        }

        passenger.transform.SetParent(null);
    }
}