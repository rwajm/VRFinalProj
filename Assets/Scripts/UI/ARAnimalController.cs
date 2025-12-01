using UnityEngine;

public class ARAnimalController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;   // 조작할 동물

    [Header("Rotate Settings")]
    [SerializeField] private float rotateSpeed = 100f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 0.005f;
    [SerializeField] private float minScale = 0.3f;
    [SerializeField] private float maxScale = 2.0f;

    private float lastPinchDistance;
    private bool isPinching = false;

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void Update()
    {
        if (target == null) return;

#if UNITY_EDITOR
        HandleEditorInput();
#else
        HandleTouchInput();
#endif
    }

    void HandleEditorInput()
    {
        // 마우스 드래그로 회전
        if (Input.GetMouseButton(0))
        {
            float deltaX = Input.GetAxis("Mouse X");
            target.Rotate(Vector3.up, -deltaX * rotateSpeed * Time.deltaTime, Space.World);
        }

        // 마우스 휠로 줌
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            float scaleFactor = 1f + scroll;
            Vector3 newScale = target.localScale * scaleFactor;
            float s = Mathf.Clamp(newScale.x, minScale, maxScale);
            target.localScale = new Vector3(s, s, s);
        }
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            // 한 손가락 → 회전
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Moved)
            {
                float deltaX = t.deltaPosition.x;
                target.Rotate(Vector3.up, -deltaX * rotateSpeed * Time.deltaTime, Space.World);
            }

            isPinching = false;
        }
        else if (Input.touchCount == 2)
        {
            // 두 손가락 → 핀치 줌
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float currentDist = Vector2.Distance(t0.position, t1.position);

            if (!isPinching)
            {
                lastPinchDistance = currentDist;
                isPinching = true;
                return;
            }

            float delta = currentDist - lastPinchDistance;
            lastPinchDistance = currentDist;

            float scaleFactor = 1f + delta * zoomSpeed;
            Vector3 newScale = target.localScale * scaleFactor;

            float s = Mathf.Clamp(newScale.x, minScale, maxScale);
            target.localScale = new Vector3(s, s, s);
        }
        else
        {
            isPinching = false;
        }
    }
}
