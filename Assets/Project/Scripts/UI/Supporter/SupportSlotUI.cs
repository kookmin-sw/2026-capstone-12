using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SupportSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Button button;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image iconImage;
    public GameObject cooldownObject;
    public TextMeshProUGUI cooldownText;

    public SupporterItemSO item;
    public SupportPlacementSystem placementSystem;

    public event Action<SupportSlotUI> Clicked;
    public event Action<SupportSlotUI> Hovered;
    public event Action<SupportSlotUI> HoverExited;

    private float cooldownEndTime;
    private bool subscribedToPlacementEvents;

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

        SubscribePlacementEvents();
        BindCooldownObject();
        BindButton();
    }

    private void OnEnable()
    {
        SubscribePlacementEvents();
        UpdateCooldownVisual();
    }

    private void Start()
    {
        Refresh();
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnMoneyChanged += HandleGoldChanged;
    }

    private void Update()
    {
        UpdateCooldownVisual();
    }

    private void OnDestroy()
    {
        UnsubscribePlacementEvents();

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnMoneyChanged -= HandleGoldChanged;
    }

    private void HandleGoldChanged(int _)
    {
        Refresh();
    }

    public void Configure(SupportPlacementSystem targetPlacementSystem, SupporterItemSO targetItem)
    {
        placementSystem = targetPlacementSystem;
        item = targetItem;
        BindCooldownObject();
        BindButton();
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (button == null || !button.interactable)
            OnClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HoverExited?.Invoke(this);
    }

    private void OnClick()
    {
        if (item == null || placementSystem == null)
            return;

        if (IsCooldownActive())
        {
            SupporterUISoundPlayer.Instance?.Play(SupporterUISoundType.CooldownDenied);
            return;
        }

        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(item.cost))
        {
            SupporterUISoundPlayer.Instance?.Play(SupporterUISoundType.ResourceLack);
            return;
        }

        placementSystem.SelectSupportItem(item);
        Clicked?.Invoke(this);
    }

    private void Refresh()
    {
        if (item == null)
        {
            if (costText != null) costText.text = "";
            if (nameText != null) nameText.text = "";
            if (descriptionText != null) descriptionText.text = "";
            if (iconImage != null) iconImage.sprite = null;
            if (button != null) button.interactable = false;
            SetCooldownVisible(false);
            return;
        }

        if (costText != null) costText.text = item.cost.ToString();
        if (nameText != null) nameText.text = string.IsNullOrWhiteSpace(item.displayName) ? item.name : item.displayName;
        if (descriptionText != null) descriptionText.text = item.description;
        if (iconImage != null) iconImage.sprite = item.icon;

        bool canAfford = ResourceManager.Instance == null || ResourceManager.Instance.CanAfford(item.cost);
        if (button != null) button.interactable = canAfford && !IsCooldownActive();

        UpdateCooldownVisual();
    }

    private void BindButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(OnClick);
        button.onClick.AddListener(OnClick);

        if (button.targetGraphic == null)
            button.targetGraphic = GetComponent<Graphic>();
    }

    private void HandleSupportSpawned(SupporterItemSO spawnedItem)
    {
        if (item == null || spawnedItem == null || item.cooldown <= 0f)
            return;

        if (item == spawnedItem || item.typeId == spawnedItem.typeId || item.kind == spawnedItem.kind)
            StartCooldown(item.cooldown);
    }

    private void SubscribePlacementEvents()
    {
        if (subscribedToPlacementEvents)
            return;

        BuildNetManager.SupportSpawned += HandleSupportSpawned;
        SupportPlacementSystem.LocalSupportPlaced += HandleSupportSpawned;
        subscribedToPlacementEvents = true;
    }

    private void UnsubscribePlacementEvents()
    {
        if (!subscribedToPlacementEvents)
            return;

        BuildNetManager.SupportSpawned -= HandleSupportSpawned;
        SupportPlacementSystem.LocalSupportPlaced -= HandleSupportSpawned;
        subscribedToPlacementEvents = false;
    }

    private void StartCooldown(float duration)
    {
        cooldownEndTime = Time.time + Mathf.Max(0f, duration);
        UpdateCooldownVisual();
        Refresh();
    }

    private bool IsCooldownActive()
    {
        return cooldownEndTime > Time.time;
    }

    private void UpdateCooldownVisual()
    {
        bool active = IsCooldownActive();
        SetCooldownVisible(active);

        if (active && cooldownText != null)
            cooldownText.text = Mathf.CeilToInt(cooldownEndTime - Time.time).ToString();

        if (button != null && item != null)
        {
            bool canAfford = ResourceManager.Instance == null || ResourceManager.Instance.CanAfford(item.cost);
            button.interactable = canAfford && !active;
        }
    }

    private void SetCooldownVisible(bool visible)
    {
        if (cooldownObject != null && cooldownObject.activeSelf != visible)
            cooldownObject.SetActive(visible);
    }

    private void BindCooldownObject()
    {
        if (cooldownObject == null)
        {
            Transform existing = transform.Find("ItemCooldown");
            if (existing != null)
                cooldownObject = existing.gameObject;
        }

        if (cooldownText == null && cooldownObject != null)
            cooldownText = cooldownObject.GetComponentInChildren<TextMeshProUGUI>(true);

        if (cooldownObject == null)
            return;

        // Cooldown UI는 Support 슬롯 프리팹에 배치된 오브젝트만 사용한다.
        DisableCooldownRaycasts(cooldownObject);
        SetCooldownVisible(false);
    }

    private void DisableCooldownRaycasts(GameObject target)
    {
        foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    private TextMeshProUGUI FindText(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    private Image FindImage(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<Image>() : null;
    }
}
