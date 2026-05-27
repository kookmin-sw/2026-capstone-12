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
    [SerializeField] private string body  = "SpawnCore가 정화 에너지를 흡수해서\n이 지역에 어둠이 찾아왔습니다.\nSpawnCore를 파괴하여 이 구역을 정화해주세요.";

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
