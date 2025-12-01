using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        // 씬이 켜질 때마다, 현재 컬렉션 상태에 맞춰 버튼 보이기/숨기기
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (CollectionManager.Instance == null)
        {
            Debug.LogWarning("[CollectionUI] CollectionManager.Instance is null");
            return;
        }

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

        // 잡은 동물만 버튼 활성화
        button.gameObject.SetActive(has);

        // 혹시 버튼에 OnClick이 아직 안 걸려 있으면, 여기서 코드로도 걸어둘 수 있지만
        // 지금은 인스펙터의 OnClick에 연결하는 쪽을 기준으로 할 거라서 여기선 X
    }

    // -------------------------
    //  버튼 OnClick에서 호출할 함수들
    // -------------------------
    public void OnClickChicken()
    {
        SelectAnimalAndGo(AnimalType.Chicken);
    }

    public void OnClickDeer()
    {
        SelectAnimalAndGo(AnimalType.Deer);
    }

    public void OnClickDog()
    {
        SelectAnimalAndGo(AnimalType.Dog);
    }

    public void OnClickHorse()
    {
        SelectAnimalAndGo(AnimalType.Horse);
    }

    public void OnClickKitty()
    {
        SelectAnimalAndGo(AnimalType.Kitty);
    }

    public void OnClickPenguin()
    {
        SelectAnimalAndGo(AnimalType.Penguin);
    }

    public void OnClickTiger()
    {
        SelectAnimalAndGo(AnimalType.Tiger);
    }

    private void SelectAnimalAndGo(AnimalType type)
    {
        SelectedAnimalHolder.SelectedAnimal = type;
        SceneManager.LoadScene("ARViewScene");
    }

    public void OnClickBackButton()
    {
        SceneManager.LoadScene("MainScene");
    }
}
