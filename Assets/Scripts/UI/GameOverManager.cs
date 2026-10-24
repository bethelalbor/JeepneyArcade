using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject gameOverCanvas;
    public TMP_Text statusText;
    public TMP_Text emergencyActionText;
    public TMP_Text earningsText;
    public TMP_Text scoreText;


    [Header("Systems")]
    public FuelSystem fuelSystem;
    public VehicleHealth vehicleHealth;
    public EconomyManager economyManager;



    [Header("Emergency Costs")]
    public int emergencyFuelCost = 200;
    public int emergencyRepairCost = 200;


    private bool vehicleIsWrecked = false;
    private int gameOverEarnings;
    private bool hasGameOverEarnings;
    private int gameOverScore;
    private bool hasGameOverScore;



    public void ShowFuelGameOver()
    {
        vehicleIsWrecked = false;

        statusText.text = "OUT OF FUEL";
        emergencyActionText.text = "EMERGENCY REFUEL ₱" + emergencyFuelCost;
        UpdateGameOverSummary();

        gameOverCanvas.SetActive(true);

        Time.timeScale = 0f;
    }



    public void ShowWrecked()
    {
        vehicleIsWrecked = true;

        statusText.text = "WRECKED";
        emergencyActionText.text = "EMERGENCY REPAIR ₱" + emergencyRepairCost;
        UpdateGameOverSummary();

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



    private void UpdateGameOverSummary()
    {
        if(economyManager != null)
        {
            gameOverEarnings = economyManager.currentBalance;
            hasGameOverEarnings = true;

            if(earningsText != null)
            {
                earningsText.text = "Earnings:₱" + gameOverEarnings;
            }
        }

        if(ScoreManager.Instance != null)
        {
            gameOverScore = ScoreManager.Instance.currentScore;
            hasGameOverScore = true;

            if(scoreText != null)
            {
                scoreText.text = "Score:" + gameOverScore;
            }
        }
    }



    public void ExitGame()
    {
        if(hasGameOverEarnings)
        {
            SavedBalance.Add(gameOverEarnings);
            hasGameOverEarnings = false;
        }

        if(hasGameOverScore)
        {
            SavedHighScore.SetIfHigher(gameOverScore);
            hasGameOverScore = false;
        }

        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenu");
    }
}
