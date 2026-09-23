using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;

    public int currentBalance = 0;


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


    public void AddMoney(int amount)
    {
        currentBalance += amount;

        Debug.Log(
            "Earned: " + amount +
            " | Balance: " + currentBalance
        );
    }



    public bool TrySpendMoney(int amount)
    {
        if(currentBalance < amount)
            return false;


        currentBalance -= amount;


        Debug.Log(
            "Spent: " + amount +
            " | Balance: " + currentBalance
        );


        return true;
    }
}
