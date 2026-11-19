
using UnityEngine;

public class RandomPlayerSpawn : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        if (player == null || spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogWarning("Player spawn references are missing.");
            return;
        }

        Transform selectedSpawn =
            spawnPoints[Random.Range(0, spawnPoints.Length)];

        if (selectedSpawn == null)
        {
            Debug.LogWarning("A player spawn point is missing.");
            return;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.position = selectedSpawn.position;
            rb.rotation = selectedSpawn.rotation;

            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            player.SetPositionAndRotation(
                selectedSpawn.position,
                selectedSpawn.rotation
            );
        }
    }
}
