using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VehicleHealthUI : MonoBehaviour
{
    public Image healthBar;
    public TMP_Text healthText;

    public VehicleHealth vehicleHealth;


    void Update()
    {
        if(vehicleHealth == null)
            return;


        float healthPercentage =
            vehicleHealth.GetHealthPercentage();


        healthBar.fillAmount =
            healthPercentage;


        int currentHealth =
            Mathf.CeilToInt(
                vehicleHealth.currentHealth
            );


        int maxHealth =
            Mathf.CeilToInt(
                vehicleHealth.maxHealth
            );


        healthText.text =
            currentHealth +
            "/" +
            maxHealth;
    }
}