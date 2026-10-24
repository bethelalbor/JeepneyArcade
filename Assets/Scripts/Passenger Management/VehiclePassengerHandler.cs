using UnityEngine;

public class VehiclePassengerHandler : MonoBehaviour
{
    public Transform passengerDoorPoint;

    public Transform GetPassengerDoor()
    {
        return passengerDoorPoint;
    }
}