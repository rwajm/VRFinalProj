using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerData playerData = new PlayerData();

    // 레벨업 이벤트 (UI에서 팝업 띄우려고 씀)
    public event Action OnLevelUp;

    private int lastLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        lastLevel = playerData.level;
    }

    public void AddExp(AnimalType animalType)
    {
        int exp = GetExpByAnimal(animalType);
        playerData.AddExp(exp);

        if (playerData.level > lastLevel)
        {
            lastLevel = playerData.level;
            OnLevelUp?.Invoke();
        }
    }

    private int GetExpByAnimal(AnimalType type)
    {
        // 동물별 경험치 차등
        switch (type)
        {
            case AnimalType.Chicken: return 10;
            case AnimalType.Deer: return 15;
            case AnimalType.Dog: return 15;
            case AnimalType.Horse: return 20;
            case AnimalType.Kitty: return 15;
            case AnimalType.Penguin: return 25;
            case AnimalType.Tiger: return 30;
            default: return 10;
        }
    }
}
