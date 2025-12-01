using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{

    [System.Serializable]
    public class CollectibleType
    {
        public string typeName;            // enum 이름 그대로 사용 (Kitty, Dog, Chicken...)
        public GameObject prefab;
        public float spawnWeight = 1f;
        public Color debugColor = Color.white;
    }

    [System.Serializable]
    public class SpawnRule
    {
        public string osmFeatureType;      // "road", "green", "water"
        public List<CollectibleType> availableTypes;
        public float spawnDensity = 0.1f;
        public float minDistance = 5f;
    }

    [Header("Spawn Settings")]
    [SerializeField] private float spawnHeightOffset = 3.0f;
    [SerializeField] private bool visualizeSpawnPoints = true;

    [Header("Prefab Registry")]
    [SerializeField] private GameObject KittyPrefab;
    [SerializeField] private GameObject DogPrefab;
    [SerializeField] private GameObject ChickenPrefab;
    [SerializeField] private GameObject DeerPrefab;
    [SerializeField] private GameObject HorsePrefab;
    [SerializeField] private GameObject TigerPrefab;
    [SerializeField] private GameObject PenguinPrefab;

    [Header("References")]
    [SerializeField] private GPSTracker gpsTracker;

    private List<SpawnRule> spawnRules = new();
    private Dictionary<string, GameObject> spawnedObjects = new();
    private System.Random random;
    private bool isInitialized = false;

    void Start()
    {
        random = new System.Random();
        InitializeRules();

        if (gpsTracker == null) gpsTracker = FindObjectOfType<GPSTracker>();



    }


    void InitializeRules()
    {
        // 도로 = Kitty / Dog / Chicken
        SpawnRule roadRule = new()
        {
            osmFeatureType = "road",
            spawnDensity = 0.005f,
            minDistance = 10f,
            availableTypes = new List<CollectibleType>
            {
                new() { typeName = "Kitty", prefab = KittyPrefab, spawnWeight = 4f, debugColor = Color.yellow },
                new() { typeName = "Dog", prefab = DogPrefab, spawnWeight = 4f, debugColor = Color.cyan },
                new() { typeName = "Chicken", prefab = ChickenPrefab, spawnWeight = 2f, debugColor = Color.white }
            }
        };

        // 녹지 = Deer / Horse / Tiger
        SpawnRule greenRule = new()
        {
            osmFeatureType = "green",
            spawnDensity = 0.01f,
            minDistance = 15f,
            availableTypes = new List<CollectibleType>
            {
                new() { typeName = "Deer", prefab = DeerPrefab, spawnWeight = 6f, debugColor = Color.green },
                new() { typeName = "Horse", prefab = HorsePrefab, spawnWeight = 3f, debugColor = new Color(0.6f, 0.4f, 0.2f) },
                new() { typeName = "Tiger", prefab = TigerPrefab, spawnWeight = 1f, debugColor = new Color(1f, 0.5f, 0f) }
            }
        };

        // 수역 = Penguin / Deer
        SpawnRule waterRule = new()
        {
            osmFeatureType = "water",
            spawnDensity = 0.05f,
            minDistance = 12f,
            availableTypes = new List<CollectibleType>
            {
                new() { typeName = "Penguin", prefab = PenguinPrefab, spawnWeight = 2f, debugColor = Color.blue },
                new() { typeName = "Deer", prefab = DeerPrefab, spawnWeight = 8f, debugColor = Color.green }
            }
        };

        spawnRules.Add(roadRule);
        spawnRules.Add(greenRule);
        spawnRules.Add(waterRule);

        Debug.Log("[Spawner] Default animal spawn rules initialized");
    }

    public void ProcessOSMFeature(string featureType, List<Vector3> positions, Dictionary<string, string> tags)
    {
        if (positions == null || positions.Count == 0)
        {
            Debug.LogWarning($"[Spawner] Null or empty positions for {featureType}");
            return;
        }

        if (!isInitialized)
        {
            isInitialized = PlayerCharacter.IsGlobalOriginSet;
            if (!isInitialized) return;
        }

        SpawnRule rule = spawnRules.Find(r => r.osmFeatureType == featureType);
        if (rule == null || rule.availableTypes.Count == 0) return;

        SpawnAlongPath(positions, rule, tags);
    }

    void SpawnAlongPath(List<Vector3> path, SpawnRule rule, Dictionary<string, string> tags)
    {
        if (path.Count < 2) return;

        float totalLength = CalculatePathLength(path);
        int spawnCount = Mathf.FloorToInt(totalLength * rule.spawnDensity);

        Debug.Log($"[Spawner] {rule.osmFeatureType} - Path length: {totalLength:F1}m, spawning {spawnCount} objects");

        List<float> spawnDistances = GenerateSpawnDistances(totalLength, spawnCount, rule.minDistance);

        foreach (float distance in spawnDistances)
        {
            Vector3 spawnPos = GetPositionAlongPath(path, distance);
            Vector3 offsetPos = GetRandomOffset(spawnPos, rule);

            CollectibleType type = SelectRandomType(rule.availableTypes);
            SpawnCollectible(type, offsetPos, tags);
        }
    }

    float CalculatePathLength(List<Vector3> path)
    {
        float length = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            length += Vector3.Distance(path[i], path[i + 1]);
        }
        return length;
    }

    List<float> GenerateSpawnDistances(float totalLength, int count, float minDistance)
    {
        List<float> distances = new List<float>();

        if (count == 0) return distances;

        int maxPossible = Mathf.FloorToInt(totalLength / minDistance);
        count = Mathf.Min(count, maxPossible);

        float segment = totalLength / (count + 1);

        for (int i = 0; i < count; i++)
        {
            float baseDistance = segment * (i + 1);
            float randomOffset = ((float)random.NextDouble() - 0.5f) * minDistance * 0.5f;
            float distance = Mathf.Clamp(baseDistance + randomOffset, 0, totalLength);
            distances.Add(distance);
        }

        return distances;
    }

    Vector3 GetPositionAlongPath(List<Vector3> path, float targetDistance)
    {
        float accumulatedDistance = 0f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(path[i], path[i + 1]);

            if (accumulatedDistance + segmentLength >= targetDistance)
            {
                float t = (targetDistance - accumulatedDistance) / segmentLength;
                return Vector3.Lerp(path[i], path[i + 1], t);
            }

            accumulatedDistance += segmentLength;
        }

        return path[path.Count - 1];
    }

    Vector3 GetRandomOffset(Vector3 basePos, SpawnRule rule)
    {
        float offsetDistance = 2f + (float)random.NextDouble() * 3f;
        float angle = (float)random.NextDouble() * Mathf.PI * 2f;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * offsetDistance,
            spawnHeightOffset,
            Mathf.Sin(angle) * offsetDistance
        );

        return basePos + offset;
    }

    CollectibleType SelectRandomType(List<CollectibleType> types)
    {
        float totalWeight = 0f;
        foreach (var type in types)
        {
            totalWeight += type.spawnWeight;
        }

        float randomValue = (float)random.NextDouble() * totalWeight;
        float currentWeight = 0f;

        foreach (var type in types)
        {
            currentWeight += type.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return type;
            }
        }

        return types[0];
    }

    void SpawnCollectible(CollectibleType type, Vector3 position, Dictionary<string, string> tags)
    {
        if (type.prefab == null)
        {
            Debug.LogWarning($"[Spawner] No prefab for type: {type.typeName}");
            return;
        }

        string id = $"{type.typeName}_{position.GetHashCode()}";

        if (spawnedObjects.ContainsKey(id))
            return;

        GameObject obj = Instantiate(type.prefab, position, Quaternion.identity, transform);
        obj.name = id;

        Collectible collectible = obj.GetComponent<Collectible>();
        if (collectible != null)
        {
            collectible.Initialize(type.typeName, tags);
        }

        spawnedObjects.Add(id, obj);

        Debug.Log($"[Spawner] Spawned {type.typeName} at {position}");
    }

    public void OnWorldShift(Vector3 shiftVector)
    {
        foreach (var obj in spawnedObjects.Values)
        {
            if (obj != null)
            {
                obj.transform.position -= shiftVector;
            }
        }
    }

    public void ClearAllSpawns()
    {
        foreach (var obj in spawnedObjects.Values)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    void OnDrawGizmos()
    {
        if (!visualizeSpawnPoints) return;

        foreach (var kvp in spawnedObjects)
        {
            if (kvp.Value != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(kvp.Value.transform.position, 0.5f);
            }
        }
    }
}
