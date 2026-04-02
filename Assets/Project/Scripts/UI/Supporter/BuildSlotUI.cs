using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Wiring")]
    public Button button;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI nameText;
    public Image iconImage;

    [Header("Runtime")]
    public BuildingTypeSO buildingType;
    public BuildSystem buildSystem;

    public event Action<BuildSlotUI> Clicked;
    public event Action<BuildSlotUI> Hovered;
    public event Action<BuildSlotUI> HoverExited;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (costText == null)
            costText = FindText("GoldCostText");

        if (nameText == null)
            nameText = FindText("NameText");

        if (iconImage == null)
            iconImage = FindImage("BuildingIcon");

        BindButton();
    }

    // 시작 시 표시를 갱신하고 자원 이벤트를 구독
    private void Start()
    {
        Refresh();
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnMoneyChanged += HandleGoldChanged;
    }

    // 파괴될 때 자원 이벤트 구독을 해제
    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnMoneyChanged -= HandleGoldChanged;
    }

    // 골드가 바뀌면 슬롯 표시를 다시 갱신
    private void HandleGoldChanged(int _)
    {
        Refresh();
    }

    // 비용, 이름, 아이콘, 버튼 활성 상태를 현재 데이터로 갱신
    private void Refresh()
    {
        if (buildingType == null)
        {
            if (costText != null) costText.text = "";
            if (nameText != null) nameText.text = "";
            if (iconImage != null) iconImage.sprite = null;
            if (button != null) button.interactable = false;
            return;
        }

        if (costText != null) costText.text = buildingType.cost.ToString();
        if (nameText != null)
            nameText.text = string.IsNullOrWhiteSpace(buildingType.displayName) ? buildingType.name : buildingType.displayName;
        if (iconImage != null)
            iconImage.sprite = buildingType.icon;

        bool canAfford = ResourceManager.Instance == null || ResourceManager.Instance.CanAfford(buildingType.cost);
        if (button != null) button.interactable = canAfford;
    }

    // 클릭 시 현재 구조물을 빌드 대상으로 선택
    private void OnClick()
    {
        if (buildingType == null || buildSystem == null)
            return;

        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(buildingType.cost))
            return;

        buildSystem.SelectBuilding(buildingType);
        Clicked?.Invoke(this);
    }

    // 외부에서 빌드 시스템과 구조물 데이터를 주입
    public void Configure(BuildSystem targetBuildSystem, BuildingTypeSO targetBuildingType)
    {
        buildSystem = targetBuildSystem;
        buildingType = targetBuildingType;
        BindButton();
        Refresh();
    }

    // 좌클릭 입력을 수동 클릭 처리와 연결
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (button == null)
            OnClick();
    }

    // 마우스가 올라오면 호버 이벤트를 전달
    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered?.Invoke(this);
    }

    // 마우스가 빠지면 호버 종료 이벤트를 전달
    public void OnPointerExit(PointerEventData eventData)
    {
        HoverExited?.Invoke(this);
    }

    // 자식 오브젝트에서 텍스트 컴포넌트를 찾음
    private TextMeshProUGUI FindText(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
            return child.GetComponent<TextMeshProUGUI>();

        return null;
    }

    // 자식 오브젝트에서 이미지 컴포넌트를 찾음.
    private Image FindImage(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
            return child.GetComponent<Image>();

        return null;
    }

    // 버튼 클릭 리스너와 타겟 그래픽을 연결
    private void BindButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(OnClick);
        button.onClick.AddListener(OnClick);

        if (button.targetGraphic == null)
            button.targetGraphic = GetComponent<Graphic>();
    }
}
