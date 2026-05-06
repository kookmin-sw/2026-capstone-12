using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 서포터 PlayerStatusPanel 의 PlayerIcon 옆에 슈터 부활 대기 시간을 표시
/// - 평소에는 숨겨져 있다가 슈터 사망 시 활성화
/// - 로컬 서포터 클라이언트에서만 활성화
/// </summary>
public class SupporterRespawnUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("부활 인디케이터 루트 오브젝트 (평소 비활성)")]
    [SerializeField] private GameObject indicatorRoot;
    [Tooltip("'부활 3초' 형태로 표시할 TextMeshPro")]
    [SerializeField] private Text countdownText;
    [Tooltip("PlayerIcon 위에 겹쳐 보여줄 원형 딤 이미지 (Image Type: Filled, 선택)")]
    [SerializeField] private Image circularFill;

    [Header("Settings")]
    [Tooltip("ShooterRespawnNet 의 respawnDelay 와 동일하게 설정")]
    [SerializeField] private float respawnDelay = 5f;

    private HealthManager shooterHealthManager;
    private Coroutine countdownCoroutine;

    private void Start()
    {
        if (!IsLocalSupporter())
        {
            enabled = false;
            return;
        }

        if (indicatorRoot != null)
            indicatorRoot.SetActive(false);

        StartCoroutine(FindAndSubscribe());
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);
    }

    private IEnumerator FindAndSubscribe()
    {
        // 씬에서 슈터 Player 오브젝트가 생성될 때까지 대기
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
        if (indicatorRoot != null)
            indicatorRoot.SetActive(true);

        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private void OnShooterRevived()
    {
        if (indicatorRoot != null)
            indicatorRoot.SetActive(false);

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
                countdownText.text = $"부활 {Mathf.CeilToInt(remaining)}초";

            if (circularFill != null)
                circularFill.fillAmount = 1f - (elapsed / delay);

            yield return null;
        }

        if (countdownText != null) countdownText.text = "부활 중...";
        if (circularFill != null) circularFill.fillAmount = 0f;
        countdownCoroutine = null;
    }

    private bool IsLocalSupporter()
    {
        if (!PhotonNetwork.IsConnected)
            return false; // 에디터 단독 테스트는 서포터 UI 비활성

        var props = PhotonNetwork.LocalPlayer.CustomProperties;
        return props.ContainsKey("Role") && (string)props["Role"] == "Supporter";
    }
}
