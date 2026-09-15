using UnityEngine;
using System.Collections.Generic;

public class PassengerSpawner : MonoBehaviour
{
    public List<GameObject> passengerPrefabs;

    public List<PassengerSpawnPoint> passengerLocations;

    public float spawnInterval = 10f;


    void Start()
    {
        InvokeRepeating(
            nameof(SpawnPassenger),
            3f,
            spawnInterval
        );
    }


    void SpawnPassenger()
    {
        if(passengerLocations.Count == 0)
            return;


        PassengerSpawnPoint location =
            passengerLocations[
                Random.Range(
                    0,
                    passengerLocations.Count
                )
            ];


        GameObject selectedPrefab =
            passengerPrefabs[
                Random.Range(
                    0,
                    passengerPrefabs.Count
                )
            ];


        GameObject passenger =
            Instantiate(
                selectedPrefab,
                location.spawnPoint.position,
                location.spawnPoint.rotation
            );


        PassengerData data =
            passenger.GetComponent<PassengerData>();


        if(data != null)
        {
            data.destination =
                location.possibleDestinations[
                    Random.Range(
                        0,
                        location.possibleDestinations.Length
                    )
                ];
        }


        PassengerAI ai =
            passenger.GetComponent<PassengerAI>();


        if(ai != null)
        {
            Vector3 queuePosition =
            location.pickupPoint.GetQueuePosition();


            ai.SetTarget(queuePosition);


            location.pickupPoint.RegisterPassenger(
                passenger.transform
            );
        }
    }
}