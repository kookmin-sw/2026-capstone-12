using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스폰 코어 파괴 시 양쪽 플레이어에게 몬스터 강화 알림을 표시
/// - SharedCanvas에 배치, SpawnCoreManager.OnSpawnCoreDifficultyChanged 구독
/// - 상단 슬라이드인 → 유지 → 페이드아웃
/// </summary>
public class CoreDestroyNotificationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private Text multiplierText;

    [Header("Animation")]
    [SerializeField] private float slideInDuration  = 0.35f;
    [SerializeField] private float holdDuration     = 2.8f;
    [SerializeField] private float fadeOutDuration  = 0.5f;

    // 패널이 완전히 보이는 Y 위치 (anchoredPosition 기준)
    private float shownY;
    // 슬라이드 시작 Y (패널 높이만큼 위)
    private float hiddenY;

    private Coroutine showCoroutine;

    private void Start()
    {
        if (panelRect != null)
        {
            shownY  = panelRect.anchoredPosition.y;
            hiddenY = shownY + panelRect.sizeDelta.y + 20f;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (panelRect != null)
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, hiddenY);

        SpawnCoreManager mgr = SpawnCoreManager.Instance;
        if (mgr != null)
            mgr.OnSpawnCoreDifficultyChanged.AddListener(OnDifficultyChanged);
        else
            StartCoroutine(WaitAndSubscribe());
    }

    private void OnDestroy()
    {
        if (SpawnCoreManager.Instance != null)
            SpawnCoreManager.Instance.OnSpawnCoreDifficultyChanged.RemoveListener(OnDifficultyChanged);
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (SpawnCoreManager.Instance == null)
            yield return null;

        SpawnCoreManager.Instance.OnSpawnCoreDifficultyChanged.AddListener(OnDifficultyChanged);
    }

    private void OnDifficultyChanged(int destroyedCount)
    {
        UpdateTexts(destroyedCount);

        if (showCoroutine != null)
            StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(ShowRoutine());
    }

    private void UpdateTexts(int destroyedCount)
    {
        float multiplier = destroyedCount >= 2 ? 2.0f : 1.5f;
        string coreCountStr = destroyedCount == 1 ? "첫 번째" : $"{destroyedCount}번째";

        if (titleText != null)
            titleText.text = $"⚠  스폰 코어 파괴!  ({coreCountStr})";

        if (subtitleText != null)
            subtitleText.text = "몬스터가 강화되었습니다";

        if (multiplierText != null)
            multiplierText.text = $"체력 · 공격력 · 이동속도  ×{multiplier:F1}";
    }

    private IEnumerator ShowRoutine()
    {
        // 초기화
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        float startX = panelRect.anchoredPosition.x;

        // 1. 슬라이드 인
        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideInDuration);
            panelRect.anchoredPosition = new Vector2(startX, Mathf.Lerp(hiddenY, shownY, t));
            yield return null;
        }
        panelRect.anchoredPosition = new Vector2(startX, shownY);

        // 2. 유지
        yield return new WaitForSeconds(holdDuration);

        // 3. 페이드 아웃
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        panelRect.anchoredPosition = new Vector2(startX, hiddenY);

        showCoroutine = null;
    }
}
