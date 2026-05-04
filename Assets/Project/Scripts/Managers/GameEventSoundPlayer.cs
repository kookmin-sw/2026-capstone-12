using UnityEngine;

public class GameEventSoundPlayer : MonoBehaviour
{
    public static GameEventSoundPlayer Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] private AudioClip buildStructureClip;
    [SerializeField] private AudioClip supplyItemClip;
    [SerializeField] private AudioClip shooterDeathClip;

    [Header("Playback")]
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float buildStructureVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float supplyItemVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float shooterDeathVolume = 1f;

    private AudioSource audioSource;

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

    // 게임 이벤트 타입에 맞는 one-shot 사운드 재생
    public void Play(GameEventSoundType soundType)
    {
        EnsureAudioSource();

        AudioClip clip = GetClip(soundType);
        if (clip == null)
            return;

        audioSource.PlayOneShot(clip, masterVolume * GetVolume(soundType));
    }

    // 게임 이벤트 타입별 AudioClip 조회
    private AudioClip GetClip(GameEventSoundType soundType)
    {
        return soundType switch
        {
            GameEventSoundType.BuildStructure => buildStructureClip,
            GameEventSoundType.SupplyItem => supplyItemClip,
            GameEventSoundType.ShooterDeath => shooterDeathClip,
            _ => null
        };
    }

    // 게임 이벤트 타입별 개별 볼륨 조회
    private float GetVolume(GameEventSoundType soundType)
    {
        return soundType switch
        {
            GameEventSoundType.BuildStructure => buildStructureVolume,
            GameEventSoundType.SupplyItem => supplyItemVolume,
            GameEventSoundType.ShooterDeath => shooterDeathVolume,
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
