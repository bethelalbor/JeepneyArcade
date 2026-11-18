using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PassengerDropOff : MonoBehaviour
{
    public DestinationPoint destination;

    private readonly HashSet<GameObject> passengersDroppingOff = new HashSet<GameObject>();


    private void OnTriggerEnter(Collider other)
    {
        TryDropOff(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDropOff(other);
    }

    private void TryDropOff(Collider other)
    {
        Rigidbody vehicle = other.attachedRigidbody;
        if(vehicle == null || !vehicle.CompareTag("Player") || destination == null)
            return;


        VehiclePassengerHandler handler =
            vehicle.GetComponent<VehiclePassengerHandler>();


        if(handler == null || handler.GetPassengerDoor() == null)
            return;


        PassengerManager manager =
            vehicle.GetComponent<PassengerManager>();


        if(manager != null)
        {
            CheckPassengers(manager, handler.GetPassengerDoor());
        }
    }



    void CheckPassengers(PassengerManager manager, Transform passengerDoorPoint)
    {
        if(manager.passengersOnBoard.Count == 0)
            return;

        List<GameObject> passengersToDrop =
            new List<GameObject>();


        foreach(GameObject passenger in manager.passengersOnBoard)
        {
            if(passenger == null || passengersDroppingOff.Contains(passenger))
                continue;

            PassengerData data =
                passenger.GetComponent<PassengerData>();


            if(data != null && data.isOnVehicle && data.destination == destination)
            {
                passengersToDrop.Add(passenger);
            }
        }



        foreach(GameObject passenger in passengersToDrop)
        {
            if(!passengersDroppingOff.Add(passenger))
                continue;

            StartCoroutine(
                DropPassengerRoutine(
                    manager,
                    passenger,
                    passengerDoorPoint
                )
            );
        }
    }




    IEnumerator DropPassengerRoutine(
        PassengerManager manager,
        GameObject passenger,
        Transform passengerDoorPoint
    )
    {
        try
        {
            PassengerData data =
                passenger.GetComponent<PassengerData>();

            // Remove the passenger before rewarding delivery, so another collider or
            // overlapping destination trigger cannot process the same fare again.
            if(data == null || !data.isOnVehicle || data.destination != destination ||
               !manager.passengersOnBoard.Contains(passenger))
                yield break;

            manager.DropPassenger(passenger);

            if(EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddMoney(
                    data.fareAmount
                );
            }


            // SCORE: successful delivery
            if(ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(10);


                if(VehicleFeedbackText.Instance != null)
                {
                    VehicleFeedbackText.Instance.Show("+10 points");
                }
            }



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



            if(ai != null && destination != null && destination.walkPoint != null)
            {
                ai.WalkTo(
                    destination.walkPoint.position
                );


                while(ai != null && !ai.HasReachedTarget())
                {
                    yield return null;
                }
            }


            Destroy(passenger);
        }
        finally
        {
            passengersDroppingOff.Remove(passenger);
        }
    }
}
