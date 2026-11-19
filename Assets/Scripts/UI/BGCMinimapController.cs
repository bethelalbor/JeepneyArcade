using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Moves an existing PNG map beneath a player marker. PNG north must point up.
/// Configure the circular Image/Mask on MapViewport in the Inspector.
/// </summary>
[DisallowMultipleComponent]
public sealed class BGCMinimapController : MonoBehaviour
{
    [Serializable]
    private struct LandmarkPair
    {
        [Tooltip("Measured Unity world coordinates: X = world X, Y = world Z.")]
        public Vector2 worldXZ;

        [Tooltip("Matching PNG coordinates: X = U (left 0, right 1), Y = V (bottom 0, top 1).")]
        public Vector2 imageUV;
    }

    [Header("Existing Objects")]
    [SerializeField] private Transform player;
    [SerializeField] private RectTransform mapViewport;
    [SerializeField] private RectTransform mapContent;
    [SerializeField] private RectTransform playerMarker;

    [Header("Service Locations")]
    [SerializeField] private Transform fuelStation;
    [SerializeField] private Transform repairStation;
    [SerializeField] private RectTransform fuelMarker;
    [SerializeField] private RectTransform repairMarker;
    [SerializeField] private RectTransform fuelEdgeMarker;
    [SerializeField] private RectTransform repairEdgeMarker;
    [SerializeField] private RectTransform markerLayer;
    [SerializeField] private RectTransform edgeIndicators;

    [Header("Seated Passenger Destinations")]
    [SerializeField] private PassengerManager passengerManager;
    [SerializeField] private Sprite destinationDotSprite;
    [SerializeField] private Vector2 destinationMarkerSize = new Vector2(18f, 18f);

    [Header("Map Size")]
    [SerializeField, Min(1f)] private float displayedHeight = 400f;

    [Header("Three Measured Landmark Pairs")]
    [SerializeField] private LandmarkPair landmark1;
    [SerializeField] private LandmarkPair landmark2;
    [SerializeField] private LandmarkPair landmark3;

    private Vector2 uvPerWorldX;
    private Vector2 uvPerWorldZ;
    private float spriteAspect;
    private bool needsConfiguration = true;
    private bool ready;
    private bool warningIssued;

    private struct StationCache
    {
        public Transform station;
        public Vector3 worldPosition;
        public Vector2 imageUV;
        public bool calibrated;
        public bool warned;
    }

    private StationCache fuelCache;
    private StationCache repairCache;
    private readonly Vector3[] markerCorners = new Vector3[4];

    // Presentation pool only. PassengerManager remains the source of passenger state.
    private sealed class DestinationMarker
    {
        public DestinationPoint destination;
        public Image mapImage;
        public Image edgeImage;
        public StationCache cache;
    }

    private readonly List<DestinationMarker> destinationMarkers = new List<DestinationMarker>();
    private readonly HashSet<DestinationPoint> seatedDestinations = new HashSet<DestinationPoint>();
    private PassengerManager subscribedPassengerManager;
    private bool destinationsDirty = true;
    private bool destinationWarningIssued;

    private void OnEnable()
    {
        needsConfiguration = true;
        destinationsDirty = true;
    }

    private void OnValidate()
    {
        needsConfiguration = true;
        destinationsDirty = true;
    }

    private void OnDisable()
    {
        if (subscribedPassengerManager != null)
            subscribedPassengerManager.PassengersOnBoardChanged -= MarkDestinationsDirty;
        subscribedPassengerManager = null;
        HideDestinationMarkers();
    }

    private void OnDestroy()
    {
        // Destroy only the runtime UI objects owned by this controller.
        for (int i = 0; i < destinationMarkers.Count; i++)
        {
            DestinationMarker marker = destinationMarkers[i];
            if (marker.mapImage != null)
                Destroy(marker.mapImage.gameObject);
            if (marker.edgeImage != null)
                Destroy(marker.edgeImage.gameObject);
        }
    }

