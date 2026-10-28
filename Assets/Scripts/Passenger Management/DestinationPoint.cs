using UnityEngine;

public class DestinationPoint : MonoBehaviour
{
    public string destinationName;

    public Transform walkPoint;

    [Header("Marker Visuals")]
    [Tooltip("The marker visual to show only while an onboard passenger is travelling here.")]
    [SerializeField] private Transform visuals;
    [Tooltip("Complete pulse cycles per second.")]
    [Min(0f)] [SerializeField] private float pulseSpeed = 1f;
    [Min(0f)] [SerializeField] private float minimumPulseScale = 0.9f;
    [Min(0f)] [SerializeField] private float maximumPulseScale = 1.1f;

    private PassengerManager passengerManager;
    private Vector3 baseVisualScale;
    private bool visualsVisible;

    private void Awake()
    {
        if (visuals == null)
            visuals = transform.Find("Visuals");

        if (visuals != null)
        {
            baseVisualScale = visuals.localScale;
            visuals.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        FindPassengerManager();
        RefreshVisualState();
    }

    private void Start()
    {
        FindPassengerManager();
        RefreshVisualState();
    }

    private void OnDisable()
    {
        if (passengerManager != null)
            passengerManager.PassengersOnBoardChanged -= RefreshVisualState;
    }

    private void Update()
    {
        if (!visualsVisible || visuals == null)
            return;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        visuals.localScale = baseVisualScale * Mathf.Lerp(minimumPulseScale, maximumPulseScale, pulse);
    }

    private void FindPassengerManager()
    {
        if (passengerManager != null)
            return;

        passengerManager = FindAnyObjectByType<PassengerManager>();

        if (passengerManager != null)
            passengerManager.PassengersOnBoardChanged += RefreshVisualState;
    }

    private void RefreshVisualState()
    {
        if (visuals == null)
            return;

        bool shouldShow = HasOnboardPassengerForThisDestination();
        if (visualsVisible == shouldShow)
            return;

        visualsVisible = shouldShow;
        visuals.localScale = baseVisualScale;
        visuals.gameObject.SetActive(visualsVisible);
    }

    private bool HasOnboardPassengerForThisDestination()
    {
        if (passengerManager == null)
            return false;

        foreach (GameObject passenger in passengerManager.passengersOnBoard)
        {
            if (passenger == null)
                continue;

            PassengerData passengerData = passenger.GetComponent<PassengerData>();
            if (passengerData != null && passengerData.destination == this)
                return true;
        }

        return false;
    }
}
