using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Mode")]
    [SerializeField] private CameraMode mode = CameraMode.FollowHeading;

    [Header("References")]
    [SerializeField] private GPSTracker gpsTracker;

    [Header("Follow Heading Settings")]
    [SerializeField] private float followRotationSpeed = 5f;

    [Header("Manual Control Settings")]
    [SerializeField] private float manualRotationSpeed = 100f;
    [SerializeField] private float touchSensitivity = 0.5f;

    private float targetRotation = 0f;
    private float currentManualRotation = 0f;

    public enum CameraMode
    {
        FollowHeading,  // 방향 자동 따라가기
        Manual          // 사용자가 직접 회전
    }

    void Start()
    {
        if (gpsTracker == null)
        {
            gpsTracker = FindObjectOfType<GPSTracker>();
        }
    }

    void Update()
    {
        if (mode == CameraMode.FollowHeading)
        {
            UpdateFollowHeading();
        }
        else if (mode == CameraMode.Manual)
        {
            UpdateManualControl();
        }
    }

    void UpdateFollowHeading()
    {
        if (gpsTracker == null)
            return;

        // GPS heading을 따라 카메라 회전
        targetRotation = gpsTracker.CurrentHeading;

        // 부드러운 회전
        Quaternion targetQuaternion = Quaternion.Euler(90f, targetRotation, 0f);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetQuaternion,
            Time.deltaTime * followRotationSpeed
        );
    }

    void UpdateManualControl()
    {
        // 터치/마우스 입력으로 회전
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float deltaX = touch.deltaPosition.x * touchSensitivity;
                currentManualRotation += deltaX;
            }
        }
        // 에디터 테스트: 마우스 드래그
        else if (Input.GetMouseButton(0))
        {
            float deltaX = Input.GetAxis("Mouse X") * manualRotationSpeed * Time.deltaTime;
            currentManualRotation += deltaX;
        }

        // 회전 적용
        Quaternion targetQuaternion = Quaternion.Euler(90f, currentManualRotation, 0f);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetQuaternion,
            Time.deltaTime * followRotationSpeed
        );
    }

    // 외부에서 모드 전환
    public void SetCameraMode(CameraMode newMode)
    {
        mode = newMode;

        if (mode == CameraMode.Manual)
        {
            // Manual 모드로 전환 시 현재 회전값 저장
            currentManualRotation = transform.rotation.eulerAngles.y;
        }
    }

    // 현재 모드 확인
    public CameraMode GetCurrentMode()
    {
        return mode;
    }

    // 모드 토글 (UI 버튼용)
    public void ToggleCameraMode()
    {
        if (mode == CameraMode.FollowHeading)
        {
            SetCameraMode(CameraMode.Manual);
            Debug.Log("카메라 모드: 수동 제어");
        }
        else
        {
            SetCameraMode(CameraMode.FollowHeading);
            Debug.Log("카메라 모드: 방향 자동 추적");
        }
    }
}