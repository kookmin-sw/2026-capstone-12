using UnityEngine;

public class TurretTargeting : MonoBehaviour
{
    public float detectRadius = 10f;
    public LayerMask enemyMask;

    public Transform CurrentTarget { get; private set; }

    public float scanInterval = 0.15f;
    private float scanTimer;

    private void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = scanInterval;

        CurrentTarget = FindNearestEnemy();
    }

    private Transform FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, enemyMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return null;

        Transform nearest = null;
        float bestSqr = float.MaxValue;

        Vector3 p = transform.position;
        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            float sqr = (t.position - p).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = t;
            }
        }
        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
