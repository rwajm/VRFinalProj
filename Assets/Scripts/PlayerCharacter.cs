using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GPSTracker gpsTracker;

    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float movementSpeed = 5f;

    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private string speedParameterName = "Speed";
    [SerializeField] private float idleThreshold = 0.1f;  // 이 속도 이하면 Idle

    private Animator animator;
    private float targetRotation = 0f;
    private Vector3 lastPosition;
    private float currentSpeed = 0f;

    void Start()
    {
        if (gpsTracker == null)
        {
            gpsTracker = FindObjectOfType<GPSTracker>();
        }

        if (gpsTracker != null)
        {
            gpsTracker.OnGPSUpdate += OnGPSUpdated;
        }

        // Animator 가져오기
        animator = GetComponent<Animator>();

        lastPosition = transform.position;
    }

    void OnGPSUpdated(double lat, double lon, float heading)
    {
        // 방향 업데이트
        targetRotation = heading;

        // TODO: GPS 좌표를 Unity 월드 좌표로 변환하여 위치 업데이트
        // 현재는 방향만 업데이트
    }

    void Update()
    {
        // 방향 회전
        Quaternion targetQuaternion = Quaternion.Euler(0f, targetRotation, 0f);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetQuaternion,
            Time.deltaTime * rotationSpeed
        );

        // 현재 속도 계산
        currentSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        // 애니메이션 업데이트
        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        if (!useAnimation || animator == null)
            return;

        // 속도에 따라 애니메이션 파라미터 설정
        if (currentSpeed < idleThreshold)
        {
            animator.SetFloat(speedParameterName, 0f);  // Idle
        }
        else
        {
            animator.SetFloat(speedParameterName, currentSpeed);  // Walk/Run
        }
    }

    void OnDestroy()
    {
        if (gpsTracker != null)
        {
            gpsTracker.OnGPSUpdate -= OnGPSUpdated;
        }
    }
}