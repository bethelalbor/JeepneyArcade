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
        passengerManager = FindFirstObjectByType<PassengerManager>();
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


        // CHECK SEAT BEFORE PASSENGER MOVES
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


        // Walk to jeepney door
        ai.WalkTo(
            vehicleHandler.GetPassengerDoor().position
        );


        while(!ai.HasReachedTarget())
        {
            yield return null;
        }


        // Play sitting animation
        ai.Sit();


        yield return new WaitForSeconds(0.5f);


        // Assign already confirmed seat
        passengerManager.AddPassengerToSeat(
            passenger,
            availableSeat
        );


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