using UnityEngine;
using UnityEngine.UI;

public class PurificationGaugeUI : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private RectTransform gaugeFillRect; // Purification Gauge 채움 영역
    [SerializeField] private Text gaugeText; // Purification Gauge 퍼센트 텍스트

    private ShooterPurification purification;
    private bool subscribed;

    private void OnEnable()
    {
        purification = GetComponentInParent<ShooterPurification>();
        SubscribeGaugeEvent();
        RefreshGaugeDisplay();
    }

    private void OnDisable()
    {
        UnsubscribeGaugeEvent();
    }

    // ShooterPurification 게이지 이벤트 구독
    private void SubscribeGaugeEvent()
    {
        if (subscribed || purification == null)
            return;

        purification.OnGaugeChanged += HandleGaugeChanged;
        subscribed = true;
    }

    // ShooterPurification 게이지 이벤트 구독 해제
    private void UnsubscribeGaugeEvent()
    {
        if (!subscribed || purification == null)
            return;

        purification.OnGaugeChanged -= HandleGaugeChanged;
        subscribed = false;
    }

    // 현재 Purification Gauge 값으로 UI 초기 표시
    private void RefreshGaugeDisplay()
    {
        float currentGauge = purification != null ? purification.CurrentGauge : 0f;
        float maxGauge = purification != null ? purification.MaxGauge : 100f;
        HandleGaugeChanged(currentGauge, maxGauge);
    }

    // Purification Gauge 변경에 따른 UI 표시 갱신
    private void HandleGaugeChanged(float currentGauge, float maxGauge)
    {
        float ratio = maxGauge > 0f ? Mathf.Clamp01(currentGauge / maxGauge) : 0f;

        if (gaugeFillRect != null)
        {
            Vector2 anchorMin = gaugeFillRect.anchorMin;
            Vector2 anchorMax = gaugeFillRect.anchorMax;
            gaugeFillRect.anchorMin = new Vector2(0f, anchorMin.y);
            gaugeFillRect.anchorMax = new Vector2(ratio, anchorMax.y);
            gaugeFillRect.offsetMin = new Vector2(0f, gaugeFillRect.offsetMin.y);
            gaugeFillRect.offsetMax = new Vector2(0f, gaugeFillRect.offsetMax.y);
        }

        if (gaugeText != null)
            gaugeText.text = $"Purification {Mathf.RoundToInt(ratio * 100f)}%";
    }

}
