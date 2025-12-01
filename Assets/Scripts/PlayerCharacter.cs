using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GPSTracker gpsTracker;
    [SerializeField] private Animator animator;
    [SerializeField] private OSMTerrainLoader osmTerrainLoader;

    [Header("Settings")]
    [SerializeField] private int zoomLevel = 19;
    [SerializeField] private float floatingOriginThreshold = 200f;

    [Header("Movement")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float maxMoveSpeed = 5f;
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private string speedParameterName = "Speed";

    public static bool IsGlobalOriginSet { get; private set; } = false;
    public static double GlobalOriginX { get; private set; }
    public static double GlobalOriginY { get; private set; }
    public static float GlobalTileSizeMeters { get; private set; }

    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private Vector3 lastPosition;
    private float currentSpeed;

    void Start()
    {
        if (gpsTracker == null) gpsTracker = FindObjectOfType<GPSTracker>();
        if (osmTerrainLoader == null) osmTerrainLoader = FindObjectOfType<OSMTerrainLoader>();
        if (animator == null) animator = GetComponent<Animator>();

        targetPosition = transform.position;
        lastPosition = transform.position;

        if (gpsTracker != null)
        {
            gpsTracker.OnGPSUpdate += OnGPSUpdated;
            Debug.Log("[Player] GPS event subscribed");
        }
    }

    void OnGPSUpdated(double lat, double lon, float heading)
    {
        Debug.Log($"[Player] GPS Update: {lat:F6}, {lon:F6}");

        double currentPreciseX, currentPreciseY;
        GPSUtils.GetTileCoordinate(lat, lon, zoomLevel, out currentPreciseX, out currentPreciseY);

        if (!IsGlobalOriginSet)
        {
            GlobalOriginX = currentPreciseX;
            GlobalOriginY = currentPreciseY;
            GlobalTileSizeMeters = GPSUtils.GetTileSizeMeters(lat, zoomLevel);
            IsGlobalOriginSet = true;

            Debug.Log($"[Player] Global Origin Set: ({GlobalOriginX:F5}, {GlobalOriginY:F5}), TileSize: {GlobalTileSizeMeters:F2}m");

            Vector3 zero = new Vector3(0, transform.position.y, 0);
            transform.position = zero;
            targetPosition = zero;
            lastPosition = zero;
            currentVelocity = Vector3.zero;
            return;
        }

        if (targetPosition.magnitude > floatingOriginThreshold)
        {
            ShiftWorld(currentPreciseX, currentPreciseY);
            return;
        }

        double xDiff = (GlobalOriginX - currentPreciseX) * GlobalTileSizeMeters;
        double zDiff = (currentPreciseY - GlobalOriginY) * GlobalTileSizeMeters;

        targetPosition = new Vector3((float)xDiff, transform.position.y, (float)zDiff);
    }

    void ShiftWorld(double newX, double newY)
    {
        Debug.Log($"[Player] >> World Shift << from ({GlobalOriginX:F5}, {GlobalOriginY:F5}) to ({newX:F5}, {newY:F5})");

        float shiftX = (float)((GlobalOriginX - newX) * GlobalTileSizeMeters);
        float shiftZ = (float)((newY - GlobalOriginY) * GlobalTileSizeMeters);
        Vector3 shiftVector = new Vector3(shiftX, 0, shiftZ);

        GlobalOriginX = newX;
        GlobalOriginY = newY;

        transform.position -= shiftVector;
        targetPosition -= shiftVector;
        lastPosition -= shiftVector;

        if (osmTerrainLoader != null)
        {
            osmTerrainLoader.OnWorldShift(shiftVector);
        }
    }

    void Update()
    {
        if (!IsGlobalOriginSet) return;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime, maxMoveSpeed);

        if (gpsTracker != null)
        {
            float rawHeading = gpsTracker.CurrentHeading;
            Quaternion targetQuaternion = Quaternion.Euler(0f, rawHeading + 180f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetQuaternion, Time.deltaTime * rotationSpeed);
        }

        currentSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        if (animator != null)
        {
            float animValue = (currentSpeed > 0.01f) ? 1.0f : 0f;
            animator.SetFloat(speedParameterName, animValue, 0.1f, Time.deltaTime);
        }
    }

    void OnDestroy()
    {
        if (gpsTracker != null)
        {
            gpsTracker.OnGPSUpdate -= OnGPSUpdated;
        }
    }

    //void OnGUI()
    //{
    //    GUIStyle style = new GUIStyle();
    //    style.fontSize = 30;
    //    style.normal.textColor = Color.cyan;

    //    string debugInfo = $"[Player Debug]\n" +
    //                      $"Origin Set: {IsGlobalOriginSet}\n" +
    //                      $"Position: {transform.position}\n" +
    //                      $"Target: {targetPosition}\n" +
    //                      $"Speed: {currentSpeed:F2} m/s";

    //    GUI.Label(new Rect(50, 50, 800, 300), debugInfo, style);
    //}
}