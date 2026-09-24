using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("References")]
    public GameObject gameOverCanvas;
    public FuelSystem fuelSystem;
    public EconomyManager economyManager;


    [Header("Emergency Fuel")]
    public int emergencyFuelCost = 200;



    public void ShowGameOver()
    {
        gameOverCanvas.SetActive(true);

        Time.timeScale = 0f;
    }



    public void EmergencyRefuel()
    {
        if(EconomyManager.Instance != null &&
           EconomyManager.Instance.TrySpendMoney(emergencyFuelCost))
        {
            Time.timeScale = 1f;


            fuelSystem.EmergencyRefuel();


            gameOverCanvas.SetActive(false);


            Debug.Log(
                "Emergency refuel purchased for ₱"
                + emergencyFuelCost
            );
        }
        else
        {
            Debug.Log("Not enough money for emergency refuel");
        }
    }



    public void ExitGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenu");
    }
}
