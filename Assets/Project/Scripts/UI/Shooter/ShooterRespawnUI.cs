using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슈터 사망 시 풀스크린 부활 대기 UI
/// - 반투명 어두운 오버레이
/// - 원형 프로그레스 바 + 카운트다운 숫자
/// - 로컬 슈터 클라이언트에서만 활성화
/// </summary>
public class ShooterRespawnUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("오버레이 전체 루트 (Canvas 하위 Panel)")]
    [SerializeField] private GameObject overlayRoot;
    [Tooltip("원형으로 줄어드는 Image (Image Type: Filled, Fill Method: Radial 360)")]
    [SerializeField] private Image circularFill;
    [Tooltip("남은 초를 표시할 Text")]
    [SerializeField] private Text countdownText;
    [Tooltip("'부활까지...' 같은 보조 텍스트 (선택)")]
    [SerializeField] private Text subText;

    [Header("Settings")]
    [Tooltip("ShooterRespawnNet 의 respawnDelay 와 동일하게 설정")]
    [SerializeField] private float respawnDelay = 5f;

    private HealthManager shooterHealthManager;
    private Coroutine countdownCoroutine;

    private void Start()
    {
        if (!IsLocalShooter())
        {
            enabled = false;
            return;
        }

        StartCoroutine(FindAndSubscribe());

        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);
    }

    private IEnumerator FindAndSubscribe()
    {
        while (shooterHealthManager == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                shooterHealthManager = playerObj.GetComponent<HealthManager>();
            if (shooterHealthManager == null)
                yield return new WaitForSeconds(0.3f);
        }

        shooterHealthManager.OnDied += OnShooterDied;
        shooterHealthManager.OnRevived += OnShooterRevived;
    }

    private void Unsubscribe()
    {
        if (shooterHealthManager == null) return;
        shooterHealthManager.OnDied -= OnShooterDied;
        shooterHealthManager.OnRevived -= OnShooterRevived;
    }

    private void OnShooterDied()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(true);

        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private void OnShooterRevived()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    private IEnumerator CountdownRoutine()
    {
        float elapsed = 0f;
        float delay = Mathf.Max(0.1f, respawnDelay);

        while (elapsed < delay)
        {
            elapsed += Time.deltaTime;
            float remaining = Mathf.Max(0f, delay - elapsed);

            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(remaining).ToString();

            if (circularFill != null)
                circularFill.fillAmount = 1f - (elapsed / delay);

            yield return null;
        }

        if (countdownText != null) countdownText.text = "0";
        if (circularFill != null) circularFill.fillAmount = 0f;
        countdownCoroutine = null;
    }

    private bool IsLocalShooter()
    {
        if (!PhotonNetwork.IsConnected)
            return true; // 에디터 단독 테스트는 슈터로 처리

        var props = PhotonNetwork.LocalPlayer.CustomProperties;
        return props.ContainsKey("Role") && (string)props["Role"] == "Shooter";
    }
}
