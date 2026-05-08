using UnityEngine;
using UnityEngine.UI;

public class DarknessExposureUI : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private DarknessExposureController exposureController;
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private Text warningText;

    private bool subscribed;

    private void OnEnable()
    {
        if (exposureController == null)
            exposureController = GetComponentInParent<DarknessExposureController>();

        SubscribeExposureEvent();
        RefreshDisplay();
    }

    private void OnDisable()
    {
        UnsubscribeExposureEvent();
    }

    // DarknessExposureController 이벤트 구독
    private void SubscribeExposureEvent()
    {
        if (subscribed || exposureController == null)
            return;

        exposureController.OnExposureChanged += HandleExposureChanged;
        subscribed = true;
    }

    // DarknessExposureController 이벤트 구독 해제
    private void UnsubscribeExposureEvent()
    {
        if (!subscribed || exposureController == null)
            return;

        exposureController.OnExposureChanged -= HandleExposureChanged;
        subscribed = false;
    }

    private void RefreshDisplay()
    {
        if (exposureController == null)
        {
            SetWarningVisible(false);
            return;
        }

        HandleExposureChanged(
            exposureController.CurrentExposure,
            exposureController.MaxExposure,
            exposureController.ShouldShowWarning);
    }

    // Darkness Exposure 값에 따라 Shooter 경고 문구 갱신
    private void HandleExposureChanged(float currentExposure, float maxExposure, bool shouldShowWarning)
    {
        if (warningText == null)
            return;

        float ratio = maxExposure > 0f ? Mathf.Clamp01(currentExposure / maxExposure) : 0f;
        SetWarningVisible(shouldShowWarning);

        if (!shouldShowWarning)
            return;

        string title = "Purification Field Lost";
        if (exposureController != null)
        {
            if (!exposureController.IsInDarkness)
                title = "Return to Light";
            else if (currentExposure >= exposureController.DamageThreshold)
                title = "Corruption Exposure Rising";
        }

        warningText.text = $"{title}\nExposure {Mathf.RoundToInt(ratio * 100f)}%";
    }

    private void SetWarningVisible(bool visible)
    {
        if (warningPanel != null && warningPanel.activeSelf != visible)
            warningPanel.SetActive(visible);
    }

}
