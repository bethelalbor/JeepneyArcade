using UnityEngine;
using System.Collections;

public class RepairStation : MonoBehaviour
{
    [Header("Repair Settings")]
    public int repairCost = 50;
    public float repairDelay = 1f;


    private VehicleHealth vehicleHealth;
    private EconomyManager economyManager;
    private Rigidbody vehicleRb;

    private bool playerInside = false;
    private bool repairing = false;
    private bool repairBlockedByInsufficientFunds = false;



    void Update()
    {
        if(playerInside && !repairing &&
           !repairBlockedByInsufficientFunds &&
           vehicleHealth != null &&
           vehicleHealth.GetHealthPercentage() < 1f)
        {
            if(vehicleRb != null &&
               vehicleRb.linearVelocity.magnitude < 0.1f)
            {
                StartCoroutine(RepairVehicle());
            }
        }
    }



    IEnumerator RepairVehicle()
    {
        repairing = true;


        if(economyManager == null ||
           economyManager.currentBalance < repairCost)
        {
            Debug.Log("Insufficient balance for repair");

            repairBlockedByInsufficientFunds = true;
            repairing = false;

            yield break;
        }


        PlayerMovement movement =
            vehicleHealth.GetComponent<PlayerMovement>();


        if(movement != null)
        {
            vehicleHealth.StartRepairing();
        }


        yield return new WaitForSeconds(repairDelay);



        if(economyManager.TrySpendMoney(repairCost))
        {
            while(!vehicleHealth.RepairOverTime())
            {
                yield return null;
            }

            Debug.Log(
                "Vehicle repaired for ₱"
                + repairCost
            );
        }
        else
        {
            Debug.Log(
                "Insufficient balance for repair"
            );

            repairBlockedByInsufficientFunds = true;

            if(movement != null)
            {
                movement.EnableMovement();
            }
        }


        repairing = false;
    }



    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player"))
            return;


        playerInside = true;
        repairBlockedByInsufficientFunds = false;


        vehicleHealth =
            other.GetComponent<VehicleHealth>();


        vehicleRb =
            other.GetComponent<Rigidbody>();


        economyManager =
            FindFirstObjectByType<EconomyManager>();
    }



    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerInside = false;

            repairing = false;
            repairBlockedByInsufficientFunds = false;

            vehicleHealth = null;
            vehicleRb = null;
        }
    }
}
