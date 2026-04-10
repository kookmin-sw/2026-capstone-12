using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 UI 관리
/// - 모드 이름 표시
/// - 남은 적 수 표시
/// </summary>
public class WaveUI : MonoBehaviour
{
    // ============================================================
    // 참조
    // ============================================================
    [Header("References")]
    [SerializeField] private Text waveText;
    [SerializeField] private Text enemyCountText;

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Start()
    {
        if (CombatUIManager.Instance != null)
            CombatUIManager.Instance.OnRemainingEnemiesUpdate.AddListener(UpdateEnemyCount);

        UpdateWaveNumber(0);
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
        waveText.text = "Core Assault";
    }

    /// <summary>
    /// 남은 적 수 업데이트
    /// </summary>
    void UpdateEnemyCount(int count)
    {
        enemyCountText.text = $"남은 적: {count}";
    }

}
