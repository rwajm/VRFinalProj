using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ARViewUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnRoot;  // 동물이 나타날 기준 위치
    [SerializeField] private GameObject chickenPrefab;
    [SerializeField] private GameObject deerPrefab;
    [SerializeField] private GameObject dogPrefab;
    [SerializeField] private GameObject horsePrefab;
    [SerializeField] private GameObject kittyPrefab;
    [SerializeField] private GameObject penguinPrefab;
    [SerializeField] private GameObject tigerPrefab;

    private GameObject spawnedInstance;

    private void Start()
    {
        var type = SelectedAnimalHolder.SelectedAnimal;

        // UI 제목
        if (titleText != null)
        {
            titleText.text = $"{GetAnimalDisplayName(type)}";
        }

        // AR 동물 스폰
        SpawnAnimal(type);
    }

    private void SpawnAnimal(AnimalType type)
    {
        if (spawnRoot == null)
        {
            spawnRoot = this.transform;
        }

        GameObject prefab = GetPrefab(type);
        if (prefab == null)
        {
            Debug.LogWarning($"[ARViewUI] {type}용 프리팹이 설정되어 있지 않습니다.");
            return;
        }

        if (spawnedInstance != null)
        {
            Destroy(spawnedInstance);
        }

        spawnedInstance = Instantiate(prefab, spawnRoot);

        spawnedInstance.transform.localPosition = Vector3.zero;
        spawnedInstance.transform.localRotation = Quaternion.identity;
        spawnedInstance.transform.localScale = Vector3.one;

        // ★ 여기 추가: ARAnimalController에 대상 전달
        var controller = GetComponent<ARAnimalController>();
        if (controller != null)
        {
            controller.SetTarget(spawnedInstance.transform);
        }
    }


    private GameObject GetPrefab(AnimalType type)
    {
        switch (type)
        {
            case AnimalType.Chicken: return chickenPrefab;
            case AnimalType.Deer: return deerPrefab;
            case AnimalType.Dog: return dogPrefab;
            case AnimalType.Horse: return horsePrefab;
            case AnimalType.Kitty: return kittyPrefab;
            case AnimalType.Penguin: return penguinPrefab;
            case AnimalType.Tiger: return tigerPrefab;
            default: return null;
        }
    }

    public void OnClickBackButton()
    {
        SceneManager.LoadScene("CollectionScene");
    }

    private string GetAnimalDisplayName(AnimalType type)
    {
        switch (type)
        {
            case AnimalType.Chicken: return "Chicken";
            case AnimalType.Deer: return "Deer";
            case AnimalType.Dog: return "Dog";
            case AnimalType.Horse: return "Horse";
            case AnimalType.Kitty: return "Kitty";
            case AnimalType.Penguin: return "Penguin";
            case AnimalType.Tiger: return "Tiger";
            default: return "Animal";
        }
    }

}
