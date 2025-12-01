using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainUI : MonoBehaviour
{
    [Header("Top UI")]
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private Image expFillImage;

    [Header("Capture Popup")]
    [SerializeField] private GameObject capturePopupRoot;
    [SerializeField] private TextMeshProUGUI captureTitleText;

    [Header("Level Up Popup")]
    [SerializeField] private GameObject levelUpPopupRoot;
    [SerializeField] private TextMeshProUGUI levelUpMessageText;

    private AnimalType currentAnimalInPopup;
    private Collectible currentCollectible;    // ★ 새로 추가됨

    private void Start()
    {
        UpdatePlayerUI();

        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp += OnLevelUp;

        if (capturePopupRoot) capturePopupRoot.SetActive(false);
        if (levelUpPopupRoot) levelUpPopupRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp -= OnLevelUp;
    }

    private void UpdatePlayerUI()
    {
        // GameManager 존재 여부 체크
        if (GameManager.Instance == null)
        {
            Debug.LogError("[MainUI] GameManager.Instance is null. 씬에 GameManager 오브젝트가 있는지 확인하세요.");
            return;
        }

        var data = GameManager.Instance.playerData;
        if (data == null)
        {
            Debug.LogError("[MainUI] playerData is null.");
            return;
        }

        // 레벨 텍스트 갱신
        if (playerLevelText != null)
        {
            playerLevelText.text = $"Lv. {data.level}";
        }
        else
        {
            Debug.LogWarning("[MainUI] playerLevelText가 인스펙터에 연결되어 있지 않습니다.");
        }

        // 경험치 게이지 갱신
        if (expFillImage != null)
        {
            float ratio = 0f;

            // PlayerData에 GetExpRatio()가 있으면 그걸 쓰고,
            // 없으면 currentExp / expToNextLevel로 계산
            if (data.expToNextLevel > 0)
            {
                ratio = (float)data.currentExp / data.expToNextLevel;
            }

            expFillImage.fillAmount = Mathf.Clamp01(ratio);
        }
        else
        {
            Debug.LogWarning("[MainUI] expFillImage가 인스펙터에 연결되어 있지 않습니다.");
        }
    }


    // ------------------------------------------------------
    //      Collectible에서 직접 호출하는 함수 (핵심)
    // ------------------------------------------------------
    public void OnCollectibleClicked(Collectible collectible)
    {
        currentCollectible = collectible;
        currentAnimalInPopup = collectible.animalType;

        if (capturePopupRoot != null)
            capturePopupRoot.SetActive(true);

        if (captureTitleText != null)
            captureTitleText.text = $"Wild {GetAnimalDisplayName(currentAnimalInPopup)}!";
    }

    // ------------------------------------------------------
    //          캡처 버튼 (여기서 실제 수집 처리)
    // ------------------------------------------------------
    public void OnClickCaptureButton()
    {
        if (currentCollectible == null)
        {
            Debug.LogWarning("[MainUI] currentCollectible is null");
            return;
        }

        // 실패 확률 20%
        float r = Random.value;
        if (r < 0.2f)
        {
            if (captureTitleText != null)
                captureTitleText.text = "Fail...";
            return;
        }

        // ------ 성공 처리 ------
        GameManager.Instance.AddExp(currentAnimalInPopup);

        if (CollectionManager.Instance != null)
            CollectionManager.Instance.RegisterCapture(currentAnimalInPopup);

        UpdatePlayerUI();

        // Collectible 제거
        currentCollectible.RemoveCollectible();
        currentCollectible = null;

        if (capturePopupRoot != null)
            capturePopupRoot.SetActive(false);
    }

    public void OnClickCloseCapturePopup()
    {
        if (capturePopupRoot != null)
            capturePopupRoot.SetActive(false);

        currentCollectible = null;
    }

    private void OnLevelUp()
    {
        if (levelUpPopupRoot != null)
            levelUpPopupRoot.SetActive(true);

        var data = GameManager.Instance.playerData;
        if (levelUpMessageText != null)
            levelUpMessageText.text = $"Level UP!\nNow Lv: {data.level}";
    }

    public void OnClickLevelUpOkButton()
    {
        if (levelUpPopupRoot != null)
            levelUpPopupRoot.SetActive(false);
    }

    public void OnClickCollectionButton()
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
