using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SlowTowerAura : GhostDisabledBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectRadius = 5f;
    [SerializeField] private LayerMask enemyMask = 1 << 7;
    [SerializeField] private float scanInterval = 0.1f;
    [SerializeField] private float effectRefreshDuration = 0.25f;

    [Header("Slow Effect")]
    [SerializeField] private float moveSpeedMultiplier = 0.6f;
    [SerializeField] private float attackSpeedMultiplier = 0.6f;

    private readonly Collider[] overlapResults = new Collider[64];
    private readonly HashSet<EnemyAI> affectedEnemies = new HashSet<EnemyAI>();
    private readonly HashSet<EnemyAI> scannedEnemies = new HashSet<EnemyAI>();

    private PhotonView photonView;
    private float scanTimer;
    private int sourceId;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    protected override void Start()
    {
        base.Start();

        sourceId = photonView != null && photonView.ViewID > 0
            ? photonView.ViewID
            : GetInstanceID();
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f)
            return;

        scanTimer = scanInterval;
        ScanAndApply();
    }

    private void ScanAndApply()
    {
        scannedEnemies.Clear();

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectRadius,
            overlapResults,
            enemyMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];
            if (hit == null)
                continue;

            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy == null || !scannedEnemies.Add(enemy))
                continue;

            enemy.ApplySlow(sourceId, moveSpeedMultiplier, attackSpeedMultiplier, effectRefreshDuration);
        }

        foreach (EnemyAI enemy in affectedEnemies)
        {
            if (enemy == null || scannedEnemies.Contains(enemy))
                continue;

            enemy.RemoveSlow(sourceId);
        }

        affectedEnemies.Clear();
        foreach (EnemyAI enemy in scannedEnemies)
            affectedEnemies.Add(enemy);
    }

    private void OnDisable()
    {
        ClearAppliedSlows();
    }

    private void OnDestroy()
    {
        ClearAppliedSlows();
    }

    private void ClearAppliedSlows()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        foreach (EnemyAI enemy in affectedEnemies)
        {
            if (enemy != null)
                enemy.RemoveSlow(sourceId);
        }

        affectedEnemies.Clear();
        scannedEnemies.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
