using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 웨이브 UI 관리
/// - 웨이브 번호, 남은 적, 타이머 표시
/// </summary>
public class WaveUI : MonoBehaviour
{
    // ============================================================
    // 참조
    // ============================================================
    [Header("References")]
    [SerializeField] private Text waveText;
    [SerializeField] private Text enemyCountText;
    [SerializeField] private Text timerText;

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Start()
    {
        // WaveManager 이벤트 구독
        WaveManager.Instance.OnWaveStart.AddListener(UpdateWaveNumber);
        WaveManager.Instance.OnRemainingEnemiesUpdate.AddListener(UpdateEnemyCount);
        WaveManager.Instance.OnWaveTimerUpdate.AddListener(UpdateWaveTimer);
        WaveManager.Instance.OnPrepareTimerUpdate.AddListener(UpdatePrepareTimer);

        // 초기 상태
        UpdateWaveNumber(1);
        UpdateEnemyCount(0);
    }

    // ============================================================
    // UI 업데이트
    // ============================================================
    /// <summary>
    /// 웨이브 번호 업데이트
    /// </summary>
    void UpdateWaveNumber(int wave)
    {
        waveText.text = $"Wave {wave}";
    }

    /// <summary>
    /// 남은 적 수 업데이트
    /// </summary>
    void UpdateEnemyCount(int count)
    {
        enemyCountText.text = $"남은 적: {count}";
    }

    /// <summary>
    /// 웨이브 타이머 업데이트
    /// </summary>
    void UpdateWaveTimer(float seconds)
    {
        int minutes = (int)(seconds / 60f);
        int secs = (int)(seconds % 60f);
        timerText.text = $"{minutes}:{secs:00}";
        timerText.color = Color.white;
    }

    /// <summary>
    /// 준비 타이머 업데이트
    /// </summary>
    void UpdatePrepareTimer(float seconds)
    {
        int secs = (int)seconds;
        timerText.text = $"시작까지: {secs}초";
        timerText.color = Color.yellow;
    }
}