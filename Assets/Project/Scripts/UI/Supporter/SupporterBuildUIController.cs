using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SupporterBuildUIController : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private BuildSystem buildSystem;
    [SerializeField] private GameObject tabPanel;
    [SerializeField] private GameObject buildTab;
    [SerializeField] private GameObject buildingListPanel;
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TextMeshProUGUI descriptionNameText;
    [SerializeField] private TextMeshProUGUI descriptionGoldCostText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject activeResourcePanel;

    private readonly List<BuildSlotUI> slots = new();
    private BuildSlotUI hoveredSlot;
    private BuildSlotUI selectedSlot;
    private bool initialized;
    private bool buildSystemBound;
    private bool buildTabBound;
    private bool buildSlotsBound;

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
        buildSlotsBound = BindBuildSlots() || buildSlotsBound;

        if (buildingListPanel != null)
            buildingListPanel.SetActive(false);

        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);

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
            buildSlotsBound;
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

        if (buildingListPanel == null)
            buildingListPanel = FindDescendant("BuildingListPanel");

        if (descriptionPanel == null)
            descriptionPanel = FindDescendant("DescriptionPanel");

        if (activeResourcePanel == null)
            activeResourcePanel = FindDescendant("ResourcePanel");
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

    // Build 탭 클릭 시 목록 패널을 열고 닫음
    private void ToggleBuildingListPanel()
    {
        if (buildingListPanel == null)
            return;

        bool shouldOpen = !buildingListPanel.activeSelf;
        buildingListPanel.SetActive(shouldOpen);

        if (!shouldOpen)
        {
            hoveredSlot = null;
            HideDescription();
        }
    }

    // 슬롯 클릭 시 선택 상태를 저장하고 설명을 보여쥼
    private void HandleSlotClicked(BuildSlotUI slot)
    {
        selectedSlot = slot;
        hoveredSlot = slot;
        ShowDescription(slot);
    }

    // 슬롯 호버 시 해당 구조물 설명을 보여줌
    private void HandleSlotHovered(BuildSlotUI slot)
    {
        hoveredSlot = slot;
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

        if (descriptionNameText != null)
            descriptionNameText.text = displayName;

        if (descriptionGoldCostText != null)
            descriptionGoldCostText.text = type.cost.ToString();

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

    // 하위 전체에서 이름에 맞는 텍스트를 찾음
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
}
