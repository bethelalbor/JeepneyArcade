using UnityEngine;
using UnityEngine.UI;

public class FuelUI : MonoBehaviour
{
    public Slider fuelSlider;

    public FuelSystem fuelSystem;


    void Update()
    {
        if(fuelSystem != null)
        {
            fuelSlider.value =
                fuelSystem.GetFuelPercentage();
        }
    }
}