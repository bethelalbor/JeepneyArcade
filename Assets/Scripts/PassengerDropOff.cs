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
        PassengerData data =
            passenger.GetComponent<PassengerData>();


        if(data != null)
        {
            EconomyManager.Instance.AddMoney(
                data.fareAmount
            );
        }


        // SCORE: successful delivery
        if(ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(10);
        }



        manager.DropPassenger(passenger);



        PassengerAI ai =
            passenger.GetComponent<PassengerAI>();


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