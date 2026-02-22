using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 체력바 UI 관리
/// - 체력바 길이 변화
/// - 체력바 색상 변화 (초록 → 노랑 → 빨강)
/// - 체력 텍스트 표시
/// </summary>
public class HealthUI : MonoBehaviour
{
    // ============================================================
    // 참조
    // ============================================================
    [Header("References")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Text healthText;
    [SerializeField] private HealthManager healthManager;

    // ============================================================
    // 색상
    // ============================================================
    private readonly Color fullColor = new Color(0f, 0.78f, 0f);     // 초록색 (50% 이상)
    private readonly Color midColor = new Color(1f, 1f, 0f);          // 노란색 (25%~50%)
    private readonly Color lowColor = new Color(1f, 0f, 0f);          // 빨간색 (25% 이하)

    private const float BAR_WIDTH = 200f;                              // 체력바 기본 가로 길이

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Start()
    {
        // HealthManager 이벤트에 구독
        healthManager.OnHealthChanged.AddListener(UpdateHealthBar);

        // 초기 상태
        UpdateHealthBar(1f);
    }

    // ============================================================
    // UI 업데이트
    // ============================================================
    /// <summary>
    /// 체력바 업데이트 (0~1 비율 받음)
    /// </summary>
    void UpdateHealthBar(float healthRatio)
    {
        // 체력바 길이 변경
        healthBarFill.rectTransform.sizeDelta = new Vector2(BAR_WIDTH * healthRatio, 30f);

        // 색상 변경
        if (healthRatio > 0.5f)
            healthBarFill.color = fullColor;
        else if (healthRatio > 0.25f)
            healthBarFill.color = midColor;
        else
            healthBarFill.color = lowColor;

        // 텍스트 업데이트
        int current = (int)(healthRatio * healthManager.MaxHp);
        int max = (int)healthManager.MaxHp;
        healthText.text = $"{current} / {max}";
    }
}