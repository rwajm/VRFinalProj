using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class MapTileLoader : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int zoomLevel = 16;
    [SerializeField] private GameObject mapPlane;

    [Header("References")]
    [SerializeField] private GPSTracker gpsTracker;

    private Material mapMaterial;
    private int currentTileX;
    private int currentTileY;

    void Start()
    {
        // GPSTracker 자동 찾기
        if (gpsTracker == null)
        {
            gpsTracker = FindObjectOfType<GPSTracker>();
        }

        // Material 생성 및 설정
        if (mapPlane != null)
        {
            Renderer renderer = mapPlane.GetComponent<Renderer>();
            mapMaterial = new Material(Shader.Find("Unlit/Texture"));
            renderer.material = mapMaterial;
        }

        // GPS 업데이트 이벤트 구독
        if (gpsTracker != null)
        {
            gpsTracker.OnGPSUpdate += OnGPSUpdated;
        }
    }

    void OnGPSUpdated(double lat, double lon, float heading)
    {
        // 타일 좌표 계산
        int tileX = LatLonToTileX(lon, zoomLevel);
        int tileY = LatLonToTileY(lat, zoomLevel);

        // 타일이 변경된 경우에만 다운로드
        if (tileX != currentTileX || tileY != currentTileY)
        {
            currentTileX = tileX;
            currentTileY = tileY;

            StartCoroutine(LoadMapTile(tileX, tileY));
        }
    }

    // 경도를 타일 X 좌표로 변환
    int LatLonToTileX(double lon, int zoom)
    {
        return (int)((lon + 180.0) / 360.0 * (1 << zoom));
    }

    // 위도를 타일 Y 좌표로 변환
    int LatLonToTileY(double lat, int zoom)
    {
        double latRad = lat * Mathf.Deg2Rad;
        return (int)((1.0 - Mathf.Log(Mathf.Tan((float)latRad) + 1.0f / Mathf.Cos((float)latRad)) / Mathf.PI) / 2.0 * (1 << zoom));
    }

    IEnumerator LoadMapTile(int tileX, int tileY)
    {
        string url = $"https://tile.openstreetmap.org/{zoomLevel}/{tileX}/{tileY}.png";
        Debug.Log($"타일 다운로드 중: {url}");

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            // OpenStreetMap 사용 정책: User-Agent 설정
            request.SetRequestHeader("User-Agent", "UnityMapApp/1.0");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);

                if (mapMaterial != null)
                {
                    mapMaterial.mainTexture = texture;
                    Debug.Log("타일 로드 완료");
                }
            }
            else
            {
                Debug.LogError($"타일 다운로드 실패: {request.error}");
            }
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (gpsTracker != null)
        {
            gpsTracker.OnGPSUpdate -= OnGPSUpdated;
        }
    }

    // 디버그용: 수동으로 특정 위치의 타일 로드
    public void LoadTileAtCoordinates(double lat, double lon)
    {
        int tileX = LatLonToTileX(lon, zoomLevel);
        int tileY = LatLonToTileY(lat, zoomLevel);
        StartCoroutine(LoadMapTile(tileX, tileY));
    }
}
