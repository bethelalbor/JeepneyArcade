using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Presentation only; service colliders and station buildings never pulse.</summary>
[DisallowMultipleComponent]
public sealed class StationZoneVisual : MonoBehaviour
{
    public enum ServiceType { Fuel, Repair }
    [SerializeField] private ServiceType service;
    [SerializeField] private BoxCollider serviceZone;
    [SerializeField] private Transform markerVisuals;
    [SerializeField] private Renderer zoneMarker;
    [SerializeField] private Renderer zoneOutline;
    [SerializeField] private Image floatingIcon;
    [SerializeField] private FuelSystem playerFuel;
    [SerializeField] private VehicleHealth playerHealth;
    [SerializeField] private Camera playerCamera;
    [Tooltip("Show this station's zone, icon, and cost only below this fuel/health value.")]
    [SerializeField, Min(0f)] private float visibilityThreshold = 80f;
    [Header("Cost Label")]
    [SerializeField] private TMP_Text costLabel;
    [Tooltip("Leave empty to use Refuel or Repair. The price always comes from the station component.")]
    [SerializeField] private string costLabelPrefix = "";
    [SerializeField, Min(1f)] private float costFontSize = 22f;
    [SerializeField] private Color costTextColor = Color.white;
    [Tooltip("Position relative to the icon, in its local UI units.")]
    [SerializeField] private Vector2 costLabelOffset = new Vector2(0f, -75f);
    [Header("Pulse")]
    [SerializeField, Min(0f)] private float pulseSpeed = 1f;
    [SerializeField, Min(0f)] private float minimumPulseScale = 0.9f;
    [SerializeField, Min(0f)] private float maximumPulseScale = 1.1f;
    [SerializeField, Min(0f)] private float iconHeight = 1.5f;

    private MaterialPropertyBlock markerProperties;
    private MaterialPropertyBlock outlineProperties;
    private Color markerColor;
    private Color outlineColor;
    private Color iconColor;
    private Vector3 markerScale;
    private Vector3 outlineScale;
    private Vector3 iconScale;
    private float nextReferenceLookup;
    private RefuelStation refuelStation;
    private RepairStation repairStation;
    private int displayedCost;
    private string displayedPrefix;
    private bool hasDisplayedCost;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (serviceZone != null)
        {
            refuelStation = serviceZone.GetComponent<RefuelStation>();
            repairStation = serviceZone.GetComponent<RepairStation>();
        }
        if (costLabel != null)
            costLabel.raycastTarget = false;
        markerProperties = new MaterialPropertyBlock();
        outlineProperties = new MaterialPropertyBlock();
        if (zoneMarker != null)
        {
            markerScale = zoneMarker.transform.localScale;
            markerColor = ReadColor(zoneMarker);
        }
        if (zoneOutline != null)
        {
            outlineScale = zoneOutline.transform.localScale;
            outlineColor = ReadColor(zoneOutline);
        }
        if (floatingIcon != null)
        {
            iconColor = floatingIcon.color;
            iconScale = floatingIcon.transform.localScale;
            floatingIcon.raycastTarget = false;
        }
        SetVisible(false);
    }

    private void LateUpdate()
    {
        ResolveReferences();
        bool needed = service == ServiceType.Fuel
            ? playerFuel != null && playerFuel.currentFuel < visibilityThreshold
            : playerHealth != null && playerHealth.currentHealth < visibilityThreshold;
        bool visible = needed && serviceZone != null && serviceZone.enabled && serviceZone.gameObject.activeInHierarchy;
        SetVisible(visible);
        if (!visible || markerVisuals == null)
            return;

        Vector3 center = serviceZone.transform.TransformPoint(serviceZone.center);
        markerVisuals.position = center;
        markerVisuals.rotation = serviceZone.transform.rotation;
        float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float scale = Mathf.Lerp(minimumPulseScale, maximumPulseScale, pulse);
        if (zoneMarker != null)
        {
            zoneMarker.transform.localScale = markerScale * scale;
            SetOpacity(zoneMarker, markerProperties, markerColor, pulse);
        }
        if (zoneOutline != null)
        {
            zoneOutline.transform.localScale = outlineScale * scale;
            SetOpacity(zoneOutline, outlineProperties, outlineColor, pulse);
        }
        if (floatingIcon != null)
        {
            Transform icon = floatingIcon.transform;
            icon.position = center + Vector3.up * (serviceZone.bounds.extents.y + iconHeight);
            icon.localScale = iconScale * scale;
            if (playerCamera != null)
            {
                // World-space UI faces its viewer along its negative Z axis.
                Vector3 direction = icon.position - playerCamera.transform.position;
                if (direction.sqrMagnitude > 0.0001f)
                    icon.rotation = Quaternion.LookRotation(direction, playerCamera.transform.up);
            }
            Color color = iconColor;
            color.a *= pulse;
            floatingIcon.color = color;
        }
        UpdateCostLabel(pulse);
    }

    private void UpdateCostLabel(float pulse)
    {
        if (costLabel == null)
            return;
        bool hasStation = service == ServiceType.Fuel ? refuelStation != null : repairStation != null;
        costLabel.enabled = hasStation;
        if (!hasStation)
            return;

        int cost = service == ServiceType.Fuel ? refuelStation.refuelCost : repairStation.repairCost;
        string prefix = string.IsNullOrEmpty(costLabelPrefix)
            ? (service == ServiceType.Fuel ? "Refuel" : "Repair") : costLabelPrefix;
        if (!hasDisplayedCost || displayedCost != cost || displayedPrefix != prefix)
        {
            // Existing fonts may not include the peso glyph; never show a missing box.
            string currency = costLabel.font != null && costLabel.font.HasCharacter('\u20b1') ? "\u20b1" : "PHP ";
            costLabel.text = prefix + " - " + currency + cost;
            displayedCost = cost;
            displayedPrefix = prefix;
            hasDisplayedCost = true;
        }
        // The label is a child of the icon's world-space Canvas, sharing its billboard
        // rotation and pulse scale; TMP color has its own synchronized alpha.
        costLabel.rectTransform.anchoredPosition = costLabelOffset;
        costLabel.fontSize = costFontSize;
        Color color = costTextColor;
        color.a *= pulse;
        costLabel.color = color;
    }

    private void ResolveReferences()
    {
        if (Time.unscaledTime < nextReferenceLookup)
            return;
        nextReferenceLookup = Time.unscaledTime + 1f;
        if (playerFuel == null || playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerFuel = player.GetComponent<FuelSystem>();
                playerHealth = player.GetComponent<VehicleHealth>();
            }
        }
        if (playerCamera == null || !playerCamera.isActiveAndEnabled)
            playerCamera = Camera.main;
    }

    private void OnDisable() => SetVisible(false);

    private void SetVisible(bool visible)
    {
        if (markerVisuals != null && markerVisuals.gameObject.activeSelf != visible)
            markerVisuals.gameObject.SetActive(visible);
    }

    private static Color ReadColor(Renderer renderer)
    {
        Material material = renderer.sharedMaterial;
        if (material == null)
            return Color.white;
        return material.HasProperty(BaseColor) ? material.GetColor(BaseColor) : material.color;
    }

    private static void SetOpacity(Renderer renderer, MaterialPropertyBlock properties, Color color, float pulse)
    {
        color.a *= pulse;
        renderer.GetPropertyBlock(properties);
        properties.SetColor(BaseColor, color);
        properties.SetColor(ColorProperty, color);
        renderer.SetPropertyBlock(properties);
    }
}
