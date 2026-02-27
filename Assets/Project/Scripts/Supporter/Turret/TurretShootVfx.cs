using UnityEngine;

public class TurretShootVfx : MonoBehaviour
{
    [SerializeField] private Transform shootEffectRoot;

    private ParticleSystem[] particles;

    private void Awake()
    {
        if (shootEffectRoot != null)
            particles = shootEffectRoot.GetComponentsInChildren<ParticleSystem>(true);
        else
            particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    
    public void PlayShootVfx()
    {
        if (particles == null || particles.Length == 0)
            return;

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Play(true);
        }
    }
}