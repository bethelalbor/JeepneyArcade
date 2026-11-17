using UnityEngine;
using UnityEngine.EventSystems;

public class MobileThrottleButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private bool accelerating;

    private PlayerMovement playerMovement;
    private bool isPressed;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>(); //search for the PlayerMovement script in the scene pag nagmultiple players, this will find the first one it encounters so di sya ideal for multiplayer, but for single player it should be fine

        if (playerMovement == null)
            return;

        isPressed = true;
        playerMovement.SetMobileThrottle(accelerating, true); //notify the PlayerMovement script that the throttle button is pressed
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ReleaseThrottle();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ReleaseThrottle();
    }

    private void OnDisable()
    {
        ReleaseThrottle();
    }

    private void ReleaseThrottle()
    {
        if (!isPressed)
            return;

        isPressed = false;
        if (playerMovement != null)
            playerMovement.SetMobileThrottle(accelerating, false);
    }
}
