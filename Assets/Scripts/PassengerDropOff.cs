using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PassengerDropOff : MonoBehaviour
{
    public DestinationPoint destination;

    private Transform passengerDoorPoint;


    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player"))
            return;


        // Get jeepney door location from Player prefab
        VehiclePassengerHandler handler =
            other.GetComponent<VehiclePassengerHandler>();


        if(handler != null)
        {
            passengerDoorPoint =
                handler.GetPassengerDoor();
        }


        PassengerManager manager =
            other.GetComponentInParent<PassengerManager>();


        if(manager != null)
        {
            CheckPassengers(manager);
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
            StartCoroutine(
                DropPassengerRoutine(
                    manager,
                    passenger
                )
            );
        }
    }



    IEnumerator DropPassengerRoutine(
        PassengerManager manager,
        GameObject passenger
    )
    {
        // Remove passenger from seat
        manager.DropPassenger(passenger);


        PassengerAI ai =
            passenger.GetComponent<PassengerAI>();


        // Move passenger instantly outside jeepney
        if(passengerDoorPoint != null)
        {
            passenger.transform.position =
                passengerDoorPoint.position;
        }


        if(ai != null)
        {
            ai.StandUp();
        }


        yield return new WaitForSeconds(0.3f);



        // Walk away from jeepney
        if(ai != null && destination.walkPoint != null)
        {
            ai.WalkTo(
                destination.walkPoint.position
            );


            while(!ai.HasReachedTarget())
            {
                yield return null;
            }
        }


        Destroy(passenger);
    }
}