using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject gameOverCanvas;
    public TMP_Text statusText;
    public TMP_Text emergencyActionText;


    [Header("Systems")]
    public FuelSystem fuelSystem;
    public VehicleHealth vehicleHealth;
    public EconomyManager economyManager;



    [Header("Emergency Costs")]
    public int emergencyFuelCost = 200;
    public int emergencyRepairCost = 200;


    private bool vehicleIsWrecked = false;



    public void ShowFuelGameOver()
    {
        vehicleIsWrecked = false;

        statusText.text = "OUT OF FUEL";
        emergencyActionText.text =
            "EMERGENCY REFUEL (₱" +
            emergencyFuelCost +
            ")";

        gameOverCanvas.SetActive(true);

        Time.timeScale = 0f;
    }



    public void ShowWrecked()
    {
        vehicleIsWrecked = true;

        statusText.text = "WRECKED";
        emergencyActionText.text =
            "EMERGENCY REPAIR (₱" +
            emergencyRepairCost +
            ")";

        gameOverCanvas.SetActive(true);

        Time.timeScale = 0f;
    }



    public void EmergencyRefuel()
    {
        if(economyManager.TrySpendMoney(emergencyFuelCost))
        {
            Time.timeScale = 1f;

            fuelSystem.EmergencyRefuel();

            gameOverCanvas.SetActive(false);
        }
    }




    public void EmergencyRecovery()
    {
        if(vehicleIsWrecked)
        {
            EmergencyRepair();
        }
        else
        {
            EmergencyRefuel();
        }
    }



    public void EmergencyRepair()
    {
        if(economyManager.TrySpendMoney(emergencyRepairCost))
        {
            Time.timeScale = 1f;

            vehicleHealth.RepairVehicle();

            gameOverCanvas.SetActive(false);
        }
    }



    public void ExitGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenu");
    }
}
