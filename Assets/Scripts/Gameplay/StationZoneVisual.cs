using UnityEngine;
using UnityEngine.UI;

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
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private void Awake()
    {
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
            ? playerFuel != null && playerFuel.currentFuel < playerFuel.maxFuel
            : playerHealth != null && playerHealth.currentHealth < playerHealth.maxHealth;
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
