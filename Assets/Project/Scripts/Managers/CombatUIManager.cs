using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;

/// <summary>
/// 전투 UI 중계 관리
/// - 적 수 동기화
/// </summary>
public class CombatUIManager : MonoBehaviourPun
{
    // ============================================================
    // 싱글턴
    // ============================================================
    public static CombatUIManager Instance { get; private set; }

    public UnityEventInt OnRemainingEnemiesUpdate = new UnityEventInt(); // 남은 적 수 업데이트

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetRemainingEnemyCount(int count)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC(nameof(RPC_UpdateEnemyCount), RpcTarget.All, count);
    }

    [PunRPC]
    private void RPC_UpdateEnemyCount(int count)
    {
        OnRemainingEnemiesUpdate.Invoke(count);
    }
}

/// <summary>
/// 적 타입 열거형
/// </summary>
public enum EnemyType
{
    Basic,
    Tank,
    Fast
}
