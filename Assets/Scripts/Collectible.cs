using UnityEngine;
using System.Collections.Generic;

public class Collectible : MonoBehaviour
{
    [Header("Collectible Data")]
    public string collectibleType;
    public int rarity = 1; // 1=common, ~

    [Header("Touch Settings")]
    [SerializeField] private LayerMask collectibleLayer;
    [SerializeField] private float touchRadius = 2f; // 터치 인식 반경

    private Dictionary<string, string> osmTags;
    private bool isCollected = false;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    public void Initialize(string type, Dictionary<string, string> tags)
    {
        collectibleType = type;
        osmTags = tags;
        transform.Rotate(0f, Random.Range(0f, 360f), 0f);
    }

    void Update()
    {
        if (isCollected) return;

        // 터치 입력 처리
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                CheckTouchCollect(touch.position);
            }
        }
        // 에디터 테스트용 마우스 클릭
        else if (Input.GetMouseButtonDown(0))
        {
            CheckTouchCollect(Input.mousePosition);
        }
    }

    void CheckTouchCollect(Vector3 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // Raycast로 터치한 오브젝트 확인
        if (Physics.Raycast(ray, out hit, 100f, collectibleLayer))
        {
            if (hit.collider.gameObject == gameObject)
            {
                Collect();
            }
        }
        else
        {
            // Raycast 실패 시, 화면상 거리로 체크 (2D 터치 범위)
            Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
            float distance = Vector2.Distance(new Vector2(screenPos.x, screenPos.y),
                                             new Vector2(screenPosition.x, screenPosition.y));

            if (distance < touchRadius * 50f)
            {
                Collect();
            }
        }
    }

    void Collect()
    {
        Transform playerTransform = FindObjectOfType<PlayerCharacter>()?.transform;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > 20f) return;

        isCollected = true;

        Debug.Log($"[Collectible] Collected: {collectibleType}, Rarity: {rarity}");

        // TODO: Add effects

        CollectionManager manager = FindObjectOfType<CollectionManager>();
        if (manager != null)
        {
            manager.OnCollectibleCollected(collectibleType, rarity);
        }

        Destroy(gameObject, 0.5f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = GetRarityColor();
        Gizmos.DrawWireSphere(transform.position, 1f);
    }

    Color GetRarityColor()
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