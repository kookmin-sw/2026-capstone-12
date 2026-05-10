using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PhotonView))]
public class BuildingHealthNet : MonoBehaviourPun
{
    [Header("HP")]
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;

    public float MaxHp => maxHp;
    public float CurrentHp => currentHp;
    public bool IsFullHp => currentHp >= maxHp;

    public void Init(float maxHp)
    {
        this.maxHp = maxHp;
        currentHp = maxHp;
    }

    public void MasterTakeDamage(float damage)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (damage <= 0f) return;
        if (!CanTakeDamage(damage)) return;

        ApplyDamageInternal(damage);
    }

    public void MasterRepair(float amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (amount <= 0f) return;

        float newHp = Mathf.Clamp(currentHp + amount, 0f, maxHp);
        if (Mathf.Approximately(newHp, currentHp)) return;

        currentHp = newHp;
        photonView.RPC(nameof(RpcSetHp), RpcTarget.All, currentHp);
    }

    public void MasterSetHp(float hp)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        currentHp = Mathf.Clamp(hp, 0f, maxHp);
        photonView.RPC(nameof(RpcSetHp), RpcTarget.All, currentHp);

        if (currentHp <= 0f)
            MasterKill();
    }

    private void ApplyDamageInternal(float damage)
    {
        float newHp = currentHp - damage;
        currentHp = Mathf.Clamp(newHp, 0f, maxHp);

        photonView.RPC(nameof(RpcSetHp), RpcTarget.All, currentHp);
        NotifyDamagedByMaster(damage);

        if (currentHp <= 0f)
            MasterKill();
    }

    private void MasterKill()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Vector3 destroyedPosition = transform.position;
        if (GetComponent<SpawnCore>() == null && GetComponent<CommandTower>() == null)
            SoundNet.Instance?.RequestPlayAt(GameSoundType.BuildingDestroyed, destroyedPosition);

        NotifyDestroyedByMaster();
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    private void RpcSetHp(float hp)
    {
        currentHp = Mathf.Clamp(hp, 0f, maxHp);
    }

    private bool CanTakeDamage(float damage)
    {
        // 동일 오브젝트 내 데미지 허용 컴포넌트 검사
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour is IBuildingDamageGate gate && !gate.CanTakeDamage(this, damage))
                return false;
        }

        return true;
    }

    private void NotifyDestroyedByMaster()
    {
        // 동일 대상 중복 호출 방지 집합
        HashSet<Object> notifiedTargets = new HashSet<Object>();

        // 동일 오브젝트 내 파괴 알림 수신 컴포넌트 호출
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            IBuildingDestroyedListener listener = behaviour as IBuildingDestroyedListener;
            if (listener == null)
                continue;

            Object target = behaviour as Object;
            if (target == null || !notifiedTargets.Add(target))
                continue;

            listener.OnBuildingDestroyedByMaster(this);
        }
    }

    private void NotifyDamagedByMaster(float damage)
    {
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            IBuildingDamagedListener listener = behaviour as IBuildingDamagedListener;
            if (listener == null)
                continue;

            listener.OnBuildingDamagedByMaster(this, damage);
        }
    }
}

public interface IBuildingDamageGate
{
    // 데미지 허용 여부 판단 계약
    bool CanTakeDamage(BuildingHealthNet buildingHealth, float incomingDamage);
}

public interface IBuildingDestroyedListener
{
    // 파괴 직전 후처리 알림 계약
    void OnBuildingDestroyedByMaster(BuildingHealthNet buildingHealth);
}

public interface IBuildingDamagedListener
{
    void OnBuildingDamagedByMaster(BuildingHealthNet buildingHealth, float damage);
}
