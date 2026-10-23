using UnityEngine;


public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;


    [Header("Score")]
    public int currentScore = 0;


    [Header("Perfect Pickup Combo")]
    public int cleanDeliveryStreak = 0;



    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }



    public void AddScore(int amount)
    {
        currentScore += amount;

        Debug.Log(
            "Score +" + amount +
            " | Total: " + currentScore
        );
    }



    public int GetPassengerPickupScore(bool isPerfectPickup)
    {
        if(!isPerfectPickup)
        {
            cleanDeliveryStreak = 0;

            return 10;
        }


        cleanDeliveryStreak++;


        int comboBonus =
            Mathf.Min(cleanDeliveryStreak - 1, 3)
            * 10;


        return 25 + comboBonus;
    }



    public void ResetScore()
    {
        currentScore = 0;
        cleanDeliveryStreak = 0;
    }
}
