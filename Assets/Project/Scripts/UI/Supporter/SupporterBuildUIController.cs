using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SupporterBuildUIController : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private BuildSystem buildSystem;
    [SerializeField] private GameObject tabPanel;
    [SerializeField] private GameObject buildTab;
    [SerializeField] private GameObject supportTab; // Support 목록 토글용 탭 버튼
    [SerializeField] private GameObject buildingListPanel;
    [SerializeField] private GameObject supportListPanel; // Support 아이템 슬롯 목록 패널
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TextMeshProUGUI descriptionNameText;
    [SerializeField] private TextMeshProUGUI descriptionGoldCostText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject activeResourcePanel;
    [SerializeField] private SupportPlacementSystem supportPlacementSystem; // Support 아이템 월드 배치 시스템
    [FormerlySerializedAs("soundManager")]
    [SerializeField] private SupporterUISoundPlayer soundPlayer; // Supporter UI 공통 사운드 재생기

    private readonly List<BuildSlotUI> slots = new();
    private readonly List<SupportSlotUI> supportSlots = new(); // Support 슬롯 이벤트 해제와 상태 관리를 위한 목록
    private BuildSlotUI hoveredSlot;
    private BuildSlotUI selectedSlot;
    private SupportSlotUI hoveredSupportSlot; // 현재 설명 표시 중인 Support 슬롯
    private SupportSlotUI selectedSupportSlot; // 클릭으로 고정된 Support 설명 슬롯
    private bool initialized;
    private bool buildSystemBound;
    private bool buildTabBound;
    private bool supportTabBound; // Support 탭 클릭 이벤트 연결 상태
    private bool buildSlotsBound;
    private bool supportSlotsBound; // Support 슬롯 이벤트 연결 상태

    private void Awake()
    {
        TryInitialize();
    }

    private void Start()
    {
        TryInitialize();
    }

    private void OnEnable()
    {
        TryInitialize();
    }

    // UI 동작에 필요한 참조와 이벤트 연결
    private void TryInitialize()
    {
        if (initialized)
            return;

        ResolveReferences();

        if (descriptionPanel != null)
        {
            if (descriptionNameText == null)
                descriptionNameText = FindTextInDescendants(descriptionPanel.transform, "NameText");

            if (descriptionGoldCostText == null)
                descriptionGoldCostText = FindTextInDescendants(descriptionPanel.transform, "GoldCostText");

            if (descriptionText == null)
                descriptionText = FindDescriptionBodyText(descriptionPanel.transform);
        }

        BindResourcePanel();
        buildSystemBound = BindBuildSystem() || buildSystemBound;
        buildTabBound = BindBuildTab() || buildTabBound;
        supportTabBound = BindSupportTab() || supportTabBound;
        buildSlotsBound = BindBuildSlots() || buildSlotsBound;
        supportSlotsBound = BindSupportSlots() || supportSlotsBound;

        if (buildingListPanel != null)
            buildingListPanel.SetActive(false);

        if (supportListPanel != null)
            supportListPanel.SetActive(false);

        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);

        bool supportUiReady = supportTab == null || (supportListPanel != null && supportTabBound && supportSlotsBound); // Support UI 선택 참조 초기화 완료 여부

        initialized =
            buildSystem != null &&
            buildTab != null &&
            buildingListPanel != null &&
            descriptionPanel != null &&
            descriptionNameText != null &&
            descriptionGoldCostText != null &&
            descriptionText != null &&
            buildSystemBound &&
            buildTabBound &&
            buildSlotsBound &&
            supportUiReady;
    }

    // 씬/자식 오브젝트에서 필요한 참조를 찾아 채움
    private void ResolveReferences()
    {
        if (buildSystem == null)
            buildSystem = FindObjectOfType<BuildSystem>(true);

        if (tabPanel == null)
            tabPanel = FindDescendant("TabPanel");

        if (buildTab == null)
            buildTab = FindChild(tabPanel != null ? tabPanel.transform : null, "BuildTab");

        if (supportTab == null)
            supportTab = FindChild(tabPanel != null ? tabPanel.transform : null, "SupportTab");

        if (buildingListPanel == null)
            buildingListPanel = FindDescendant("BuildingListPanel");

        if (supportListPanel == null)
            supportListPanel = FindDescendant("SupportListPanel");

        if (descriptionPanel == null)
            descriptionPanel = FindDescendant("DescriptionPanel");

        if (activeResourcePanel == null)
            activeResourcePanel = FindDescendant("ResourcePanel");

        if (supportPlacementSystem == null)
            supportPlacementSystem = GetComponent<SupportPlacementSystem>();

        if (supportPlacementSystem == null)
            supportPlacementSystem = gameObject.AddComponent<SupportPlacementSystem>();

        EnsureSoundPlayer();
    }

    // 리소스 패널의 골드 텍스트 참조를 연결
    private void BindResourcePanel()
    {
        if (activeResourcePanel == null)
            return;

        ResourcePanel resourcePanel = activeResourcePanel.GetComponent<ResourcePanel>();
        if (resourcePanel == null)
            resourcePanel = activeResourcePanel.AddComponent<ResourcePanel>();

        if (resourcePanel.goldText == null)
        {
            Transform goldTextTransform = activeResourcePanel.transform.Find("GoldText");
            if (goldTextTransform != null)
                resourcePanel.goldText = goldTextTransform.GetComponent<TextMeshProUGUI>();
        }
    }

    // Build 탭 버튼 클릭 이벤트를 연결
    private bool BindBuildTab()
    {
        if (buildTab == null)
            return false;

        Button button = buildTab.GetComponent<Button>();
        if (button == null)
            button = buildTab.AddComponent<Button>();

        Graphic graphic = buildTab.GetComponent<Graphic>();
        if (graphic != null && button.targetGraphic == null)
            button.targetGraphic = graphic;

        button.onClick.RemoveListener(ToggleBuildingListPanel);
        button.onClick.AddListener(ToggleBuildingListPanel);
        EnsureSoundEmitter(buildTab);

        return true;
    }

    // Support 탭 이벤트 연결
    private bool BindSupportTab()
    {
        if (supportTab == null)
            return false;

        Button button = supportTab.GetComponent<Button>();
        if (button == null)
            button = supportTab.AddComponent<Button>();

        Graphic graphic = supportTab.GetComponent<Graphic>();
        if (graphic != null && button.targetGraphic == null)
            button.targetGraphic = graphic;

        button.onClick.RemoveListener(ToggleSupportListPanel);
        button.onClick.AddListener(ToggleSupportListPanel);
        EnsureSoundEmitter(supportTab);

        return true;
    }

    // 빌드 모드 종료 이벤트를 UI에 연결
    private bool BindBuildSystem()
    {
        if (buildSystem == null)
            return false;

        buildSystem.BuildModeEnded -= HandleBuildModeEnded;
        buildSystem.BuildModeEnded += HandleBuildModeEnded;
        return true;
    }

    // 빌드 슬롯들을 데이터와 이벤트에 연결
    private bool BindBuildSlots()
    {
        if (buildingListPanel == null || buildSystem == null)
            return false;

        slots.Clear();

        Transform listTransform = buildingListPanel.transform.Find("BuildingList");
        if (listTransform == null)
            return false;

        List<Transform> slotTransforms = new();
        foreach (Transform child in listTransform)
        {
            if (child.name.StartsWith("Building"))
                slotTransforms.Add(child);
        }

        for (int i = 0; i < slotTransforms.Count; i++)
        {
            Transform slotTransform = slotTransforms[i];
            BuildSlotUI slot = slotTransform.GetComponent<BuildSlotUI>();
            if (slot == null)
                slot = slotTransform.gameObject.AddComponent<BuildSlotUI>();

            Button button = slotTransform.GetComponent<Button>();
            if (button == null)
                button = slotTransform.gameObject.AddComponent<Button>();

            Graphic graphic = slotTransform.GetComponent<Graphic>();
            if (graphic != null && button.targetGraphic == null)
                button.targetGraphic = graphic;

            EnsureSoundEmitter(slotTransform.gameObject);

            slot.button = button;
            slot.costText = FindText(slotTransform, "GoldCostText");
            slot.nameText = FindText(slotTransform, "NameText");
            slot.iconImage = FindImage(slotTransform, "BuildingIcon");

            BuildingTypeSO type = i < buildSystem.buildingTypes.Length ? buildSystem.buildingTypes[i] : null;
            slot.Configure(buildSystem, type);

            slot.Clicked -= HandleSlotClicked;
            slot.Hovered -= HandleSlotHovered;
            slot.HoverExited -= HandleSlotHoverExited;
            slot.Clicked += HandleSlotClicked;
            slot.Hovered += HandleSlotHovered;
            slot.HoverExited += HandleSlotHoverExited;

            slots.Add(slot);
        }

        return slots.Count > 0;
    }

    // Support 슬롯 이벤트 연결
    private bool BindSupportSlots()
    {
        if (supportListPanel == null || supportPlacementSystem == null)
            return supportTab == null;

        Transform listTransform = supportListPanel.transform.Find("SupportList"); // Support 슬롯 탐색 기준 부모
        if (listTransform == null)
            listTransform = supportListPanel.transform;

        supportSlots.Clear();
        List<Transform> slotTransforms = new(); // UI 순서와 아이템 카탈로그 순서 매칭용 슬롯 목록
        foreach (Transform child in listTransform)
        {
            bool hasSlotRootGraphic = child.GetComponent<Selectable>() != null || child.GetComponent<Graphic>() != null; // 버튼형 슬롯 루트 판정
            bool hasSupportSlotChildren =
                child.Find("SupportIcon") != null &&
                child.Find("NameText") != null &&
                child.Find("GoldCostText") != null; // Support 슬롯 하위 구조 판정

            if (!hasSlotRootGraphic && !hasSupportSlotChildren)
                continue;

            slotTransforms.Add(child);
        }

        int supportItemCount = supportPlacementSystem.SupportItemCount;
        while (slotTransforms.Count > 0 && slotTransforms.Count < supportItemCount)
        {
            Transform template = slotTransforms[slotTransforms.Count - 1];
            Transform clone = Instantiate(template, template.parent);
            clone.name = $"SupportSlot_{slotTransforms.Count + 1}";
            slotTransforms.Add(clone);
        }

        for (int i = 0; i < slotTransforms.Count; i++)
        {
            Transform slotTransform = slotTransforms[i];
            SupportSlotUI slot = slotTransform.GetComponent<SupportSlotUI>(); // Support 전용 클릭과 호버 처리 컴포넌트
            if (slot == null)
                slot = slotTransform.gameObject.AddComponent<SupportSlotUI>();

            Button button = slotTransform.GetComponent<Button>();
            if (button == null)
                button = slotTransform.gameObject.AddComponent<Button>();

            Graphic graphic = slotTransform.GetComponent<Graphic>();
            if (graphic != null && button.targetGraphic == null)
                button.targetGraphic = graphic;

            EnsureSoundEmitter(slotTransform.gameObject);

            slot.button = button;
            slot.costText = FindText(slotTransform, "GoldCostText");
            slot.nameText = FindText(slotTransform, "NameText");
            slot.descriptionText = FindTextInDescendants(slotTransform, "DescriptionText");
            slot.iconImage = FindImage(slotTransform, "SupportIcon");
            slot.Configure(supportPlacementSystem, supportPlacementSystem.GetSupportItem(i));

            slot.Clicked -= HandleSupportSlotClicked;
            slot.Hovered -= HandleSupportSlotHovered;
            slot.HoverExited -= HandleSupportSlotHoverExited;
            slot.Clicked += HandleSupportSlotClicked;
            slot.Hovered += HandleSupportSlotHovered;
            slot.HoverExited += HandleSupportSlotHoverExited;

            supportSlots.Add(slot);
        }

        return supportSlots.Count > 0;
    }

    // Build 목록 패널 토글
    private void ToggleBuildingListPanel()
    {
        if (buildingListPanel == null)
            return;

        bool shouldOpen = !buildingListPanel.activeSelf;
        buildingListPanel.SetActive(shouldOpen);

        if (shouldOpen && supportListPanel != null)
            supportListPanel.SetActive(false);

        if (!shouldOpen)
        {
            hoveredSlot = null;
            hoveredSupportSlot = null;
            HideDescription();
        }
    }

    // Support 목록 패널 토글
    private void ToggleSupportListPanel()
    {
        if (supportListPanel == null)
            return;

        bool shouldOpen = !supportListPanel.activeSelf;
        supportListPanel.SetActive(shouldOpen);

        if (shouldOpen && buildingListPanel != null)
            buildingListPanel.SetActive(false);

        hoveredSlot = null;
        hoveredSupportSlot = null;
        selectedSupportSlot = null;
        HideDescription();
    }

    // 슬롯 클릭 시 선택 상태를 저장하고 설명을 보여쥼
    private void HandleSlotClicked(BuildSlotUI slot)
    {
        selectedSlot = slot;
        hoveredSlot = slot;
        selectedSupportSlot = null;
        hoveredSupportSlot = null;
        ShowDescription(slot);
    }

    // 슬롯 호버 시 해당 구조물 설명을 보여줌
    private void HandleSlotHovered(BuildSlotUI slot)
    {
        hoveredSlot = slot;
        hoveredSupportSlot = null;
        ShowDescription(slot);
    }

    // 슬롯에서 마우스가 빠질 때 설명 패널 표시 상태를 정리
    private void HandleSlotHoverExited(BuildSlotUI slot)
    {
        if (hoveredSlot == slot)
            hoveredSlot = null;

        if (selectedSlot == slot)
        {
            ShowDescription(slot);
            return;
        }

        HideDescription();
    }

    // 설명 패널의 이름, 비용, 설명 텍스트를 갱신
    private void ShowDescription(BuildSlotUI slot)
    {
        if (slot == null || slot.buildingType == null || descriptionPanel == null)
            return;

        BuildingTypeSO type = slot.buildingType;
        string displayName = string.IsNullOrWhiteSpace(type.displayName) ? type.name : type.displayName;
        string description = string.IsNullOrWhiteSpace(type.description) ? displayName : type.description;

        ShowDescription(displayName, type.cost.ToString(), description);
    }

    // Support 설명 표시
    private void ShowDescription(SupportSlotUI slot)
    {
        if (slot == null || slot.item == null || descriptionPanel == null)
            return;

        string displayName = string.IsNullOrWhiteSpace(slot.item.displayName) ? slot.item.name : slot.item.displayName;
        string description = string.IsNullOrWhiteSpace(slot.item.description) ? displayName : slot.item.description;

        ShowDescription(displayName, slot.item.cost.ToString(), description);
    }

    // 설명 텍스트 갱신
    private void ShowDescription(string displayName, string goldCost, string description)
    {
        if (descriptionPanel == null)
            return;

        if (descriptionNameText != null)
            descriptionNameText.text = displayName;

        if (descriptionGoldCostText != null)
            descriptionGoldCostText.text = goldCost;

        if (descriptionText != null)
            descriptionText.text = description;

        descriptionPanel.SetActive(true);
    }

    // 설명 패널을 숨김
    private void HideDescription()
    {
        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);
    }

    // 빌드 모드가 끝나면 선택 상태와 설명 패널을 초기화
    private void HandleBuildModeEnded()
    {
        selectedSlot = null;
        hoveredSlot = null;
        hoveredSupportSlot = null;
        selectedSupportSlot = null;
        HideDescription();
    }

    // Support 슬롯 선택
    private void HandleSupportSlotClicked(SupportSlotUI slot)
    {
        selectedSupportSlot = slot;
        hoveredSupportSlot = slot;
        selectedSlot = null;
        hoveredSlot = null;
        ShowDescription(slot);
    }

    // Support 슬롯 호버
    private void HandleSupportSlotHovered(SupportSlotUI slot)
    {
        hoveredSupportSlot = slot;
        hoveredSlot = null;
        ShowDescription(slot);
    }

    // Support 슬롯 호버 종료
    private void HandleSupportSlotHoverExited(SupportSlotUI slot)
    {
        if (hoveredSupportSlot == slot)
            hoveredSupportSlot = null;

        if (selectedSupportSlot == slot)
        {
            ShowDescription(slot);
            return;
        }

        HideDescription();
    }

    // 파괴될 때 BuildSystem 이벤트 구독을 해제
    private void OnDestroy()
    {
        if (buildSystem != null)
            buildSystem.BuildModeEnded -= HandleBuildModeEnded;
    }

    // 하위 오브젝트 전체에서 이름으로 대상을 찾음
    private GameObject FindDescendant(string targetName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == targetName)
                return child.gameObject;
        }

        return null;
    }

    // 지정한 부모의 직접 자식 중에서 이름으로 대상을 찾음
    private GameObject FindChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        Transform child = parent.Find(childName);
        return child != null ? child.gameObject : null;
    }

    // 지정한 부모 아래에서 텍스트 컴포넌트를 찾음
    private TextMeshProUGUI FindText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    // 지정한 부모 아래에서 이미지 컴포넌트를 찾음
    private Image FindImage(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    // 하위 텍스트 조회
    private TextMeshProUGUI FindTextInDescendants(Transform parent, string childName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child.GetComponent<TextMeshProUGUI>();
        }

        return null;
    }

    // 설명 본문에 사용할 텍스트 오브젝트를 우선순위대로 찾음
    private TextMeshProUGUI FindDescriptionBodyText(Transform parent)
    {
        TextMeshProUGUI text = FindTextInDescendants(parent, "DescriptionText");
        if (text != null)
            return text;

        text = FindTextInDescendants(parent, "Description");
        if (text != null)
            return text;

        return FindTextInDescendants(parent, "NameText (1)");
    }

    // Supporter Canvas 범위에서 UI 사운드 관리자를 보장
    private void EnsureSoundPlayer()
    {
        if (soundPlayer != null)
            return;

        soundPlayer = GetComponentInParent<SupporterUISoundPlayer>(true);
        if (soundPlayer == null)
            soundPlayer = SupporterUISoundPlayer.Instance;

        if (soundPlayer == null)
            soundPlayer = FindObjectOfType<SupporterUISoundPlayer>(true);
    }

    // 클릭 가능한 Supporter UI 요소에 hover/click 사운드 감지기를 보장
    private void EnsureSoundEmitter(GameObject target)
    {
        if (target == null)
            return;

        if (target.GetComponent<SupporterUISoundEmitter>() == null)
            target.AddComponent<SupporterUISoundEmitter>();
    }
}
