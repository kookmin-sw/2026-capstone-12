using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 서포터 화면에서 슈터의 체력 UI를 표시한다.
/// </summary>
public class SupporterShooterHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private HealthManager shooterHealthManager;

    private readonly Color fullColor = new Color(0f, 0.78f, 0f);
    private readonly Color midColor = new Color(1f, 1f, 0f);
    private readonly Color lowColor = new Color(1f, 0f, 0f);

    private float barWidth;
    private float barHeight;
    private bool subscribed;

    private void Start()
    {
        if (healthBarFill == null || shooterHealthManager == null)
        {
            Debug.LogWarning($"{nameof(SupporterShooterHealthUI)} is missing references on {name}.");
            enabled = false;
            return;
        }

        barWidth = healthBarFill.rectTransform.sizeDelta.x;
        barHeight = healthBarFill.rectTransform.sizeDelta.y;

        shooterHealthManager.OnHealthChanged.AddListener(UpdateHealthBar);
        subscribed = true;

        float ratio = shooterHealthManager.MaxHp > 0f
            ? shooterHealthManager.CurrentHp / shooterHealthManager.MaxHp
            : 0f;

        UpdateHealthBar(ratio);
    }

    private void OnDestroy()
    {
        if (subscribed && shooterHealthManager != null)
            shooterHealthManager.OnHealthChanged.RemoveListener(UpdateHealthBar);
    }

    private void UpdateHealthBar(float healthRatio)
    {
        healthRatio = Mathf.Clamp01(healthRatio);
        healthBarFill.rectTransform.sizeDelta = new Vector2(barWidth * healthRatio, barHeight);

        if (healthRatio > 0.5f)
            healthBarFill.color = fullColor;
        else if (healthRatio > 0.25f)
            healthBarFill.color = midColor;
        else
            healthBarFill.color = lowColor;

        if (healthText != null)
        {
            int current = Mathf.RoundToInt(shooterHealthManager.CurrentHp);
            int max = Mathf.RoundToInt(shooterHealthManager.MaxHp);
            healthText.text = $"{current} / {max}";
        }
    }
}
