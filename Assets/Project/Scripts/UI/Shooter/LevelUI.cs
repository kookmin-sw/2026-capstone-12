using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슈터 레벨/XP UI (런타임 자동 생성)
/// - 화면 좌하단에 레벨 텍스트 + XP바 상시 표시
/// - 레벨업 시 화면 중앙에 "Lv.N 달성! OO ↑" 팝업 후 자동 사라짐
/// ShooterLevelNet 오브젝트에 함께 부착하면 됨
/// </summary>
public class LevelUI : MonoBehaviour
{
    [Header("팝업 설정")]
    [SerializeField] private float popupDuration = 2.5f;

    // 생성된 UI 요소
    private Text levelText;
    private Image xpBarFill;
    private RectTransform xpBarBg;
    private GameObject levelUpPopup;
    private Text levelUpText;

    private float popupTimer;
    private float barWidth;

    private bool uiReady;

    void Start()
    {
        // 서포터면 UI 생성하지 않음
        if (!IsShooter())
            return;

        BuildUI();

        if (ShooterLevelNet.Instance != null)
        {
            ShooterLevelNet.Instance.OnXpChanged += HandleXpChanged;
            ShooterLevelNet.Instance.OnLevelUp += HandleLevelUp;
        }

        UpdateDisplay(1, 0, 100);
    }

    bool IsShooter()
    {
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
            return false;
        return (PhotonNetwork.LocalPlayer.CustomProperties["Role"] as string) == "Shooter";
    }

    void OnDestroy()
    {
        if (ShooterLevelNet.Instance != null)
        {
            ShooterLevelNet.Instance.OnXpChanged -= HandleXpChanged;
            ShooterLevelNet.Instance.OnLevelUp -= HandleLevelUp;
        }
    }

    void Update()
    {
        if (popupTimer > 0f)
        {
            popupTimer -= Time.deltaTime;
            if (popupTimer <= 0f && levelUpPopup != null)
                levelUpPopup.SetActive(false);
        }
    }

    // ========================================
    // UI 런타임 생성
    // ========================================

    void BuildUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[LevelUI] Canvas를 찾을 수 없습니다.");
            return;
        }

        Transform canvasT = canvas.transform;

        // --- 레벨 패널 (우상단) ---
        GameObject panel = CreateUIObject("LevelPanel", canvasT);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-20f, -20f);
        panelRect.sizeDelta = new Vector2(180f, 50f);

        // 레벨 텍스트
        GameObject levelObj = CreateUIObject("LevelText", panelRect);
        levelText = levelObj.AddComponent<Text>();
        levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelText.fontSize = 22;
        levelText.fontStyle = FontStyle.Bold;
        levelText.color = Color.white;
        levelText.alignment = TextAnchor.MiddleRight;
        levelText.text = "Lv.1";
        AddOutline(levelObj);

        RectTransform levelRect = levelObj.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(1f, 0.5f);
        levelRect.anchorMax = new Vector2(1f, 0.5f);
        levelRect.pivot = new Vector2(1f, 0.5f);
        levelRect.anchoredPosition = new Vector2(0f, 10f);
        levelRect.sizeDelta = new Vector2(180f, 30f);

        // XP바 배경
        GameObject barBgObj = CreateUIObject("XpBarBg", panelRect);
        Image barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        xpBarBg = barBgObj.GetComponent<RectTransform>();
        xpBarBg.anchorMin = new Vector2(1f, 0f);
        xpBarBg.anchorMax = new Vector2(1f, 0f);
        xpBarBg.pivot = new Vector2(1f, 0f);
        xpBarBg.anchoredPosition = new Vector2(0f, 0f);
        xpBarBg.sizeDelta = new Vector2(170f, 10f);

        // XP바 채움 (우→좌 방향으로 차오름)
        GameObject barFillObj = CreateUIObject("XpBarFill", xpBarBg);
        xpBarFill = barFillObj.AddComponent<Image>();
        xpBarFill.color = new Color(0.3f, 0.8f, 1f, 1f); // 밝은 파란색

        RectTransform fillRect = barFillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 0f); // 시작은 0

        barWidth = 170f;

        // --- 레벨업 팝업 (화면 중앙 상단) ---
        levelUpPopup = CreateUIObject("LevelUpPopup", canvasT);
        RectTransform popupRect = levelUpPopup.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.75f);
        popupRect.anchorMax = new Vector2(0.5f, 0.75f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;
        popupRect.sizeDelta = new Vector2(400f, 60f);

        // 팝업 배경
        Image popupBg = levelUpPopup.AddComponent<Image>();
        popupBg.color = new Color(0f, 0f, 0f, 0.7f);

        // 팝업 텍스트
        GameObject popupTextObj = CreateUIObject("PopupText", popupRect);
        levelUpText = popupTextObj.AddComponent<Text>();
        levelUpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelUpText.fontSize = 26;
        levelUpText.fontStyle = FontStyle.Bold;
        levelUpText.color = new Color(1f, 0.85f, 0.2f); // 금색
        levelUpText.alignment = TextAnchor.MiddleCenter;
        levelUpText.text = "";
        AddOutline(popupTextObj);

        RectTransform popupTextRect = popupTextObj.GetComponent<RectTransform>();
        popupTextRect.anchorMin = Vector2.zero;
        popupTextRect.anchorMax = Vector2.one;
        popupTextRect.offsetMin = Vector2.zero;
        popupTextRect.offsetMax = Vector2.zero;

        levelUpPopup.SetActive(false);
        uiReady = true;
    }

    GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        obj.layer = LayerMask.NameToLayer("UI");
        return obj;
    }

    void AddOutline(GameObject obj)
    {
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    // ========================================
    // 이벤트 핸들러
    // ========================================

    void HandleXpChanged(int level, int currentXp, int xpToNext)
    {
        UpdateDisplay(level, currentXp, xpToNext);
    }

    void HandleLevelUp(int newLevel, ShooterLevelNet.StatType stat)
    {
        if (!uiReady) return;

        AudioManager.Instance?.PlayLevelUp();

        string statName = stat switch
        {
            ShooterLevelNet.StatType.Damage => "공격력",
            ShooterLevelNet.StatType.Speed => "이동속도",
            ShooterLevelNet.StatType.Health => "체력",
            _ => ""
        };

        levelUpText.text = $"Lv.{newLevel} 달성!  {statName} ↑";
        levelUpPopup.SetActive(true);
        popupTimer = popupDuration;
    }

    void UpdateDisplay(int level, int currentXp, int xpToNext)
    {
        if (!uiReady) return;

        levelText.text = $"Lv.{level}";

        float ratio = xpToNext > 0 ? (float)currentXp / xpToNext : 0f;
        RectTransform fillRect = xpBarFill.GetComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(barWidth * ratio, 0f);
    }
}
