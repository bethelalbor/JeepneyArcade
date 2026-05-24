using UnityEngine;
using System.Collections;

public class PassengerPickup : MonoBehaviour
{
    public GameObject passenger;

    private bool playerInside = false;
    private bool passengerPickedUp = false;

    void Update()
    {
        if (playerInside && !passengerPickedUp)
        {
            Rigidbody rb = GetComponent<Rigidbody>();

            // Check if vehicle is almost stopped
            if (rb.linearVelocity.magnitude < 0.1f)
            {
                StartCoroutine(PickupPassenger());
            }
        }
    }

    IEnumerator PickupPassenger()
    {
        passengerPickedUp = true;

        // Wait 2 seconds
        yield return new WaitForSeconds(2f);

        // Move passenger into vehicle
        passenger.transform.SetParent(transform);

        passenger.transform.localPosition = new Vector3(0f, 1f, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PickupZone"))
        {
            playerInside = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PickupZone"))
        {
            playerInside = false;
        }
    }
}