    private void LateUpdate()
    {
        BindPassengerManager();
        if (needsConfiguration)
        {
            needsConfiguration = false;
            fuelCache.calibrated = false;
            repairCache.calibrated = false;
            for (int i = 0; i < destinationMarkers.Count; i++)
                destinationMarkers[i].cache.calibrated = false;
            ready = Configure();
        }

        // Invalid or absent calibration must never produce guessed movement.
        if (!ready || player == null || mapViewport == null ||
            mapContent == null || playerMarker == null)
        {
            HideDestinationMarkers();
            return;
        }

        Vector2 mapSize = new Vector2(displayedHeight * spriteAspect, displayedHeight);
        if (mapContent.sizeDelta != mapSize)
            mapContent.sizeDelta = mapSize;

        Vector3 worldPosition = player.position;
        Vector2 uv = WorldToImageUV(worldPosition);
        Vector2 mapPoint = new Vector2((uv.x - 0.5f) * mapSize.x,
                                     (uv.y - 0.5f) * mapSize.y);

        Vector2 viewportSize = mapViewport.rect.size;
        Vector2 travel = new Vector2(Mathf.Max(0f, (mapSize.x - viewportSize.x) * 0.5f),
                                     Mathf.Max(0f, (mapSize.y - viewportSize.y) * 0.5f));
        Vector2 translation = new Vector2(Mathf.Clamp(-mapPoint.x, -travel.x, travel.x),
                                          Mathf.Clamp(-mapPoint.y, -travel.y, travel.y));
        mapContent.anchoredPosition = translation;

        // PlayerMarker is a sibling of the viewport, so convert through world UI space.
        // Do not clamp the marker separately: that would falsify its map position.
        Vector2 markerPoint = mapViewport.rect.center + mapPoint + translation;
        playerMarker.position = mapViewport.TransformPoint(new Vector3(markerPoint.x, markerPoint.y, 0f));

        Vector3 forward = player.forward;
        Vector2 headingUV = uvPerWorldX * forward.x + uvPerWorldZ * forward.z;
        Vector2 headingUI = new Vector2(headingUV.x * mapSize.x, headingUV.y * mapSize.y);
        if (headingUI.sqrMagnitude > 0.00000001f)
        {
            float angle = Mathf.Atan2(headingUI.y, headingUI.x) * Mathf.Rad2Deg - 90f;
            playerMarker.rotation = mapViewport.rotation * Quaternion.Euler(0f, 0f, angle);
        }

        UpdateStation(fuelStation, fuelMarker, fuelEdgeMarker, ref fuelCache, "Fuel Station");
        UpdateStation(repairStation, repairMarker, repairEdgeMarker, ref repairCache, "Repair Station");
        UpdateDestinationMarkers();
    }

    private void BindPassengerManager()
    {
        if (subscribedPassengerManager == passengerManager)
            return;
        if (subscribedPassengerManager != null)
            subscribedPassengerManager.PassengersOnBoardChanged -= MarkDestinationsDirty;
        subscribedPassengerManager = passengerManager;
        if (subscribedPassengerManager != null)
            subscribedPassengerManager.PassengersOnBoardChanged += MarkDestinationsDirty;
        destinationsDirty = true;
    }

    private void MarkDestinationsDirty()
    {
        destinationsDirty = true;
    }

    private void UpdateDestinationMarkers()
    {
        if (passengerManager == null || destinationDotSprite == null ||
            markerLayer == null || edgeIndicators == null || markerLayer.parent != mapContent ||
            edgeIndicators.IsChildOf(mapContent) || !IsFinite(destinationMarkerSize) ||
            destinationMarkerSize.x <= 0f || destinationMarkerSize.y <= 0f)
        {
            HideDestinationMarkers();
            destinationsDirty = true;
            if (!destinationWarningIssued)
            {
                Debug.LogWarning("BGCMinimapController: Assign the player's Passenger Manager, Destination Dot Sprite, a positive Destination Marker Size, MarkerLayer under MapContent, and EdgeIndicators outside MapContent. Passenger destination markers are skipped.", this);
                destinationWarningIssued = true;
            }
            return;
        }

        if (destinationsDirty)
        {
            SynchronizeDestinations();
            destinationsDirty = false;
        }

        for (int i = 0; i < destinationMarkers.Count; i++)
        {
            DestinationMarker marker = destinationMarkers[i];
            if (marker.destination == null)
            {
                HideDestinationMarker(marker);
                continue;
            }

            // Recover missing owned UI references without affecting existing map objects.
            if (marker.mapImage == null)
                marker.mapImage = CreateDestinationImage("Passenger Destination Dot", markerLayer);
            if (marker.edgeImage == null)
                marker.edgeImage = CreateDestinationImage("Passenger Destination Edge Dot", edgeIndicators);
            ConfigureDestinationImage(marker.mapImage, markerLayer);
            ConfigureDestinationImage(marker.edgeImage, edgeIndicators);

            // Reuse exactly the service markers' calibration, visibility, and edge placement.
            // DestinationPoint is the drop-off destination; never use its walkPoint.
            UpdateStation(marker.destination.transform, marker.mapImage.rectTransform,
                marker.edgeImage.rectTransform, ref marker.cache, "Passenger Destination");
        }
    }

