using UnityEngine;

/// <summary>
/// Starts the menu character's dance when its MainMenu scene instance becomes active.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class MainMenuDancePlayer : MonoBehaviour
{
    [Tooltip("Full Animator state path to play when this MainMenu-only component is enabled.")]
    [SerializeField] private string stateName = "Base Layer.Dance_01";
    [Min(0)] [SerializeField] private int layerIndex;
    [Range(0f, 1f)] [SerializeField] private float normalizedStartTime;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("MainMenuDancePlayer requires an Animator.", this);
            return;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(layerIndex, stateHash))
        {
            Debug.LogError($"MainMenuDancePlayer could not find '{stateName}' on layer {layerIndex}.", this);
            return;
        }

        animator.Play(stateHash, layerIndex, normalizedStartTime);
    }
}
