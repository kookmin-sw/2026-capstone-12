using System.Collections;
using UnityEngine;

public class TurretShooter : GhostDisabledBehaviour
{
    private TurretTargeting targeting;

    [Header("Transforms")]
    public Transform muzzle;
    public Transform shootEffectRoot;

    [Header("Shoot Settings")]
    public float fireRate = 4f;
    public float damage = 10f;
    public float range = 30f;

    private float fireTimer;
    private ParticleSystem[] particles;

    private void Awake()
    {
        targeting = GetComponent<TurretTargeting>();
        if (shootEffectRoot != null)
            particles = shootEffectRoot.GetComponentsInChildren<ParticleSystem>(true);
    }

    private void Update()
    {
        if (targeting == null || muzzle == null) return;

        Transform target = targeting.CurrentTarget;
        if (target == null) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        fireTimer = 1f / Mathf.Max(0.01f, fireRate);

        PlayShootVfx();
        FireOnce(target);
    }

    private void PlayShootVfx()
    {
        if (particles == null || particles.Length == 0)
            return;

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Play(true);
        }
    }

    private void FireOnce(Transform target)
    {
        Vector3 origin = muzzle.position;
        Vector3 dir = (target.position - origin).normalized;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, targeting.enemyMask, QueryTriggerInteraction.Ignore))
        {
            // HP 감소 로직 추가
            /*
            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damage);
            */
        }
    }
}

/*public interface IDamageable
{
    void TakeDamage(float amount);
}*/
