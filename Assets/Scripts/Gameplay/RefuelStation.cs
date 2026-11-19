using System.Collections.Generic;
using UnityEngine;

public class RefuelStation : MonoBehaviour
{
    [Header("Refueling")]
    public int refuelCost = 50;
    public float stopDelay = 2f;

    private readonly HashSet<Collider> vehicleColliders = new HashSet<Collider>();
    private FuelSystem fuelSystem;
    private Rigidbody vehicleRb;
    private bool refueling;
    private float stoppedTime;
    private bool fundsWarningShown;

    private void Update()
    {
        vehicleColliders.RemoveWhere(IsUnavailable);
        if (vehicleColliders.Count == 0 || vehicleRb == null || fuelSystem == null)
        {
            ReleaseVehicle();
            return;
        }
        if (Time.timeScale <= 0f)
            return;
        if (refueling)
        {
            if (fuelSystem.RefuelOverTime())
                refueling = false;
            return;
        }
        if (fuelSystem.GetFuelPercentage() >= 1f || vehicleRb.linearVelocity.magnitude >= 0.1f)
        {
            stoppedTime = 0f;
            return;
        }
        stoppedTime += Time.deltaTime;
        if (stoppedTime < stopDelay)
            return;
        EconomyManager economy = EconomyManager.Instance;
        if (economy == null || economy.currentBalance < refuelCost)
        {
            if (!fundsWarningShown)
                Debug.Log("Insufficient balance for refueling.", this);
            fundsWarningShown = true;
            return;
        }
        if (!economy.TrySpendMoney(refuelCost))
            return;
        fundsWarningShown = false;
        stoppedTime = 0f;
        refueling = true;
        vehicleRb.linearVelocity = Vector3.zero;
        vehicleRb.angularVelocity = Vector3.zero;
        fuelSystem.StartRefueling();
    }

    private void OnTriggerEnter(Collider other) => TrackVehicle(other);
    private void OnTriggerStay(Collider other) => TrackVehicle(other);

    private void TrackVehicle(Collider other)
    {
        Rigidbody candidate = other.attachedRigidbody;
        if (candidate == null || !candidate.CompareTag("Player") ||
            (vehicleRb != null && candidate != vehicleRb))
            return;
        FuelSystem candidateFuel = candidate.GetComponent<FuelSystem>();
        if (candidateFuel == null)
            return;
        vehicleRb = candidate;
        fuelSystem = candidateFuel;
        vehicleColliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (vehicleColliders.Remove(other) && vehicleColliders.Count == 0)
            ReleaseVehicle();
    }

    private void OnDisable() => ReleaseVehicle();

    private void ReleaseVehicle()
    {
        if (refueling && fuelSystem != null)
            fuelSystem.CancelRefueling();
        refueling = false;
        stoppedTime = 0f;
        fundsWarningShown = false;
        vehicleColliders.Clear();
        fuelSystem = null;
        vehicleRb = null;
    }

    private static bool IsUnavailable(Collider collider)
    {
        return collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy;
    }
}
