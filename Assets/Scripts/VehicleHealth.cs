using UnityEngine;

public class VehicleHealth : MonoBehaviour
{
    [Header("Vehicle Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;


    [Header("Collision Damage")]
    public float collisionDamageMultiplier = 5f;


    [Header("Repairing")]
    public float repairRate = 20f;


    private PlayerMovement playerMovement;



    void Start()
    {
        currentHealth = maxHealth;

        playerMovement =
            GetComponent<PlayerMovement>();
    }



    private void OnCollisionEnter(Collision collision)
    {
        float impactForce =
            collision.relativeVelocity.magnitude;


        float damage =
            impactForce * collisionDamageMultiplier;


        TakeDamage(damage);
    }



    public void TakeDamage(float damage)
    {
        currentHealth -= damage;


        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0,
                maxHealth
            );


        Debug.Log(
            "Vehicle Damage: "
            + damage
            +
            " | Health: "
            + currentHealth
        );


        if(currentHealth <= 0)
        {
            WreckVehicle();
        }
    }



    void WreckVehicle()
    {
        playerMovement.DisableMovement();


        GameOverManager gameOver =
            FindAnyObjectByType<GameOverManager>();


        if(gameOver != null)
        {
            gameOver.ShowWrecked();
        }
    }



    public void RepairVehicle()
    {
        currentHealth = maxHealth;

        playerMovement.EnableMovement();


        Debug.Log(
            "Vehicle repaired"
        );
    }




    public void StartRepairing()
    {
        playerMovement.DisableMovement();
    }



    public bool RepairOverTime()
    {
        currentHealth +=
            repairRate *
            Time.deltaTime;


        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0,
                maxHealth
            );


        if(currentHealth >= maxHealth)
        {
            FinishRepairing();

            return true;
        }


        return false;
    }



    void FinishRepairing()
    {
        currentHealth = maxHealth;

        playerMovement.EnableMovement();

        Debug.Log("Vehicle repaired");
    }



    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
}
