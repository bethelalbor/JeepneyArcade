using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Gives a menu or modal an initial gamepad selection and maps the UI Cancel
/// action to an existing button without changing the project's input bindings.
/// </summary>
[DisallowMultipleComponent]
public sealed class UIFocusAndCancel : MonoBehaviour
{
    [Header("Gamepad Focus")]
    [SerializeField] private Selectable initialSelection;

    [Header("Cancel Action")]
    [Tooltip("Invoked when the UI Cancel action is pressed while this UI is active.")]
    [SerializeField] private Button cancelButton;

    private InputAction cancelAction;

    private void OnEnable()
    {
        SelectInitialControl();
        SubscribeToCancelAction();
    }

    private void Start()
    {
        SelectInitialControl();
    }

    private void OnDisable()
    {
        if (cancelAction != null)
            cancelAction.performed -= HandleCancel;

        cancelAction = null;
    }

    private void SelectInitialControl()
    {
        if (initialSelection == null || !initialSelection.isActiveAndEnabled || !initialSelection.interactable)
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(initialSelection.gameObject);
    }

    private void SubscribeToCancelAction()
    {
        InputSystemUIInputModule inputModule = FindAnyObjectByType<InputSystemUIInputModule>();
        if (inputModule == null || inputModule.cancel == null || inputModule.cancel.action == null)
            return;

        cancelAction = inputModule.cancel.action;
        cancelAction.performed += HandleCancel;
    }

    private void HandleCancel(InputAction.CallbackContext context)
    {
        if (!isActiveAndEnabled || cancelButton == null || !cancelButton.isActiveAndEnabled || !cancelButton.interactable)
            return;

        cancelButton.onClick.Invoke();
    }
}
