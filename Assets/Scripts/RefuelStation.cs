using UnityEngine;

public class RefuelStation : MonoBehaviour
{
    [Header("Refueling")]
    public int refuelCost = 50;
    public float stopDelay = 2f;


    private FuelSystem fuelSystem;
    private Rigidbody vehicleRb;


    private bool playerInside = false;
    private bool refueling = false;
    private float stoppedTime = 0f;



    void Update()
    {
        if(playerInside && !refueling &&
           fuelSystem != null &&
           fuelSystem.GetFuelPercentage() < 1f)
        {
            if(vehicleRb != null &&
               vehicleRb.linearVelocity.magnitude < 0.1f)
            {
                stoppedTime += Time.deltaTime;


                if(stoppedTime >= stopDelay)
                {
                    StartRefuel();
                }
            }
            else
            {
                stoppedTime = 0f;
            }
        }
        else
        {
            stoppedTime = 0f;
        }


        if(refueling)
        {
            if(fuelSystem.RefuelOverTime())
            {
                refueling = false;
            }
        }
    }



    void StartRefuel()
    {
        if(EconomyManager.Instance == null ||
           !EconomyManager.Instance.TrySpendMoney(refuelCost))
            return;


        refueling = true;

        vehicleRb.linearVelocity = Vector3.zero;
        vehicleRb.angularVelocity = Vector3.zero;

        fuelSystem.StartRefueling();

        Debug.Log("Started refueling");
    }



    void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player"))
            return;


        playerInside = true;

        fuelSystem =
            other.GetComponent<FuelSystem>();

        vehicleRb =
            other.GetComponent<Rigidbody>();


        if(EconomyManager.Instance != null &&
           EconomyManager.Instance.currentBalance < refuelCost)
        {
            Debug.Log("not enough balance");
        }
    }



    void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerInside = false;

            refueling = false;
            stoppedTime = 0f;

            fuelSystem = null;
            vehicleRb = null;
        }
    }
}