    private void SynchronizeDestinations()
    {
        seatedDestinations.Clear();
        List<GameObject> passengers = passengerManager.passengersOnBoard;
        if (passengers != null)
        {
            for (int i = 0; i < passengers.Count; i++)
            {
                GameObject passenger = passengers[i];
                if (passenger == null)
                    continue;
                PassengerData data = passenger.GetComponent<PassengerData>();
                if (data != null && data.isOnVehicle && data.destination != null)
                    seatedDestinations.Add(data.destination);
            }
        }

        // Retain markers that still have seated passengers and free the rest for reuse.
        for (int i = 0; i < destinationMarkers.Count; i++)
        {
            DestinationMarker marker = destinationMarkers[i];
            if (marker.destination != null && seatedDestinations.Remove(marker.destination))
                continue;
            marker.destination = null;
            marker.cache = default(StationCache);
            HideDestinationMarker(marker);
        }

        foreach (DestinationPoint destination in seatedDestinations)
        {
            DestinationMarker freeMarker = null;
            for (int i = 0; i < destinationMarkers.Count; i++)
            {
                if (destinationMarkers[i].destination == null)
                {
                    freeMarker = destinationMarkers[i];
                    break;
                }
            }
            if (freeMarker == null)
            {
                freeMarker = new DestinationMarker();
                destinationMarkers.Add(freeMarker);
            }
            freeMarker.destination = destination;
            freeMarker.cache = default(StationCache);
        }
        seatedDestinations.Clear();
    }

    private Image CreateDestinationImage(string objectName, RectTransform parent)
    {
        // Runtime UI children only: no scene assets, prefabs, or gameplay objects.
        GameObject markerObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        markerObject.SetActive(false);
        markerObject.layer = parent.gameObject.layer;
        Image image = markerObject.GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        image.raycastTarget = false;
        image.maskable = true;
        image.preserveAspect = true;
        ConfigureDestinationImage(image, parent);
        return image;
    }

    private void ConfigureDestinationImage(Image image, RectTransform parent)
    {
        if (image.rectTransform.parent != parent)
            image.rectTransform.SetParent(parent, false);
        if (image.sprite != destinationDotSprite)
            image.sprite = destinationDotSprite;
        if (image.rectTransform.sizeDelta != destinationMarkerSize)
            image.rectTransform.sizeDelta = destinationMarkerSize;
    }

    private void HideDestinationMarkers()
    {
        for (int i = 0; i < destinationMarkers.Count; i++)
            HideDestinationMarker(destinationMarkers[i]);
    }

    private static void HideDestinationMarker(DestinationMarker marker)
    {
        if (marker.mapImage != null)
            SetMarkerVisible(marker.mapImage.rectTransform, false);
        if (marker.edgeImage != null)
            SetMarkerVisible(marker.edgeImage.rectTransform, false);
    }

    private Vector2 WorldToImageUV(Vector3 worldPosition)
    {
        Vector2 delta = new Vector2(worldPosition.x, worldPosition.z) - landmark1.worldXZ;
        return landmark1.imageUV + uvPerWorldX * delta.x + uvPerWorldZ * delta.y;
    }

