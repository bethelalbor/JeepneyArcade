using UnityEngine;
using UnityEngine.EventSystems;

public class MobileSteeringButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private bool steeringLeft;

    private PlayerMovement playerMovement;
    private bool isPressed;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (playerMovement == null)
            return;

        isPressed = true;
        playerMovement.SetMobileSteering(steeringLeft, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ReleaseSteering();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ReleaseSteering();
    }

    private void OnDisable()
    {
        ReleaseSteering();
    }

    private void ReleaseSteering()
    {
        if (!isPressed)
            return;

        isPressed = false;
        if (playerMovement != null)
            playerMovement.SetMobileSteering(steeringLeft, false);
    }
}
