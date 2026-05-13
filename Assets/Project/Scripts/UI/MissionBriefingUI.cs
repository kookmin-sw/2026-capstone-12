using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MissionBriefingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;

    [Header("Content")]
    [SerializeField] private string title = "작전 목표";
    [SerializeField] private string body  = "커맨드 타워를 사수하고,\n3개의 스폰 코어를 파괴하라.";

    [Header("Timing")]
    [SerializeField] private float delayBeforeShow = 1.5f;
    [SerializeField] private float fadeInDuration  = 0.5f;
    [SerializeField] private float holdDuration    = 3.5f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    private void Start()
    {
        if (titleText != null) titleText.text = title;
        if (bodyText  != null) bodyText.text  = body;
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return new WaitForSeconds(delayBeforeShow);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(holdDuration);

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }
}
