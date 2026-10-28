using UnityEngine;

/// <summary>
/// Enables the menu character's dance-only Animator transition while its
/// MainMenu scene instance is active.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class MainMenuDancePlayer : MonoBehaviour
{
    [Tooltip("Boolean Animator parameter that enables the Dance_01 transition.")]
    [SerializeField] private string danceParameterName = "IsDancing";

    private Animator animator;
    private int danceParameterHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        danceParameterHash = Animator.StringToHash(danceParameterName);
    }

    private void OnEnable()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
            animator.SetBool(danceParameterHash, true);
        else
            Debug.LogError("MainMenuDancePlayer requires an Animator.", this);
    }

    private void OnDisable()
    {
        if (animator != null)
            animator.SetBool(danceParameterHash, false);
    }
}
