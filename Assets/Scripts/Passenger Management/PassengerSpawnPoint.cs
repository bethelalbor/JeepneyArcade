using UnityEngine;

public class PassengerSpawnPoint : MonoBehaviour
{
    public Transform spawnPoint;
    public PassengerQueuePoint pickupPoint;

    public DestinationPoint[] possibleDestinations;
}