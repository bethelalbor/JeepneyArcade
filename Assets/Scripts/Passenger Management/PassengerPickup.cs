using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PassengerPickup : MonoBehaviour
{
    private const float PerfectPickupDistance = 5f;

    public GameObject passenger;


    private Rigidbody vehicleRb;

    private PassengerManager passengerManager;

    private VehiclePassengerHandler vehicleHandler;


    private readonly HashSet<Collider> vehicleColliders = new HashSet<Collider>();
    private PassengerManager reservationManager;
    private Transform reservedSeat;
    private PassengerAI boardingAI;
    private bool boardingInProgress;

    private bool passengerPickedUp = false;



    void Update()
    {
        vehicleColliders.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

        if(vehicleColliders.Count > 0 && !passengerPickedUp)
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
        PassengerAI ai = passenger != null ? passenger.GetComponent<PassengerAI>() : null;
        Transform doorTarget = vehicleHandler != null ? vehicleHandler.GetPassengerDoor() : null;
        PassengerManager manager = passengerManager;
        if(ai == null || !ai.isActiveAndEnabled || manager == null || doorTarget == null)
            yield break;

        Transform availableSeat =
            manager.GetAvailableSeat();



        if(availableSeat == null || !manager.ReserveSeat(availableSeat))
        {
            yield break;
        }

        passengerPickedUp = true;
        boardingInProgress = true;
        reservationManager = manager;
        reservedSeat = availableSeat;
        boardingAI = ai;
        bool seated = false;
        try
        {
            yield return new WaitForSeconds(1f);

            if(vehicleColliders.Count == 0 || vehicleRb == null ||
               vehicleRb.linearVelocity.magnitude >= 0.1f ||
               ai == null || !ai.isActiveAndEnabled || manager == null || doorTarget == null)
                yield break;



            float passengerTravelDistance = 0f;

            Vector3 lastPassengerPosition =
                passenger.transform.position;


            Vector3 passengerPosition =
                passenger.transform.position;


            Vector3 doorPosition =
                doorTarget.position;



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

                if(ai == null || !ai.isActiveAndEnabled || manager == null || doorTarget == null)
                    yield break;

                passengerTravelDistance +=
                    Vector3.Distance(
                        lastPassengerPosition,
                        passenger.transform.position
                    );

                lastPassengerPosition =
                    passenger.transform.position;
            }



            ai.FollowTarget(doorTarget);


            while(!ai.HasReachedTarget())
            {
                yield return null;

                if(ai == null || !ai.isActiveAndEnabled || manager == null || doorTarget == null)
                    yield break;

                passengerTravelDistance +=
                    Vector3.Distance(
                        lastPassengerPosition,
                        passenger.transform.position
                    );

                lastPassengerPosition =
                    passenger.transform.position;
            }



            ai.Sit();



            yield return new WaitForSeconds(0.5f);



            if(ai == null || !ai.isActiveAndEnabled || manager == null || doorTarget == null ||
               !manager.AddPassengerToSeat(passenger, availableSeat))
                yield break;

            seated = true;
            boardingInProgress = false;
            reservedSeat = null;
            reservationManager = null;
            boardingAI = null;



            // SCORE: successful pickup or perfect pickup
            if(ScoreManager.Instance != null)
            {
                bool isPerfectPickup =
                    passengerTravelDistance < PerfectPickupDistance;


                int pickupScore =
                    ScoreManager.Instance.GetPassengerPickupScore(
                        isPerfectPickup
                    );

                ScoreManager.Instance.AddScore(pickupScore);


                string feedbackMessage =
                    "+" + pickupScore + " points";


                if(isPerfectPickup)
                {
                    feedbackMessage = "Perfect Pickup!";


                    if(ScoreManager.Instance.PerfectPickupStreak > 1)
                    {
                        feedbackMessage +=
                            " X" +
                            ScoreManager.Instance.PerfectPickupStreak;
                    }


                    feedbackMessage +=
                        "\n+" + pickupScore + " points";
                }


                if(VehicleFeedbackText.Instance != null)
                {
                    VehicleFeedbackText.Instance.Show(feedbackMessage);
                }
            }



            // SCORE: full passenger capacity
            if(manager.passengersOnBoard.Count ==
               manager.seats.Count)
            {
                if(ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddScore(100);
                }
            }



            Debug.Log("Passenger seated");
        }
        finally
        {
            if(!seated)
                CancelBoarding();
        }
    }





    void OnTriggerEnter(Collider other)
    {
        TrackVehicleCollider(other);
    }

    void OnTriggerStay(Collider other)
    {
        TrackVehicleCollider(other);
    }

    void TrackVehicleCollider(Collider other)
    {
        if(passengerPickedUp && !boardingInProgress)
            return;

        Rigidbody candidate = other.attachedRigidbody;
        if(candidate == null || !candidate.CompareTag("Player"))
            return;

        if((vehicleColliders.Count > 0 || boardingInProgress) && candidate != vehicleRb)
            return;

        vehicleRb = candidate;
        vehicleHandler = candidate.GetComponent<VehiclePassengerHandler>();
        passengerManager = candidate.GetComponent<PassengerManager>();
        vehicleColliders.Add(other);
    }





    void OnTriggerExit(Collider other)
    {
        vehicleColliders.Remove(other);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        CancelBoarding();
        vehicleColliders.Clear();
        vehicleRb = null;
        vehicleHandler = null;
        passengerManager = null;
    }

    void CancelBoarding()
    {
        if(reservationManager != null)
            reservationManager.ReleaseSeat(reservedSeat);

        if(boardingAI != null)
        {
            boardingAI.CancelMovement();
            boardingAI.StandUp();
            boardingAI.EnablePassengerCollision(true);
        }

        if(boardingInProgress)
            passengerPickedUp = false;

        boardingInProgress = false;
        reservationManager = null;
        reservedSeat = null;
        boardingAI = null;
    }
}
