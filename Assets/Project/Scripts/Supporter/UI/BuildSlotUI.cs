using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildSlotUI : MonoBehaviour
{
    [Header("Wiring")]
    public Button button;
    public TextMeshProUGUI costText;

    [Header("Runtime")]
    public BuildingTypeSO buildingType;
    public BuildSystem buildSystem;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    private void Start()
    {
        Refresh();
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnMoneyChanged += HandleGoldChanged;
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnMoneyChanged -= HandleGoldChanged;
    }

    private void HandleGoldChanged(int _)
    {
        Refresh();
    }
    private void Refresh()
    {
        if (buildingType == null)
        {
            if (costText != null) costText.text = "";
            if (button != null) button.interactable = false;
            return;
        }

        if (costText != null) costText.text = buildingType.cost.ToString();

        bool canAfford = ResourceManager.Instance == null || ResourceManager.Instance.CanAfford(buildingType.cost);
        if (button != null) button.interactable = canAfford;
    }

    private void OnClick()
    {
        if (buildingType == null || buildSystem == null)
            return;

        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(buildingType.cost))
            return;

        buildSystem.SelectBuilding(buildingType);
    }
}
