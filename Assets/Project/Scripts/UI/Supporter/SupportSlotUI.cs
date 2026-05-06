using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SupportSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Button button; // 클릭 입력 대상 버튼
    public TextMeshProUGUI costText; // 소모 골드 표시 텍스트
    public TextMeshProUGUI nameText; // 아이템 이름 표시 텍스트
    public TextMeshProUGUI descriptionText; // 슬롯 내부 설명 표시 텍스트
    public Image iconImage; // 아이템 아이콘 표시 이미지

    public SupporterItemSO item; // 슬롯에 연결된 Support 아이템 데이터
    public SupportPlacementSystem placementSystem; // 클릭 후 배치 모드 진입 대상 시스템

    public event Action<SupportSlotUI> Clicked; // 설명 선택 상태 갱신용 클릭 이벤트
    public event Action<SupportSlotUI> Hovered; // 설명 표시용 호버 이벤트
    public event Action<SupportSlotUI> HoverExited; // 설명 숨김용 호버 종료 이벤트

    // 슬롯 참조 자동 연결
    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (costText == null)
            costText = FindText("GoldCostText");

        if (nameText == null)
            nameText = FindText("NameText");

        if (descriptionText == null)
            descriptionText = FindText("DescriptionText");

        if (iconImage == null)
            iconImage = FindImage("SupportIcon");

        BindButton();
    }

    // 골드 변경 감지 시작
    private void Start()
    {
        Refresh();
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnMoneyChanged += HandleGoldChanged;
    }

    // 골드 변경 감지 해제
    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnMoneyChanged -= HandleGoldChanged;
    }

    // 골드 변경 후 버튼 상태 갱신
    private void HandleGoldChanged(int _)
    {
        Refresh();
    }

    // Support 아이템 데이터 주입
    public void Configure(SupportPlacementSystem targetPlacementSystem, SupporterItemSO targetItem)
    {
        placementSystem = targetPlacementSystem;
        item = targetItem;
        BindButton();
        Refresh();
    }

    // 버튼 컴포넌트 부재 시 클릭 입력 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (button == null || !button.interactable)
            OnClick();
    }

    // 설명 표시 요청
    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered?.Invoke(this);
    }

    // 설명 숨김 요청
    public void OnPointerExit(PointerEventData eventData)
    {
        HoverExited?.Invoke(this);
    }

    // Support 배치 모드 선택
    private void OnClick()
    {
        if (item == null || placementSystem == null)
            return;

        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(item.cost))
        {
            SupporterUISoundPlayer.Instance?.Play(SupporterUISoundType.ResourceLack);
            return;
        }

        placementSystem.SelectSupportItem(item);
        Clicked?.Invoke(this);
    }

    // 슬롯 텍스트와 구매 가능 상태 갱신
    private void Refresh()
    {
        if (item == null)
        {
            if (costText != null) costText.text = "";
            if (nameText != null) nameText.text = "";
            if (descriptionText != null) descriptionText.text = "";
            if (iconImage != null) iconImage.sprite = null;
            if (button != null) button.interactable = false;
            return;
        }

        if (costText != null) costText.text = item.cost.ToString();
        if (nameText != null) nameText.text = string.IsNullOrWhiteSpace(item.displayName) ? item.name : item.displayName;
        if (descriptionText != null) descriptionText.text = item.description;
        if (iconImage != null) iconImage.sprite = item.icon;

        bool canAfford = ResourceManager.Instance == null || ResourceManager.Instance.CanAfford(item.cost); // 골드 기반 클릭 가능 여부
        if (button != null) button.interactable = canAfford;
    }

    // 버튼 클릭 이벤트 중복 방지 연결
    private void BindButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(OnClick);
        button.onClick.AddListener(OnClick);

        if (button.targetGraphic == null)
            button.targetGraphic = GetComponent<Graphic>();
    }

    // 직접 자식 텍스트 조회
    private TextMeshProUGUI FindText(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    // 직접 자식 이미지 조회
    private Image FindImage(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<Image>() : null;
    }
}
