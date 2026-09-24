using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject gameOverCanvas;
    public TMP_Text statusText;


    [Header("Systems")]
    public FuelSystem fuelSystem;
    public VehicleHealth vehicleHealth;
    public EconomyManager economyManager;



    [Header("Emergency Costs")]
    public int emergencyFuelCost = 200;
    public int emergencyRepairCost = 200;



    public void ShowFuelGameOver()
    {
        statusText.text = "OUT OF FUEL";

        gameOverCanvas.SetActive(true);

        Time.timeScale = 0f;
    }



    public void ShowWrecked()
    {
        statusText.text = "WRECKED";

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