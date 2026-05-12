using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 서포터 화면에서 커맨드 타워의 체력 UI를 표시한다.
/// </summary>
public class CommandTowerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image healthBarFill; // 체력 바 이미지
    [SerializeField] private TextMeshProUGUI healthText; // 체력 수치 텍스트
    [SerializeField] private CommandTower commandTower; // 커맨드 타워 참조

    private readonly Color fullColor = new Color(0f, 0.78f, 0f);
    private readonly Color midColor = new Color(1f, 1f, 0f);
    private readonly Color lowColor = new Color(1f, 0f, 0f);

    private BuildingHealthNet commandTowerHealthNet; // 커맨드 타워 체력 소스
    private float barWidth; // 바 기준 너비
    private float barHeight; // 바 기준 높이
    private bool subscribed; // HP 이벤트 구독 상태
    private bool waitingForTower; // 활성 타워 대기 상태

    /// <summary>
    /// 커맨드 타워 체력 참조와 초기 UI 상태를 맞춘다.
    /// </summary>
    private void Start()
    {
        ResolveCommandTowerHealthNet();

        if (healthBarFill == null)
        {
            Debug.LogWarning($"{nameof(CommandTowerHealthUI)} is missing references on {name}.");
            enabled = false;
            return;
        }

        barWidth = healthBarFill.rectTransform.sizeDelta.x;
        barHeight = healthBarFill.rectTransform.sizeDelta.y;

        if (commandTowerHealthNet != null)
        {
            BindToCommandTower(commandTowerHealthNet);
            return;
        }

        waitingForTower = true;
        CommandTower.ActiveTowerChanged += HandleActiveTowerChanged;
    }

    /// <summary>
    /// HP 이벤트와 활성 타워 변경 구독을 해제한다.
    /// </summary>
    private void OnDestroy()
    {
        if (waitingForTower)
            CommandTower.ActiveTowerChanged -= HandleActiveTowerChanged;

        if (subscribed && commandTowerHealthNet != null)
            commandTowerHealthNet.OnHpChanged -= HandleHpChanged;
    }

    /// <summary>
    /// 커맨드 타워 체력 소스를 찾는다.
    /// </summary>
    private void ResolveCommandTowerHealthNet()
    {
        if (commandTower != null)
        {
            commandTowerHealthNet = commandTower.GetComponent<BuildingHealthNet>();
            return;
        }

        if (CommandTower.ActiveTower != null)
            commandTowerHealthNet = CommandTower.ActiveTower.GetComponent<BuildingHealthNet>();
    }

    /// <summary>
    /// 활성 커맨드 타워 변경을 UI에 반영한다.
    /// </summary>
    private void HandleActiveTowerChanged(CommandTower activeTower)
    {
        if (activeTower == null)
            return;

        BuildingHealthNet activeTowerHealthNet = activeTower.GetComponent<BuildingHealthNet>();
        if (activeTowerHealthNet == null)
            return;

        CommandTower.ActiveTowerChanged -= HandleActiveTowerChanged;
        waitingForTower = false;
        BindToCommandTower(activeTowerHealthNet);
    }

    /// <summary>
    /// 커맨드 타워 HP 이벤트를 UI에 연결한다.
    /// </summary>
    private void BindToCommandTower(BuildingHealthNet newCommandTowerHealthNet)
    {
        if (commandTowerHealthNet != null)
            commandTowerHealthNet.OnHpChanged -= HandleHpChanged;

        commandTowerHealthNet = newCommandTowerHealthNet;
        commandTowerHealthNet.OnHpChanged += HandleHpChanged;
        subscribed = true;

        float ratio = commandTowerHealthNet.MaxHp > 0f
            ? commandTowerHealthNet.CurrentHp / commandTowerHealthNet.MaxHp
            : 0f;

        UpdateHealthBar(ratio);
    }

    /// <summary>
    /// 커맨드 타워 HP 변경을 UI에 반영한다.
    /// </summary>
    private void HandleHpChanged(BuildingHealthNet buildingHealth, float currentHp, float maxHp)
    {
        if (buildingHealth != commandTowerHealthNet)
            return;

        float ratio = maxHp > 0f ? currentHp / maxHp : 0f;
        UpdateHealthBar(ratio);
    }

    /// <summary>
    /// 체력 비율에 맞춰 바와 텍스트를 갱신한다.
    /// </summary>
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

        if (healthText != null && commandTowerHealthNet != null)
        {
            int current = Mathf.RoundToInt(commandTowerHealthNet.CurrentHp);
            int max = Mathf.RoundToInt(commandTowerHealthNet.MaxHp);
            healthText.text = $"{current} / {max}";
        }
    }
}
