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
        EnsureCooldownObject();
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
        EnsureCooldownObject();
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

    private void EnsureCooldownObject()
    {
        if (cooldownObject == null)
        {
            Transform existing = transform.Find("Cooldown");
            if (existing != null)
                cooldownObject = existing.gameObject;
        }

        if (cooldownObject == null)
            cooldownObject = CreateCooldownObject();

        if (cooldownText == null && cooldownObject != null)
            cooldownText = cooldownObject.GetComponentInChildren<TextMeshProUGUI>(true);

        if (cooldownObject == null)
            return;

        cooldownObject.name = "Cooldown";
        FitCooldownRect(cooldownObject.GetComponent<RectTransform>());
        DisableCooldownRaycasts(cooldownObject);
        SetCooldownVisible(false);
    }

    private GameObject CreateCooldownObject()
    {
        Transform template = FindRespawnCooldownTemplate();
        if (template != null)
            return Instantiate(template.gameObject, transform);

        GameObject overlay = new("Cooldown", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(transform, false);

        Image image = overlay.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject textObject = new("CooldownText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(overlay.transform, false);
        FitCooldownRect(textObject.GetComponent<RectTransform>());

        cooldownText = textObject.GetComponent<TextMeshProUGUI>();
        cooldownText.alignment = TextAlignmentOptions.Center;
        cooldownText.fontSize = 34f;
        cooldownText.color = Color.white;

        return overlay;
    }

    private Transform FindRespawnCooldownTemplate()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null)
            return null;

        foreach (Transform candidate in canvas.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == "RespawnCooldown")
                return candidate;
        }

        return null;
    }

    private void FitCooldownRect(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();
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
