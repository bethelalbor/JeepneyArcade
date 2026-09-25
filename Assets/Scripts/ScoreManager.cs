using UnityEngine;


public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;


    [Header("Score")]
    public int currentScore = 0;


    [Header("Combo")]
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



    public void ResetScore()
    {
        currentScore = 0;
        cleanDeliveryStreak = 0;
    }
}