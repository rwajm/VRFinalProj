using UnityEngine;


[System.Serializable]
public class PlayerData
{
    public int level = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100; // 임시 값

    public void AddExp(int amount)
    {
        currentExp += amount;
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            level++;
            // 레벨 올라갈수록 요구 경험치 증가 같은 건 나중에 조정
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.2f);
        }
    }
}
