using UnityEngine;
using System.Collections.Generic;

public class PassengerSpawner : MonoBehaviour
{
    public List<GameObject> passengerPrefabs;

    public List<PassengerSpawnPoint> passengerLocations;

    [Min(0.1f)] public float spawnInterval = 10f;

    [Tooltip("Player jeepney. If unassigned, resolved by its Player tag on a spawn tick.")]
    public Transform player;

    [Tooltip("Maximum distance from the player to a pickup point at which new passengers can spawn.")]
    [Min(0f)] public float spawnRange = 100f;

    private readonly List<PassengerSpawnPoint> eligibleLocations = new List<PassengerSpawnPoint>();


    void Start()
    {
        InvokeRepeating(
            nameof(SpawnPassenger),
            3f,
            Mathf.Max(0.1f, spawnInterval)
        );
    }


    void SpawnPassenger()
    {
        if(!isActiveAndEnabled || passengerLocations == null || passengerLocations.Count == 0 ||
           passengerPrefabs == null || passengerPrefabs.Count == 0)
            return;

        if(player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if(playerObject != null)
                player = playerObject.transform;
        }

        if(player == null || !player.gameObject.activeInHierarchy)
            return;

        // One proximity/capacity check per spawn tick, not per frame.
        eligibleLocations.Clear();
        float rangeSquared = Mathf.Max(0f, spawnRange) * Mathf.Max(0f, spawnRange);
        foreach(PassengerSpawnPoint candidate in passengerLocations)
        {
            if(candidate == null || !candidate.isActiveAndEnabled || candidate.spawnPoint == null ||
               candidate.pickupPoint == null || !candidate.pickupPoint.isActiveAndEnabled ||
               candidate.possibleDestinations == null || candidate.possibleDestinations.Length == 0)
                continue;

            if((candidate.pickupPoint.transform.position - player.position).sqrMagnitude <= rangeSquared &&
               candidate.pickupPoint.HasCapacity)
                eligibleLocations.Add(candidate);
        }

        if(eligibleLocations.Count == 0)
            return;

        PassengerSpawnPoint location =
            eligibleLocations[
                Random.Range(
                    0,
                    eligibleLocations.Count
                )
            ];

        GameObject selectedPrefab =
            passengerPrefabs[
                Random.Range(
                    0,
                    passengerPrefabs.Count
                )
            ];

        if(selectedPrefab == null || selectedPrefab.GetComponent<PassengerData>() == null ||
           selectedPrefab.GetComponent<PassengerAI>() == null)
            return;

        DestinationPoint destination = location.possibleDestinations[
            Random.Range(0, location.possibleDestinations.Length)];
        if(destination == null)
            return;

        Vector3 queuePosition = location.pickupPoint.GetQueuePosition();

        GameObject passenger =
            Instantiate(
                selectedPrefab,
                location.spawnPoint.position,
                location.spawnPoint.rotation
            );


        PassengerData data =
            passenger.GetComponent<PassengerData>();


        if(!location.pickupPoint.RegisterPassenger(passenger.transform))
        {
            Destroy(passenger);
            return;
        }

        data.destination = destination;


        PassengerAI ai =
            passenger.GetComponent<PassengerAI>();


        if(ai != null)
        {
            ai.SetTarget(queuePosition);
        }
    }
}