[System.Serializable]
public class PlayerData
{
    public int level = 1;          // 시작 레벨
    public int currentExp = 0;     // 현재 경험치
    public int expToNextLevel = 100; // 다음 레벨까지 필요한 경험치

    // 경험치 추가
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        currentExp += amount;

        // 여러 레벨이 한 번에 오를 수 있으니 while 사용
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            level++;

            // 레벨이 올라갈수록 요구 경험치 조금씩 증가
            expToNextLevel = GetExpNeededForLevel(level);
        }
    }

    // 레벨별 필요 경험치 공식 (원하면 숫자만 바꾸면 됨)
    public int GetExpNeededForLevel(int level)
    {
        // 1레벨→2레벨: 100
        // 2→3: 150
        // 3→4: 200 … 이런 식
        return 100 + (level - 1) * 50;
    }

    // UI에서 쓸 비율값
    public float GetExpRatio()
    {
        if (expToNextLevel <= 0) return 0f;
        return (float)currentExp / expToNextLevel;
    }
}
