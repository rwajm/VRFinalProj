using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // 따라다닐 플레이어 캐릭터
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0); // 캐릭터의 발밑이 아닌 허리/머리를 보게 함

    [Header("Distance Settings")]
    [SerializeField] private float distance = 10.0f; // 초기 거리
    [SerializeField] private float minDistance = 2.0f; // 최소 줌인
    [SerializeField] private float maxDistance = 20.0f; // 최대 줌아웃
    [SerializeField] private float zoomSpeed = 2.0f; // 줌 속도

    [Header("Rotation Settings")]
    [SerializeField] private float xSpeed = 20.0f; // 좌우 회전 감도
    [SerializeField] private float ySpeed = 20.0f; // 상하 회전 감도
    [SerializeField] private float yMinLimit = 10f; // 아래로 더 못 내려가게 (바닥 뚫림 방지)
    [SerializeField] private float yMaxLimit = 80f; // 위로 더 못 올라가게 (정수리 방지)

    // 내부 연산 변수
    private float x = 0.0f; // 현재 좌우 각도 (Yaw)
    private float y = 0.0f; // 현재 상하 각도 (Pitch)
    private float currentDistance;

    void Start()
    {
        // 타겟 자동 찾기 (PlayerCharacter 태그나 스크립트 기반)
        if (target == null)
        {
            PlayerCharacter player = FindObjectOfType<PlayerCharacter>();
            if (player != null) target = player.transform;
        }

        // 초기 각도 설정
        Vector3 angles = transform.eulerAngles;

        // [최초 구도 설정]
        // x: 캐릭터의 뒤쪽을 바라보게 설정 (캐릭터 회전값 + 0도)
        // y: 사선으로 내려다보게 설정 (45도)
        if (target != null)
        {
            x = target.eulerAngles.y; // 캐릭터가 보는 방향 뒤에서 시작
            y = 45f; // 45도 각도로 내려다보기
        }
        else
        {
            x = angles.y;
            y = angles.x;
        }

        currentDistance = distance;

        // 초기 위치 즉시 적용
        ApplyCameraPosition();
    }

    void LateUpdate()
    {
        if (!target) return;

        // --- 1. 입력 처리 (터치 & 마우스) ---
        HandleInput();

        // --- 2. 카메라 위치/회전 적용 ---
        ApplyCameraPosition();
    }

    void HandleInput()
    {
        // A. 모바일 터치 입력
        if (Input.touchCount == 1)
        {
            // [회전] 한 손가락 드래그
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                x += touch.deltaPosition.x * xSpeed * 0.02f;
                y -= touch.deltaPosition.y * ySpeed * 0.02f;
            }
        }
        else if (Input.touchCount == 2)
        {
            // [줌] 두 손가락 핀치
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // 이전 프레임의 터치 위치 계산
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // 이전 프레임 거리 vs 현재 프레임 거리 차이 (Magnitude)
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // 차이만큼 줌 적용
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            distance += deltaMagnitudeDiff * zoomSpeed * 0.01f;
        }
        // B. 에디터 테스트용 (마우스)
        else
        {
            // 마우스 왼쪽 드래그로 회전
            if (Input.GetMouseButton(0))
            {
                x += Input.GetAxis("Mouse X") * xSpeed * 0.2f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.2f;
            }

            // 마우스 휠로 줌
            distance -= Input.GetAxis("Mouse ScrollWheel") * 5f;
        }

        // 값 제한 (Clamp)
        y = ClampAngle(y, yMinLimit, yMaxLimit);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // 부드러운 줌 (선택 사항)
        currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime * 5f);
    }

    void ApplyCameraPosition()
    {
        // 회전 계산 (Euler -> Quaternion)
        Quaternion rotation = Quaternion.Euler(y, x, 0);

        // 위치 계산 (타겟 위치 - (회전 * 거리))
        // 타겟의 중심(targetOffset)을 기준으로 거리를 띄움
        Vector3 position = (target.position + targetOffset) - (rotation * Vector3.forward * currentDistance);

        // 최종 적용
        transform.rotation = rotation;
        transform.position = position;
    }
    public void AlignToNorth()
    {
        StopAllCoroutines(); // 중복 실행 방지
        StartCoroutine(SmoothAlignNorth());
    }

    // 부드럽게 회전시키는 코루틴
    System.Collections.IEnumerator SmoothAlignNorth()
    {
        float duration = 0.5f; // 0.5초 동안 회전
        float elapsed = 0f;

        float startX = x;

        // 현재 각도(startX)에서 0도(북쪽)로 가는 '가장 짧은 회전 각도'를 계산
        // 예: 현재 350도라면 -> +10도 회전 (반시계), 현재 10도라면 -> -10도 회전 (시계)
        float difference = Mathf.DeltaAngle(startX, 0f);
        float targetX = startX + difference;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 부드러운 움직임 (Ease-Out 효과)
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            // x값을 부드럽게 변경
            x = Mathf.Lerp(startX, targetX, t);

            yield return null;
        }

        x = targetX; // 정확히 0도로 고정 (혹은 360, 720 등 0과 동등한 각도)
    }

    // 각도 제한 헬퍼 함수
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}