using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;   // ★ 버튼 제어용

public class CollectionUI : MonoBehaviour
{
    [Header("Animal Buttons")]
    [SerializeField] private Button chickenButton;
    [SerializeField] private Button deerButton;
    [SerializeField] private Button dogButton;
    [SerializeField] private Button horseButton;
    [SerializeField] private Button kittyButton;
    [SerializeField] private Button penguinButton;
    [SerializeField] private Button tigerButton;

    private void OnEnable()
    {
        RefreshButtons();
    }

    private void Start()
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        // CollectionManager가 아직 안 만들어져 있으면 그냥 리턴
        if (CollectionManager.Instance == null)
            return;

        SetupButton(chickenButton, AnimalType.Chicken);
        SetupButton(deerButton, AnimalType.Deer);
        SetupButton(dogButton, AnimalType.Dog);
        SetupButton(horseButton, AnimalType.Horse);
        SetupButton(kittyButton, AnimalType.Kitty);
        SetupButton(penguinButton, AnimalType.Penguin);
        SetupButton(tigerButton, AnimalType.Tiger);
    }

    private void SetupButton(Button button, AnimalType type)
    {
        if (button == null) return;

        bool has = CollectionManager.Instance.HasAnimal(type);

        // 1) 안 잡은 동물은 아예 안 보이게
        button.gameObject.SetActive(has);

        // 2) 잡은 것만 보이니까, 따로 interactable 설정은 안 해도 되지만
        //    혹시 나중에 쓸 일 있을까 봐 남겨두고 싶으면 이렇게:
        // button.interactable = has;
    }


    public void OnClickBackButton()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OnClickAnimalButton(string animalTypeName)
    {
        // 문자열 → AnimalType 변환
        if (!System.Enum.TryParse(animalTypeName, out AnimalType type))
            return;

        // 아직 안 잡은 동물이면 그냥 무시 (혹시라도 버튼이 잘못 활성화된 경우 대비)
        if (CollectionManager.Instance != null &&
            !CollectionManager.Instance.HasAnimal(type))
        {
            return;
        }

        // 선택한 동물 정보 저장 → AR 씬에서 사용
        SelectedAnimalHolder.SelectedAnimal = type;
        SceneManager.LoadScene("ARViewScene");
    }
}
