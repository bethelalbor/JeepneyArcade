using UnityEngine;

public class FuelSystem : MonoBehaviour
{
    [Header("Fuel Settings")]
    public float maxFuel = 100f;
    public float currentFuel = 100f;


    [Header("Consumption")]
    public float fuelConsumptionRate = 2f;


    [Header("Refueling")]
    public float refuelRate = 20f;


    private PlayerMovement playerMovement;
    private Rigidbody rb;


    private bool isRefueling = false;



    void Start()
    {
        currentFuel = maxFuel;


        playerMovement =
            GetComponent<PlayerMovement>();


        rb =
            GetComponent<Rigidbody>();
    }



    void Update()
    {
        if(!isRefueling)
        {
            ConsumeFuel();
        }
    }



    void ConsumeFuel()
    {
        if(rb.linearVelocity.magnitude > 0.1f)
        {
            currentFuel -=
                fuelConsumptionRate *
                Time.deltaTime;
        }


        currentFuel =
            Mathf.Clamp(
                currentFuel,
                0,
                maxFuel
            );


        if(currentFuel <= 0)
        {
            playerMovement.DisableMovement();
        }
    }



    public void StartRefueling()
    {
        isRefueling = true;


        playerMovement.DisableMovement();
    }



    public bool RefuelOverTime()
    {
        currentFuel +=
            refuelRate *
            Time.deltaTime;


        currentFuel =
            Mathf.Clamp(
                currentFuel,
                0,
                maxFuel
            );


        if(currentFuel >= maxFuel)
        {
            FinishRefueling();

            return true;
        }


        return false;
    }



    void FinishRefueling()
    {
        currentFuel = maxFuel;


        isRefueling = false;


        playerMovement.EnableMovement();


        Debug.Log("Refueling complete");
        Debug.Log("Calling EnableMovement");
        playerMovement.EnableMovement();
    }



    public float GetFuelPercentage()
    {
        return currentFuel / maxFuel;
    }
}