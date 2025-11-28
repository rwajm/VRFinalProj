using System.Collections.Generic;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance { get; private set; }

    // 잡은 동물 카운트 (1마리라도 잡았으면 컬렉션에 노출)
    private Dictionary<AnimalType, int> captureCounts = new Dictionary<AnimalType, int>();

    public IReadOnlyDictionary<AnimalType, int> CaptureCounts => captureCounts;

    [Header("Debug / 초기 세팅용")]
    [SerializeField] private List<AnimalType> initialCaptured = new List<AnimalType>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 초기 테스트용: 미리 몇 개 잡혀 있는 상태로 시작하고 싶을 때
        foreach (var type in initialCaptured)
        {
            RegisterCapture(type);
        }
    }

    //동물을 새로(또는 추가로) 잡았을 때 호출.
    public void RegisterCapture(AnimalType type)
    {
        if (!captureCounts.ContainsKey(type))
        {
            captureCounts[type] = 0;
        }
        captureCounts[type]++;
    }

    //해당 동물을 한 번이라도 잡았는지
    public bool HasAnimal(AnimalType type)
    {
        return captureCounts.ContainsKey(type);
    }

    //몇 번 잡았는지.
    public int GetCount(AnimalType type)
    {
        if (captureCounts.TryGetValue(type, out int count))
            return count;
        return 0;
    }
}