    private void UpdateStation(Transform station, RectTransform marker, RectTransform edgeMarker,
                               ref StationCache cache, string stationName)
    {
        if (station == null || marker == null || edgeMarker == null ||
            markerLayer == null || edgeIndicators == null)
        {
            SetMarkerVisible(marker, false);
            SetMarkerVisible(edgeMarker, false);
            WarnStation(ref cache, stationName, "Assign its station Transform, both icons, Marker Layer, and Edge Indicators. Only this station is skipped.");
            return;
        }

        if (marker.parent != markerLayer || markerLayer.parent != mapContent ||
            edgeMarker.parent != edgeIndicators || edgeIndicators.IsChildOf(mapContent))
        {
            WarnStation(ref cache, stationName, "Place its normal icon under MarkerLayer under MapContent, and its edge icon under EdgeIndicators outside MapContent. Only this station is skipped.");
            return;
        }

        Vector3 world = station.position;
        // Station UVs are cached; replacing/moving a station or recalibrating invalidates them.
        if (!cache.calibrated || cache.station != station || !cache.worldPosition.Equals(world))
        {
            cache.station = station;
            cache.worldPosition = world;
            cache.imageUV = WorldToImageUV(world);
            cache.calibrated = true;
        }
        if (!IsFinite(cache.imageUV))
        {
            SetMarkerVisible(marker, false);
            SetMarkerVisible(edgeMarker, false);
            WarnStation(ref cache, stationName, "Its calibrated position is not finite. Only this station is skipped.");
            return;
        }

        Rect mapRect = mapContent.rect;
        Vector3 mapPoint = new Vector3(mapRect.xMin + cache.imageUV.x * mapRect.width,
                                      mapRect.yMin + cache.imageUV.y * mapRect.height, 0f);
        // Place the visual center, accounting for arbitrary icon pivots and layer transforms.
        marker.rotation = mapViewport.rotation;
        PositionMarkerCenter(marker, mapContent.TransformPoint(mapPoint));

        Rect viewportRect = mapViewport.rect;
        Vector2 center = viewportRect.center;
        float radius = Mathf.Min(viewportRect.width, viewportRect.height) * 0.5f;
        Vector2 location = mapViewport.InverseTransformPoint(marker.TransformPoint(marker.rect.center));
        Vector2 direction = location - center;

        // Require the whole icon to fit, so a normal marker never straddles the frame.
        bool visible = radius > 0f;
        marker.GetWorldCorners(markerCorners);
        for (int i = 0; i < markerCorners.Length && visible; i++)
        {
            Vector2 corner = mapViewport.InverseTransformPoint(markerCorners[i]);
            visible = (corner - center).sqrMagnitude <= radius * radius;
        }
        SetMarkerVisible(marker, visible);
        if (visible)
        {
            SetMarkerVisible(edgeMarker, false);
            return;
        }

        // Keep edge icons upright. Transform their corners into viewport space to account
        // for scale, rotation, and pivots in either UI hierarchy without allocating arrays.
        edgeMarker.rotation = mapViewport.rotation;
        PositionMarkerCenter(edgeMarker, mapViewport.TransformPoint(new Vector3(center.x, center.y, 0f)));
        edgeMarker.GetWorldCorners(markerCorners);
        Vector2 heading = direction.normalized;
        float distance = radius;
        bool fits = radius > 0f && heading.sqrMagnitude > 0f;
        for (int i = 0; i < markerCorners.Length && fits; i++)
        {
            Vector2 corner = mapViewport.InverseTransformPoint(markerCorners[i]);
            Vector2 offset = corner - center;
            if (offset.sqrMagnitude > radius * radius)
            {
                fits = false;
                break;
            }

            // Largest distance along the station direction that keeps this corner inside.
            float projection = Vector2.Dot(heading, offset);
            float limit = -projection + Mathf.Sqrt(Mathf.Max(0f,
                projection * projection + radius * radius - offset.sqrMagnitude));
            distance = Mathf.Min(distance, limit);
        }

        if (!fits)
        {
            SetMarkerVisible(edgeMarker, false);
            WarnStation(ref cache, stationName, "Its icons cannot fit inside the circular viewport. Reduce their UI size. Only this station's edge indicator is skipped.");
            return;
        }

        Vector2 edgePoint = center + heading * Mathf.Max(0f, distance);
        PositionMarkerCenter(edgeMarker, mapViewport.TransformPoint(new Vector3(edgePoint.x, edgePoint.y, 0f)));
        SetMarkerVisible(edgeMarker, true);
    }

    private static void PositionMarkerCenter(RectTransform marker, Vector3 worldCenter)
    {
        marker.position = worldCenter - marker.TransformVector(marker.rect.center);
    }

    private static void SetMarkerVisible(RectTransform marker, bool visible)
    {
        if (marker != null && marker.gameObject.activeSelf != visible)
            marker.gameObject.SetActive(visible);
    }

    private void WarnStation(ref StationCache cache, string stationName, string message)
    {
        if (cache.warned)
            return;
        Debug.LogWarning("BGCMinimapController — " + stationName + ": " + message, this);
        cache.warned = true;
    }

