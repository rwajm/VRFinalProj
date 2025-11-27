using System.Collections;
using UnityEngine;

public class GPSTracker : MonoBehaviour
{
    [Header("GPS Settings")]
    [SerializeField] private float updateInterval = 1f;

    [Header("Editor Test Settings")]
    [SerializeField] private bool useEditorTestMode = true;
    [SerializeField] private double testLatitude = 37.5826;
    [SerializeField] private double testLongitude = 127.0099;
    [SerializeField] private float testHeading = 0f;  // 0=북, 90=동, 180=남, 270=서

    // 현재 GPS 데이터
    public double CurrentLatitude { get; private set; }
    public double CurrentLongitude { get; private set; }
    public float CurrentHeading { get; private set; }

    // 이벤트: 다른 스크립트에서 GPS 업데이트를 감지할 수 있음
    public delegate void OnGPSUpdateDelegate(double lat, double lon, float heading);
    public event OnGPSUpdateDelegate OnGPSUpdate;

    void Start()
    {
        // Compass 활성화 (방향 감지)
        Input.compass.enabled = true;

        StartCoroutine(InitializeGPS());
    }

    IEnumerator InitializeGPS()
    {
#if UNITY_EDITOR
        if (useEditorTestMode)
        {
            Debug.Log("GPS 트래커: 에디터 테스트 모드");
            CurrentLatitude = testLatitude;
            CurrentLongitude = testLongitude;
            CurrentHeading = testHeading;

            OnGPSUpdate?.Invoke(CurrentLatitude, CurrentLongitude, CurrentHeading);

            // 에디터에서도 주기적 업데이트 (테스트용)
            InvokeRepeating(nameof(UpdateTestData), updateInterval, updateInterval);
            yield break;
        }
#endif

        // GPS 권한 확인
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("GPS가 비활성화되어 있습니다.");
            yield break;
        }

        // GPS 서비스 시작
        Input.location.Start(1f, 1f);

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1)
        {
            Debug.LogError("GPS 초기화 타임아웃");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("GPS 위치를 가져올 수 없습니다.");
            yield break;
        }

        Debug.Log("GPS 트래커 초기화 완료");

        // 주기적 업데이트
        InvokeRepeating(nameof(UpdateGPSData), 0f, updateInterval);
    }

    void UpdateGPSData()
    {
        if (Input.location.status != LocationServiceStatus.Running)
            return;

        CurrentLatitude = Input.location.lastData.latitude;
        CurrentLongitude = Input.location.lastData.longitude;
        CurrentHeading = Input.compass.trueHeading;  // 진북 기준

        Debug.Log($"GPS 업데이트: 위도 {CurrentLatitude}, 경도 {CurrentLongitude}, 방향 {CurrentHeading}°");

        OnGPSUpdate?.Invoke(CurrentLatitude, CurrentLongitude, CurrentHeading);
    }

    void UpdateTestData()
    {
        CurrentLatitude = testLatitude;
        CurrentLongitude = testLongitude;
        CurrentHeading = testHeading;

        OnGPSUpdate?.Invoke(CurrentLatitude, CurrentLongitude, CurrentHeading);
    }

    void OnDestroy()
    {
        if (Input.location.isEnabledByUser)
        {
            Input.location.Stop();
        }

        Input.compass.enabled = false;
    }

    // 외부에서 현재 GPS 데이터를 가져오는 메서드
    public void GetCurrentGPSData(out double lat, out double lon, out float heading)
    {
        lat = CurrentLatitude;
        lon = CurrentLongitude;
        heading = CurrentHeading;
    }
}