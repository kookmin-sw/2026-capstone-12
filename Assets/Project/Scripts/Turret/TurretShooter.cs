using System.Collections;
using UnityEngine;
using Photon.Pun;

public class TurretShooter : GhostDisabledBehaviour
{
    private TurretTargeting targeting;
    private TurretVfxNet net;

    [Header("Transforms")]
    public Transform muzzle;

    [Header("Shoot Settings")]
    public float fireRate = 4f;
    public float damage = 10f;
    public float range = 30f;

    [Header("Fire Condition")]
    public float fireConeDegrees = 7f;  // 이 각도 이내면 발사 허용

    private float fireTimer;

    private void Awake()
    {
        targeting = GetComponent<TurretTargeting>();
        net = GetComponent<TurretVfxNet>();
    }

    private void Update()
    {   
        if (!PhotonNetwork.IsMasterClient) return;
        if (targeting == null || muzzle == null) return;

        Transform target = targeting.CurrentTarget;
        if (target == null) return;

        Vector3 toTarget = (target.position - muzzle.position);
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        float angle = Vector3.Angle(muzzle.forward, toTarget.normalized);
        if (angle > fireConeDegrees) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        fireTimer = 1f / Mathf.Max(0.01f, fireRate);

        net?.BroadcastShotFx();

        FireOnce(target);
    }

    private void FireOnce(Transform target)
    {
        Vector3 origin = muzzle.position;
        Vector3 dir = (target.position - origin).normalized;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, targeting.enemyMask, QueryTriggerInteraction.Ignore))
        {
            // 맞은 적 오브젝트의 PhotonView를 찾기
            PhotonView enemyPv = hit.collider.GetComponentInParent<PhotonView>();
            if (enemyPv == null)
                return;

            // 마스터 권위로 데미지 적용
            if (EnemyHealthNet.Instance != null)
                EnemyHealthNet.Instance.MasterApplyDamage(enemyPv.ViewID, damage);
        }
    }
}
