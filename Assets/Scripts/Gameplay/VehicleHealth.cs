using UnityEngine;

public class VehicleHealth : MonoBehaviour
{
    [Header("Vehicle Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;


    [Header("Collision Damage")]
    public float collisionDamageMultiplier = 5f;
    [Tooltip("Maximum damage a single collision can deal to the vehicle.")]
    [Min(0f)] public float maximumCollisionDamage = 40f;
    [Tooltip("Seconds to wait before another collision can damage the vehicle.")]
    [Min(0f)] public float collisionDamageCooldown = 1f;


    [Header("Repairing")]
    public float repairRate = 20f;


    private PlayerMovement playerMovement;

    private CameraFollow cameraFollow;

    private float nextCollisionDamageTime;



    void Start()
    {
        currentHealth = maxHealth;

        playerMovement =
            GetComponent<PlayerMovement>();

        cameraFollow =
            FindAnyObjectByType<CameraFollow>();
    }



    private void OnCollisionEnter(Collision collision)
    {
        if(Time.time < nextCollisionDamageTime)
            return;


        float impactForce =
            collision.relativeVelocity.magnitude;


        float damage =
            Mathf.Min(
                impactForce * collisionDamageMultiplier,
                maximumCollisionDamage
            );


        nextCollisionDamageTime =
            Time.time + collisionDamageCooldown;


        TakeDamage(damage);


        if(cameraFollow != null)
        {
            cameraFollow.ShakeForDamage(damage);
        }
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
            FindAnyObjectByType<GameOverManager>(FindObjectsInactive.Include);


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
