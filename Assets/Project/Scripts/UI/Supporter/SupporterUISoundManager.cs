using UnityEngine;

public class SupporterUISoundManager : MonoBehaviour
{
    public static SupporterUISoundManager Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip buildStructureClip;
    [SerializeField] private AudioClip resourceLackClip;
    [SerializeField] private AudioClip repairClip;
    [SerializeField] private AudioClip sellClip;
    [SerializeField] private AudioClip shooterDeathClip;
    [SerializeField] private AudioClip supplyItemClip;

    [Header("Playback")]
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float hoverVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float clickVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float buildStructureVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float resourceLackVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float repairVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float sellVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float shooterDeathVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float supplyItemVolume = 1f;
    [SerializeField] private float hoverCooldown = 0.04f; // 겹친 UI 요소의 호버음 중복 방지

    private AudioSource audioSource;
    private float nextHoverTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureAudioSource();
    }

    public void Play(SupporterUISoundType soundType)
    {
        EnsureAudioSource();

        AudioClip clip = GetClip(soundType);
        if (clip == null)
            return;

        if (soundType == SupporterUISoundType.Hover)
        {
            if (Time.unscaledTime < nextHoverTime)
                return;

            nextHoverTime = Time.unscaledTime + Mathf.Max(0f, hoverCooldown);
        }

        audioSource.PlayOneShot(clip, masterVolume * GetVolume(soundType));
    }

    // 사운드 타입별 AudioClip 조회
    private AudioClip GetClip(SupporterUISoundType soundType)
    {
        return soundType switch
        {
            SupporterUISoundType.Hover => hoverClip,
            SupporterUISoundType.Click => clickClip,
            SupporterUISoundType.BuildStructure => buildStructureClip,
            SupporterUISoundType.ResourceLack => resourceLackClip,
            SupporterUISoundType.Repair => repairClip,
            SupporterUISoundType.Sell => sellClip,
            SupporterUISoundType.ShooterDeath => shooterDeathClip,
            SupporterUISoundType.SupplyItem => supplyItemClip,
            _ => null
        };
    }

    // 사운드 타입별 개별 볼륨 조회
    private float GetVolume(SupporterUISoundType soundType)
    {
        return soundType switch
        {
            SupporterUISoundType.Hover => hoverVolume,
            SupporterUISoundType.Click => clickVolume,
            SupporterUISoundType.BuildStructure => buildStructureVolume,
            SupporterUISoundType.ResourceLack => resourceLackVolume,
            SupporterUISoundType.Repair => repairVolume,
            SupporterUISoundType.Sell => sellVolume,
            SupporterUISoundType.ShooterDeath => shooterDeathVolume,
            SupporterUISoundType.SupplyItem => supplyItemVolume,
            _ => 1f
        };
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }
}
