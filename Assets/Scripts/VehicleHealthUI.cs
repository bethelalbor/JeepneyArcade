using UnityEngine;
using UnityEngine.UI;


public class VehicleHealthUI : MonoBehaviour
{
    public Slider healthSlider;

    public VehicleHealth vehicleHealth;


    void Update()
    {
        if(vehicleHealth != null)
        {
            healthSlider.value =
                vehicleHealth.GetHealthPercentage();
        }
    }
}