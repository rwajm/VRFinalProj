using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class GPSTracker : MonoBehaviour
{
    [Header("GPS Settings")]
    private readonly float updateInterval = 0.5f;

    [Header("Editor Test Settings")]
    [SerializeField] private float testWalkSpeed = 4f;
    [SerializeField] private double testLatitude = 37.5826;
    [SerializeField] private double testLongitude = 127.0099;

    public double CurrentLatitude { get; private set; }
    public double CurrentLongitude { get; private set; }
    public float CurrentHeading { get; private set; }

    public delegate void OnGPSUpdateDelegate(double lat, double lon, float heading);
    public event OnGPSUpdateDelegate OnGPSUpdate;

    private string statusMessage = "Initializing...";
    private string compassDebug = "Compass: Waiting...";
    private double lastTimestamp = 0;

    private const double METERS_PER_LAT = 111319.9;
    private double metersPerLon;

    private bool isInitialized = false;

    void Start()
    {
        Input.compass.enabled = true;
        metersPerLon = METERS_PER_LAT * Mathf.Cos((float)(testLatitude * Mathf.Deg2Rad));

        if (Application.isEditor)
        {
            CurrentLatitude = testLatitude;
            CurrentLongitude = testLongitude;
            statusMessage = "Editor Mode";

            isInitialized = true;
            StartCoroutine(EditorTestLoop());
        }
        else
        {
            StartCoroutine(InitializeGPS());
        }
    }

    void Update()
    {
        if (!Application.isEditor)
        {
            float trueHead = Input.compass.trueHeading;
            float magHead = Input.compass.magneticHeading;

            compassDebug = $"Raw True: {trueHead:F1} / Mag: {magHead:F1}\nAccuracy: {Input.compass.headingAccuracy}";

            float targetHeading = (trueHead > 0) ? trueHead : magHead;
            CurrentHeading = Mathf.MoveTowardsAngle(CurrentHeading, targetHeading, Time.deltaTime * 100f);
        }

        if (isInitialized && Time.frameCount == 2)
        {
            Debug.Log("[GPS] Sending initial update after all Start() completed");
            OnGPSUpdate?.Invoke(CurrentLatitude, CurrentLongitude, CurrentHeading);
        }
    }

    //void OnGUI()
    //{
    //    GUIStyle style = new GUIStyle();
    //    style.fontSize = 45;
    //    style.normal.textColor = Color.green;

    //    string displayMsg = $"{statusMessage}\nLat: {CurrentLatitude:F6}\nLon: {CurrentLongitude:F6}\nLast GPS Update: {lastTimestamp:F1}\n{compassDebug}\nFinal Heading: {CurrentHeading:F1}";
    //    GUI.Label(new Rect(50, 200, 1000, 800), displayMsg, style);

    //    if (GUI.Button(new Rect(50, 1000, 400, 150), "<size=40>Force Update</size>"))
    //    {
    //        ForceUpdateEvent();
    //    }
    //}

    IEnumerator EditorTestLoop()
    {
        while (true)
        {
            yield return null;
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");

            if (moveX != 0 || moveY != 0)
            {
                double latChange = (moveY * testWalkSpeed * Time.deltaTime) / METERS_PER_LAT;
                double lonChange = (moveX * testWalkSpeed * Time.deltaTime) / metersPerLon;
                CurrentLatitude += latChange;
                CurrentLongitude += lonChange;

                float targetHeading = Mathf.Atan2(moveX, moveY) * Mathf.Rad2Deg;
                if (targetHeading < 0) targetHeading += 360;
                CurrentHeading = Mathf.LerpAngle(CurrentHeading, targetHeading, Time.deltaTime * 5f);

                lastTimestamp = Time.time;
                OnGPSUpdate?.Invoke(CurrentLatitude, CurrentLongitude, CurrentHeading);
            }
        }
    }

    IEnumerator InitializeGPS()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(1.0f);
        }
#endif
        if (!Input.location.isEnabledByUser)
        {
            statusMessage = "GPS Disabled! Turn it on.";
            yield break;
        }

        Input.location.Start(5f, 0.1f);

        int maxWait = 15;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            statusMessage = $"Waiting GPS... {maxWait}";
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            statusMessage = "GPS Failed. Compass should still work.";
        }
        else
        {
            statusMessage = "GPS Connected!";
            isInitialized = true;
            StartCoroutine(GPSLoop());
        }
    }

    IEnumerator GPSLoop()
    {
        while (true)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                CurrentLatitude = Input.location.lastData.latitude;
                CurrentLongitude = Input.location.lastData.longitude;
                lastTimestamp = Input.location.lastData.timestamp;

                OnGPSUpdate?.Invoke(CurrentLatitude, CurrentLongitude, CurrentHeading);
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    void ForceUpdateEvent()
    {
        statusMessage = "Forced Update";
        lastTimestamp = Time.time;
        OnGPSUpdate?.Invoke(CurrentLatitude, CurrentLongitude, CurrentHeading);
    }
}