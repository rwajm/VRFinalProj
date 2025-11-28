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

    private void Start()
    {
        UpdatePlayerUI();

        // 레벨업 이벤트 구독
        GameManager.Instance.OnLevelUp += OnLevelUp;

        // 처음에는 팝업들 숨기기
        if (capturePopupRoot != null) capturePopupRoot.SetActive(false);
        if (levelUpPopupRoot != null) levelUpPopupRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp -= OnLevelUp;
    }

    private void UpdatePlayerUI()
    {
        var data = GameManager.Instance.playerData;

        playerLevelText.text = $"Lv. {data.level}";

        if (expFillImage != null)
        {
            float ratio = 0f;
            if (data.expToNextLevel > 0)
            {
                ratio = (float)data.currentExp / data.expToNextLevel;
            }

            expFillImage.fillAmount = Mathf.Clamp01(ratio);
        }
    }


    // (지도 로직에서)지도 위 동물을 클릭했을 때 호출할 함수
    public void OnAnimalClicked(AnimalType type)
    {
        currentAnimalInPopup = type;

        if (capturePopupRoot != null)
            capturePopupRoot.SetActive(true);

        if (captureTitleText != null)
            captureTitleText.text = $"야생의 {GetAnimalDisplayName(type)}가 나타났다!";

    }

    public void OnClickCaptureButton()
    {
        // 실패 확률 20%
        float r = Random.value;
        if (r < 0.2f)
        {
            if (captureTitleText != null)
                captureTitleText.text = "포획에 실패했다...";
            return;
        }

        // 성공: 경험치 부여
        GameManager.Instance.AddExp(currentAnimalInPopup);
        UpdatePlayerUI();

        // 컬렉션에 등록
        if (CollectionManager.Instance != null)
        {
            CollectionManager.Instance.RegisterCapture(currentAnimalInPopup);
        }

        if (capturePopupRoot != null)
            capturePopupRoot.SetActive(false);
    }

    public void OnClickCloseCapturePopup()
    {
        if (capturePopupRoot != null)
            capturePopupRoot.SetActive(false);
    }

    private void OnLevelUp()
    {
        if (levelUpPopupRoot != null)
            levelUpPopupRoot.SetActive(true);

        var data = GameManager.Instance.playerData;
        if (levelUpMessageText != null)
            levelUpMessageText.text = $"레벨 업!\n현재 레벨: {data.level}";
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
