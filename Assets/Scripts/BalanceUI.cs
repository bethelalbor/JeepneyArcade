using UnityEngine;
using TMPro;

public class BalanceUI : MonoBehaviour
{
    public TMP_Text balanceText;


    void Update()
    {
        if(EconomyManager.Instance != null)
        {
            balanceText.text =
                "₱" + EconomyManager.Instance.currentBalance;
        }
    }
}
