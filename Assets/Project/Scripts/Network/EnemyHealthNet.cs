using Photon.Pun;
using UnityEngine;
using System.Collections;

public class EnemyHealthNet : MonoBehaviourPun
{
    public static EnemyHealthNet Instance { get; private set; }

    [SerializeField] private GameObject hitEffectPrefab;

    private void Awake()
    {
        Instance = this;
    }

    // 터렛/슈터 등이 호출: 마스터에서만 실제 적용
    public void MasterApplyDamage(int enemyViewId, float damage)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView enemyPv = PhotonView.Find(enemyViewId);
        if (enemyPv == null) return;

        EnemyHealth eh = enemyPv.GetComponent<EnemyHealth>();
        if (eh == null || eh.IsDead) return;

        // 데미지 계산
        float actualDamage = damage * (1f - eh.DamageReduction);
        float newHp = Mathf.Max(0f, eh.CurrentHp - actualDamage);

        // HP 브로드캐스트
        photonView.RPC(nameof(RpcSetEnemyHp), RpcTarget.All, enemyViewId, newHp, eh.MaxHp);

        // 사망
        if (newHp <= 0f)
        {
            // 애니메이션 동기화
            EnemyAnimationNet animationNet = enemyPv.GetComponent<EnemyAnimationNet>();
            animationNet?.PlayDeath();

            // 보상 처리
            if (ResourceNet.Instance != null)
            {
                ResourceNet.Instance.MasterAddMoney(eh.MoneyReward);
                ResourceNet.Instance.MasterAddScore(eh.ScoreReward);
                ResourceNet.Instance.MasterAddKills(1);
            }

            // 경험치 지급
            if (ShooterLevelNet.Instance != null)
                ShooterLevelNet.Instance.MasterAddXp(eh.XpReward);

            // 정화 게이지 지급
            if (ShooterPurificationNet.Instance != null)
                ShooterPurificationNet.Instance.MasterAddPurificationEnergyForEnemy(eh);

            // EnemyManager/CombatUIManager 연동도 마스터에서만
            if (EnemyManager.Instance != null)
                EnemyManager.Instance.RemoveEnemy(enemyPv.gameObject);

            // 죽는 애니메이션 재생 후 삭제 (딜레이)
            StartCoroutine(DestroyAfterDeathAnimation(enemyPv));
        }
    }

    private IEnumerator DestroyAfterDeathAnimation(PhotonView enemyPv)
    {
        yield return new WaitForSeconds(1.5f);

        if (enemyPv != null && enemyPv.gameObject != null)
            PhotonNetwork.Destroy(enemyPv.gameObject);
    }

    [PunRPC]
    private void RpcSetEnemyHp(int enemyViewId, float hp, float maxHp)
    {
        PhotonView enemyPv = PhotonView.Find(enemyViewId);
        if (enemyPv == null) return;

        EnemyHealth eh = enemyPv.GetComponent<EnemyHealth>();
        if (eh == null) return;

        bool tookDamage = hp < eh.CurrentHp && !eh.IsDead;
        Vector3 hitPos = enemyPv.transform.position + Vector3.up * 0.8f;

        eh.SetHealthFromNet(hp, maxHp);

        if (tookDamage && hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, hitPos, Quaternion.Euler(-90f, 0f, 0f));
            Destroy(effect, 1.5f);
        }
    }
}