    private bool Configure()
    {
        if (player == null || mapViewport == null || mapContent == null || playerMarker == null)
            return Warn("Assign Player, Map Viewport, Map Content, and Player Marker. The minimap will remain stationary.");

        if (mapContent.parent != mapViewport)
            return Warn("MapContent must be a direct child of MapViewport. The minimap will remain stationary.");

        Image mapImage = mapContent.GetComponent<Image>();
        Sprite sprite = mapImage != null ? mapImage.overrideSprite : null;
        if (sprite == null || sprite.rect.width <= 0f || sprite.rect.height <= 0f ||
            !IsFinite(displayedHeight) || displayedHeight <= 0f)
            return Warn("Assign the PNG sprite to MapContent's Image and a positive Displayed Height. The minimap will remain stationary.");

        spriteAspect = sprite.rect.width / sprite.rect.height;
        mapContent.anchorMin = new Vector2(0.5f, 0.5f);
        mapContent.anchorMax = new Vector2(0.5f, 0.5f);
        mapContent.pivot = new Vector2(0.5f, 0.5f);
        mapContent.localScale = Vector3.one;
        mapContent.localRotation = Quaternion.identity;
        mapContent.sizeDelta = new Vector2(displayedHeight * spriteAspect, displayedHeight);

        if (!ValidPair(landmark1) || !ValidPair(landmark2) || !ValidPair(landmark3))
            return Warn("Supply exactly three measured landmark pairs with finite Unity X/Z and PNG U/V in [0, 1]. The minimap will remain stationary.");

        Vector2 a = landmark2.worldXZ - landmark1.worldXZ;
        Vector2 b = landmark3.worldXZ - landmark1.worldXZ;
        double determinant = (double)a.x * b.y - (double)a.y * b.x;
        double lengthProduct = Math.Sqrt(((double)a.x * a.x + (double)a.y * a.y) *
                                         ((double)b.x * b.x + (double)b.y * b.y));
        if (lengthProduct == 0.0 || Math.Abs(determinant) <= lengthProduct * 0.000001)
            return Warn("Calibration is missing or its Unity landmarks are collinear. Enter three distinct, non-collinear measured landmark pairs. The minimap will remain stationary.");

        Vector2 imageA = landmark2.imageUV - landmark1.imageUV;
        Vector2 imageB = landmark3.imageUV - landmark1.imageUV;
        double imageDeterminant = (double)imageA.x * imageB.y - (double)imageA.y * imageB.x;
        double imageLengthProduct = Math.Sqrt(((double)imageA.x * imageA.x + (double)imageA.y * imageA.y) *
                                              ((double)imageB.x * imageB.x + (double)imageB.y * imageB.y));
        if (imageLengthProduct == 0.0 || Math.Abs(imageDeterminant) <= imageLengthProduct * 0.000001)
            return Warn("The three PNG landmarks must also be distinct and non-collinear. The minimap will remain stationary.");

        // Solve [a b] in world X/Z against the corresponding UV differences.
        uvPerWorldX = new Vector2((float)(((double)imageA.x * b.y - (double)imageB.x * a.y) / determinant),
                                 (float)(((double)imageA.y * b.y - (double)imageB.y * a.y) / determinant));
        uvPerWorldZ = new Vector2((float)(((double)imageB.x * a.x - (double)imageA.x * b.x) / determinant),
                                 (float)(((double)imageB.y * a.x - (double)imageA.y * b.x) / determinant));
        if (!IsFinite(uvPerWorldX) || !IsFinite(uvPerWorldZ))
            return Warn("Calibration produced an invalid affine mapping. Check the measured landmark pairs. The minimap will remain stationary.");

        if (mapContent.sizeDelta.x < mapViewport.rect.width ||
            mapContent.sizeDelta.y < mapViewport.rect.height)
            return Warn("Displayed Height is too small to cover MapViewport without empty space. Increase it until both map dimensions cover the viewport. The minimap will remain stationary.");

        return true;
    }

    private bool Warn(string message)
    {
        if (!warningIssued)
        {
            Debug.LogWarning("BGCMinimapController: " + message, this);
            warningIssued = true;
        }
        return false;
    }

    private static bool ValidPair(LandmarkPair pair)
    {
        return IsFinite(pair.worldXZ) && IsFinite(pair.imageUV) &&
               pair.imageUV.x >= 0f && pair.imageUV.x <= 1f &&
               pair.imageUV.y >= 0f && pair.imageUV.y <= 1f;
    }

    private static bool IsFinite(Vector2 value)
    {
        return IsFinite(value.x) && IsFinite(value.y);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
