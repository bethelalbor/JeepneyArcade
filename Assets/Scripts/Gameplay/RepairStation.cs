using System.Collections.Generic;
using UnityEngine;

public class RepairStation : MonoBehaviour
{
    [Header("Repair Settings")]
    public int repairCost = 50;
    public float repairDelay = 1f;

    private readonly HashSet<Collider> vehicleColliders = new HashSet<Collider>();
    private VehicleHealth vehicleHealth;
    private Rigidbody vehicleRb;
    private PlayerMovement movement;
    private bool repairing;
    private bool paid;
    private float repairTime;
    private bool fundsWarningShown;

    private void Update()
    {
        vehicleColliders.RemoveWhere(IsUnavailable);
        if (vehicleColliders.Count == 0 || vehicleRb == null || vehicleHealth == null)
        {
            ReleaseVehicle();
            return;
        }
        if (Time.timeScale <= 0f)
            return;
        if (!repairing)
        {
            if (vehicleHealth.GetHealthPercentage() >= 1f || vehicleRb.linearVelocity.magnitude >= 0.1f)
                return;
            if (!CanAffordRepair())
                return;
            repairing = true;
            repairTime = 0f;
            paid = false;
            vehicleHealth.StartRepairing();
        }

        repairTime += Time.deltaTime;
        if (repairTime < repairDelay)
            return;
        if (!paid)
        {
            if (!CanAffordRepair() || !EconomyManager.Instance.TrySpendMoney(repairCost))
            {
                CancelRepair();
                return;
            }
            paid = true;
        }
        if (vehicleHealth.RepairOverTime())
        {
            repairing = false;
            paid = false;
            repairTime = 0f;
        }
    }

    private bool CanAffordRepair()
    {
        EconomyManager economy = EconomyManager.Instance;
        bool available = economy != null && economy.currentBalance >= repairCost;
        if (!available && !fundsWarningShown)
            Debug.Log("Insufficient balance for repair.", this);
        fundsWarningShown = !available;
        return available;
    }

    private void OnTriggerEnter(Collider other) => TrackVehicle(other);
    private void OnTriggerStay(Collider other) => TrackVehicle(other);

    private void TrackVehicle(Collider other)
    {
        Rigidbody candidate = other.attachedRigidbody;
        if (candidate == null || !candidate.CompareTag("Player") ||
            (vehicleRb != null && candidate != vehicleRb))
            return;
        VehicleHealth candidateHealth = candidate.GetComponent<VehicleHealth>();
        if (candidateHealth == null)
            return;
        vehicleRb = candidate;
        vehicleHealth = candidateHealth;
        movement = candidate.GetComponent<PlayerMovement>();
        vehicleColliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (vehicleColliders.Remove(other) && vehicleColliders.Count == 0)
            ReleaseVehicle();
    }

    private void OnDisable() => ReleaseVehicle();

    private void CancelRepair()
    {
        // Do not override a game-over lock while releasing this station's movement lock.
        FuelSystem fuel = vehicleRb != null ? vehicleRb.GetComponent<FuelSystem>() : null;
        if (repairing && movement != null && vehicleHealth != null &&
            vehicleHealth.currentHealth > 0f && (fuel == null || fuel.currentFuel > 0f))
            movement.EnableMovement();
        repairing = false;
        paid = false;
        repairTime = 0f;
    }

    private void ReleaseVehicle()
    {
        CancelRepair();
        vehicleColliders.Clear();
        fundsWarningShown = false;
        vehicleHealth = null;
        vehicleRb = null;
        movement = null;
    }

    private static bool IsUnavailable(Collider collider)
    {
        return collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy;
    }
}
