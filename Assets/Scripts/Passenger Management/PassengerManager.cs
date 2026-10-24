using UnityEngine;
using System.Collections.Generic;

public class PassengerManager : MonoBehaviour
{
    public List<Transform> seats;

    public List<GameObject> passengersOnBoard =
        new List<GameObject>();


    private Dictionary<Transform, GameObject> occupiedSeats =
        new Dictionary<Transform, GameObject>();


    private HashSet<Transform> reservedSeats =
        new HashSet<Transform>();


    void Start()
    {
        foreach(Transform seat in seats)
        {
            occupiedSeats.Add(seat, null);
        }
    }


    public Transform GetAvailableSeat()
    {
        foreach(Transform seat in seats)
        {
            if(occupiedSeats[seat] == null &&
               !reservedSeats.Contains(seat))
            {
                return seat;
            }
        }

        return null;
    }


    public bool ReserveSeat(Transform seat)
    {
        if(seat == null)
            return false;


        if(occupiedSeats[seat] != null)
            return false;


        if(reservedSeats.Contains(seat))
            return false;


        reservedSeats.Add(seat);

        return true;
    }


    public bool AddPassengerToSeat(
        GameObject passenger,
        Transform seat
    )
    {
        if(seat == null)
            return false;


        reservedSeats.Remove(seat);


        if(occupiedSeats[seat] != null)
        {
            Debug.Log("Seat already occupied!");
            return false;
        }


        passenger.transform.SetParent(seat);

        passenger.transform.localPosition =
            Vector3.zero;

        passenger.transform.localRotation =
            Quaternion.identity;


        occupiedSeats[seat] = passenger;


        passengersOnBoard.Add(passenger);


        PassengerData data =
            passenger.GetComponent<PassengerData>();

        if(data != null)
        {
            data.isOnVehicle = true;
        }


        return true;
    }


    public void DropPassenger(GameObject passenger)
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


        PassengerData data =
            passenger.GetComponent<PassengerData>();

        if(data != null)
        {
            data.isOnVehicle = false;
        }


        passengersOnBoard.Remove(passenger);
    }
}