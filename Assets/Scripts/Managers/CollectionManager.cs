using System.Collections.Generic;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance { get; private set; }

    private Dictionary<AnimalType, int> captureCounts = new Dictionary<AnimalType, int>();

    public IReadOnlyDictionary<AnimalType, int> CaptureCounts => captureCounts;

    [Header("Debug /  ʱ     ÿ ")]
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

        foreach (var type in initialCaptured)
        {
            RegisterCapture(type);
        }
    }

    public void RegisterCapture(AnimalType type)
    {
        if (!captureCounts.ContainsKey(type))
        {
            captureCounts[type] = 0;
        }
        captureCounts[type]++;
    }

    public bool HasAnimal(AnimalType type)
    {
        return captureCounts.ContainsKey(type);
    }

    public int GetCount(AnimalType type)
    {
        if (captureCounts.TryGetValue(type, out int count))
            return count;
        return 0;
    }
}
