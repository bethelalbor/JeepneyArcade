using UnityEngine;
using System.Collections;

public class PassengerPickup : MonoBehaviour
{
    public GameObject passenger;

    private Rigidbody vehicleRb;
    private PassengerManager passengerManager;

    private bool playerInside = false;
    private bool passengerPickedUp = false;


    void Start()
    {
        passengerManager = FindFirstObjectByType<PassengerManager>();
    }


    void Update()
    {
        if (playerInside && !passengerPickedUp)
        {
            if (vehicleRb != null && vehicleRb.linearVelocity.magnitude < 0.1f)
            {
                StartCoroutine(PickupPassenger());
            }
        }
    }


    IEnumerator PickupPassenger()
    {
        passengerPickedUp = true;

        yield return new WaitForSeconds(2f);

        bool success = passengerManager.AddPassenger(passenger);

        if(success)
        {
            Debug.Log("Passenger successfully seated");
        }
        else
        {
            Debug.Log("No available passenger seat");
        }
    }


    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Vector3 toPassenger = (transform.position - other.transform.position).normalized;
        float side = Vector3.Dot(other.transform.right, toPassenger);

        if (side < 0f)
        {
            // passenger is on the wrong side relative to travel direction — ignore
            return;
        }

        playerInside = true;
        vehicleRb = other.GetComponent<Rigidbody>();
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