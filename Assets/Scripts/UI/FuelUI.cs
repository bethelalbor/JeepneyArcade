using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FuelUI : MonoBehaviour
{
    public Image fuelBar;
    public TMP_Text fuelText;

    public FuelSystem fuelSystem;


    void Update()
    {
        if(fuelSystem == null)
            return;


        float fuelPercentage =
            fuelSystem.GetFuelPercentage();


        fuelBar.fillAmount =
            fuelPercentage;


        int currentFuel =
            Mathf.CeilToInt(
                fuelSystem.currentFuel
            );


        int maxFuel =
            Mathf.CeilToInt(
                fuelSystem.maxFuel
            );


        fuelText.text =
            currentFuel +
            "/" +
            maxFuel;
    }
}