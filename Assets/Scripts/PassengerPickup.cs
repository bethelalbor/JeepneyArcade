using UnityEngine;
using System.Collections;

public class PassengerPickup : MonoBehaviour
{
    public GameObject passenger;
    public Transform passengerSeat;

    private Rigidbody vehicleRb;
    private bool playerInside = false;
    private bool passengerPickedUp = false;


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

        passenger.transform.SetParent(passengerSeat);

        passenger.transform.localPosition = Vector3.zero;

        passenger.transform.localRotation = Quaternion.identity;
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            vehicleRb = other.GetComponent<Rigidbody>();

            Debug.Log("Passenger zone entered by: " + other.name);
        }
    }


    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            vehicleRb = null;
        }
    }
}