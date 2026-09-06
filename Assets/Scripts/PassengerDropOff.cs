using UnityEngine;
using System.Collections.Generic;

public class PassengerDropOff : MonoBehaviour
{
    public DestinationPoint destination;


    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            PassengerManager manager =
            other.GetComponentInParent<PassengerManager>();

            if(manager != null)
            {
                CheckPassengers(manager);
            }
        }
    }


    void CheckPassengers(PassengerManager manager)
    {
        List<GameObject> passengersToDrop =
        new List<GameObject>();


        foreach(GameObject passenger in manager.passengersOnBoard)
        {
            PassengerData data =
            passenger.GetComponent<PassengerData>();

            if(data != null && data.destination == destination)
            {
                passengersToDrop.Add(passenger);
            }
        }


        foreach(GameObject passenger in passengersToDrop)
        {
            manager.DropPassenger(passenger);
        }
    }
}