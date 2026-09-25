using UnityEngine;
using System.Collections;

public class PassengerPickup : MonoBehaviour
{
    public GameObject passenger;


    private Rigidbody vehicleRb;

    private PassengerManager passengerManager;

    private VehiclePassengerHandler vehicleHandler;


    private bool playerInside = false;

    private bool passengerPickedUp = false;



    void Start()
    {
        passengerManager =
            FindFirstObjectByType<PassengerManager>();
    }



    void Update()
    {
        if(playerInside && !passengerPickedUp)
        {
            if(vehicleRb != null &&
               vehicleRb.linearVelocity.magnitude < 0.1f)
            {
                StartCoroutine(PickupPassenger());
            }
        }
    }




    IEnumerator PickupPassenger()
    {
        passengerPickedUp = true;



        Transform availableSeat =
            passengerManager.GetAvailableSeat();



        if(availableSeat == null)
        {
            Debug.Log("No available seats");

            passengerPickedUp = false;

            yield break;
        }



        passengerManager.ReserveSeat(availableSeat);



        yield return new WaitForSeconds(1f);



        PassengerAI ai =
            passenger.GetComponent<PassengerAI>();


        if(ai == null)
        {
            Debug.LogError("PassengerAI missing");
            yield break;
        }



        if(vehicleHandler == null)
        {
            Debug.LogError("VehiclePassengerHandler missing");
            yield break;
        }



        Vector3 passengerPosition =
            passenger.transform.position;


        Vector3 doorPosition =
            vehicleHandler.GetPassengerDoor().position;



        Vector3 rightDirection =
            passenger.transform.right;


        rightDirection.y = 0f;

        rightDirection.Normalize();



        Vector3 doorOffset =
            doorPosition - passengerPosition;


        doorOffset.y = 0f;



        Vector3 sideWaypoint =
            passengerPosition +
            rightDirection *
            Vector3.Dot(
                doorOffset,
                rightDirection
            );



        ai.WalkTo(sideWaypoint);


        while(!ai.HasReachedTarget())
        {
            yield return null;
        }



        ai.WalkTo(doorPosition);



        while(!ai.HasReachedTarget())
        {
            yield return null;
        }



        ai.Sit();



        yield return new WaitForSeconds(0.5f);



        passengerManager.AddPassengerToSeat(
            passenger,
            availableSeat
        );



        // SCORE: successful pickup
        if(ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(25);
        }



        // SCORE: full passenger capacity
        if(passengerManager.passengersOnBoard.Count ==
           passengerManager.seats.Count)
        {
            if(ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(50);
            }
        }



        Debug.Log("Passenger seated");
    }





    void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player"))
            return;


        playerInside = true;


        vehicleRb =
            other.GetComponent<Rigidbody>();


        vehicleHandler =
            other.GetComponent<VehiclePassengerHandler>();
    }





    void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerInside = false;

            vehicleRb = null;
        }
    }
}