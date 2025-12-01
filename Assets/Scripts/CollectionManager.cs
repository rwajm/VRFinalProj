using UnityEngine;
using System.Collections.Generic;

public class CollectionManager : MonoBehaviour
{
    [System.Serializable]
    public class CollectionStats
    {
        public string type;
        public int count;
        public int commonCount;
        public int uncommonCount;
        public int rareCount;
        public int legendaryCount;
    }

    [Header("Collection Data")]
    [SerializeField] private List<CollectionStats> collections = new List<CollectionStats>();

    private Dictionary<string, CollectionStats> collectionDict = new Dictionary<string, CollectionStats>();

    void Start()
    {
        InitializeCollections();
    }

    void InitializeCollections()
    {
        foreach (var stat in collections)
        {
            collectionDict[stat.type] = stat;
        }
    }

    public void OnCollectibleCollected(string type, int rarity)
    {
        if (!collectionDict.ContainsKey(type))
        {
            CollectionStats newStat = new CollectionStats { type = type };
            collections.Add(newStat);
            collectionDict[type] = newStat;
        }

        CollectionStats stats = collectionDict[type];
        stats.count++;

        switch (rarity)
        {
            case 1: stats.commonCount++; break;
            case 2: stats.uncommonCount++; break;
            case 3: stats.rareCount++; break;
            case 4: stats.legendaryCount++; break;
        }

        Debug.Log($"[Collection] {type} collected! Total: {stats.count} (C:{stats.commonCount}, U:{stats.uncommonCount}, R:{stats.rareCount}, L:{stats.legendaryCount})");
    }

    public CollectionStats GetStats(string type)
    {
        return collectionDict.ContainsKey(type) ? collectionDict[type] : null;
    }

    public int GetTotalCollected()
    {
        int total = 0;
        foreach (var stat in collections)
        {
            total += stat.count;
        }
        return total;
    }

    //void OnGUI()
    //{
    //    GUIStyle style = new GUIStyle();
    //    style.fontSize = 25;
    //    style.normal.textColor = Color.white;

    //    string display = $"<b>Collection</b>\nTotal: {GetTotalCollected()}\n\n";

    //    foreach (var stat in collections)
    //    {
    //        if (stat.count > 0)
    //        {
    //            display += $"{stat.type}: {stat.count}\n";
    //        }
    //    }

    //    GUI.Label(new Rect(Screen.width - 300, 50, 280, 500), display, style);
    //}
}