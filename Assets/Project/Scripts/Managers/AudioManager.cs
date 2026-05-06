using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("레벨업")]
    [SerializeField] private AudioClip levelUpClip;

    [Header("몬스터 강화")]
    [SerializeField] private AudioClip enemyBuffClip;

    [Header("슈터 피격")]
    [SerializeField] private AudioClip shooterHitClip;

    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    public void PlayLevelUp()
    {
        PlaySFX(levelUpClip);
    }

    public void PlayEnemyBuff()
    {
        PlaySFX(enemyBuffClip);
    }

    public void PlayShooterHit()
    {
        PlaySFX(shooterHitClip);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
