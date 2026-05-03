using UnityEngine;

public class PingSoundPlayer : MonoBehaviour
{
    // 핑 타입별 로컬 재생 사운드 설정
    [Header("Clips")]
    [SerializeField] private AudioClip normalPingClip;
    [SerializeField] private AudioClip dangerPingClip;
    [SerializeField] private AudioClip helpPingClip;

    [Header("Playback")]
    [SerializeField] [Range(0f, 1f)] private float pingSoundVolume = 1f;

    private AudioSource audioSource;

    /// <summary>
    /// 핑 사운드 재생 소스 초기화
    /// </summary>
    private void Awake()
    {
        EnsureAudioSource();
    }

    /// <summary>
    /// 핑 타입에 맞는 AudioClip을 현재 클라이언트에서 재생
    /// </summary>
    public void Play(ShooterPingType pingType)
    {
        EnsureAudioSource();

        AudioClip clip = GetPingClip(pingType);
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, pingSoundVolume);
    }

    /// <summary>
    /// 핑 타입에 대응하는 AudioClip 반환
    /// </summary>
    private AudioClip GetPingClip(ShooterPingType pingType)
    {
        switch (pingType)
        {
            case ShooterPingType.Danger:
                return dangerPingClip;
            case ShooterPingType.Help:
                return helpPingClip;
            default:
                return normalPingClip;
        }
    }

    /// <summary>
    /// 핑 사운드 재생에 사용할 AudioSource 보장
    /// </summary>
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
