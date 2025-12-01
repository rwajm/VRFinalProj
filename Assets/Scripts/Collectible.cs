using UnityEngine;
using System;
using System.Collections.Generic;

public class Collectible : MonoBehaviour
{
    [Header("Collectible Data")]
    public AnimalType animalType;
    [Range(1, 4)] public int rarity = 1;

    [Header("Touch Settings")]
    [SerializeField] private LayerMask collectibleLayer = ~0;   // 기본값: 모든 레이어
    [SerializeField] private float maxClickScreenDistance = 80f; // 화면 거리 허용값
    [SerializeField] private float maxCollectDistanceFromPlayer = 20f; // 플레이어와 거리 제한

    private Dictionary<string, string> osmTags;
    private Camera mainCamera;
    private bool isClicked = false;   // 팝업 떠 있는 동안 중복 클릭 방지

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    /// <summary>
    /// Spawner가 문자열 typeName을 넘겨줄 때, enum으로 매핑해주는 초기화 함수
    /// </summary>
    public void Initialize(string typeName, Dictionary<string, string> tags)
    {
        if (Enum.TryParse(typeName, out AnimalType parsed))
        {
            animalType = parsed;
        }
        else
        {
            Debug.LogWarning($"[Collectible] Unknown typeName: {typeName}. Defaulting to Chicken.");
            animalType = AnimalType.Chicken;
        }

        osmTags = tags;
    }

    private void Update()
    {
        if (isClicked) return;

        // 에디터 / PC용 마우스 입력
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            TryClick(Input.mousePosition);
        }
#endif

        // 모바일 터치 입력
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                TryClick(t.position);
            }
        }
    }

    private void TryClick(Vector2 screenPos)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        // 1) Raycast로 정확한 콜라이더 클릭 체크
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, collectibleLayer))
        {
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                ProcessClicked();
                return;
            }
        }

        // 2) 실패 시, 화면 좌표 기준 거리로 보정
        Vector3 selfScreen = mainCamera.WorldToScreenPoint(transform.position);
        float dist = Vector2.Distance(
            new Vector2(selfScreen.x, selfScreen.y),
            screenPos
        );

        if (dist <= maxClickScreenDistance)
        {
            ProcessClicked();
        }
    }

    private void ProcessClicked()
    {
        if (isClicked) return;

        // 플레이어와 너무 멀면 무시
        PlayerCharacter player = FindObjectOfType<PlayerCharacter>();
        if (player != null)
        {
            float worldDist = Vector3.Distance(player.transform.position, transform.position);
            if (worldDist > maxCollectDistanceFromPlayer)
                return;
        }

        isClicked = true;

        // UI에 이벤트 전달
        MainUI ui = FindObjectOfType<MainUI>();
        if (ui != null)
        {
            ui.OnCollectibleClicked(this);
        }
        else
        {
            Debug.LogWarning("[Collectible] MainUI not found in scene.");
            // UI가 없으면 다시 클릭 가능하도록 풀어줌
            isClicked = false;
        }
    }

    /// <summary>
    /// MainUI에서 포획 성공 시 호출 → 이 오브젝트 삭제
    /// </summary>
    public void RemoveCollectible()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = GetRarityColor();
        Gizmos.DrawWireSphere(transform.position, 1f);
    }

    private Color GetRarityColor()
    {
        switch (rarity)
        {
            case 1: return Color.white;
            case 2: return Color.green;
            case 3: return Color.blue;
            case 4: return Color.yellow;
            default: return Color.gray;
        }
    }
}
