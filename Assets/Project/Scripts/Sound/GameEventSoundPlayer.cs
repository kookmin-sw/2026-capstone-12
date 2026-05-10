using Photon.Pun;
using UnityEngine;

public class GameEventSoundPlayer : MonoBehaviour
{
    public static GameEventSoundPlayer Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] private AudioClip normalPingClip;
    [SerializeField] private AudioClip dangerPingClip;
    [SerializeField] private AudioClip helpPingClip;
    [SerializeField] private AudioClip buildStructureClip;
    [SerializeField] private AudioClip supplyItemClip;
    [SerializeField] private AudioClip turretAttackClip;
    [SerializeField] private AudioClip buildingDestroyedClip;
    [SerializeField] private AudioClip spawnCoreDestroyedClip;
    [SerializeField] private AudioClip slowTowerActivatedClip;
    [SerializeField] private AudioClip lightPylonActivatedClip;
    [SerializeField] private AudioClip purificationBeaconActivatedClip;
    [SerializeField] private AudioClip shooterDeathClip;
    [SerializeField] private AudioClip shooterRespawnClip;
    [SerializeField] private AudioClip getItemClip;
    [SerializeField] private AudioClip applyHealthPackClip;
    [SerializeField] private AudioClip applyAmmoPackClip;

    [Header("Playback")]
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float normalPingVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float dangerPingVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float helpPingVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float buildStructureVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float supplyItemVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float turretAttackVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float buildingDestroyedVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float spawnCoreDestroyedVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float slowTowerActivatedVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float lightPylonActivatedVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float purificationBeaconActivatedVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float shooterDeathVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float shooterRespawnVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float getItemVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float applyHealthPackVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float applyAmmoPackVolume = 1f;
    [SerializeField] private float spatialMinDistance = 2f; // 위치 기반 사운드가 줄어들기 시작하는 거리
    [SerializeField] private float spatialMaxDistance = 10f; // 위치 기반 사운드가 들리는 최대 거리
    [SerializeField] [Range(0f, 1f)] private float supporterViewportEdgeVolume = 0.4f; // Supporter 화면 가장자리 기준 볼륨
    [SerializeField] private float supporterViewportFalloffPower = 1.25f; // Supporter 화면 중심 거리 감쇠 곡선

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

    // 게임 사운드 타입에 맞는 one-shot 사운드 재생
    public void Play(GameSoundType soundType)
    {
        EnsureAudioSource();

        AudioClip clip = GetClip(soundType);
        if (clip == null)
            return;

        audioSource.PlayOneShot(clip, masterVolume * GetVolume(soundType));
    }

    // 게임 사운드 타입에 맞는 위치 기반 one-shot 사운드 재생
    public void PlayAt(GameSoundType soundType, Vector3 position)
    {
        AudioClip clip = GetClip(soundType);
        if (clip == null)
            return;

        if (TryGetSupporterViewportVolume(position, out float supporterVolumeScale))
        {
            if (supporterVolumeScale <= 0f)
                return;

            EnsureAudioSource();
            audioSource.PlayOneShot(clip, masterVolume * GetVolume(soundType) * supporterVolumeScale);
            return;
        }

        GameObject soundObject = new GameObject($"OneShot_{soundType}");
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = masterVolume * GetVolume(soundType);
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = Mathf.Max(0.01f, spatialMinDistance);
        source.maxDistance = Mathf.Max(source.minDistance, spatialMaxDistance);
        source.playOnAwake = false;
        source.loop = false;
        source.Play();

        Destroy(soundObject, clip.length + 0.1f);
    }

    // Supporter는 카메라 화면 기준으로 월드 사운드 청취량 계산
    private bool TryGetSupporterViewportVolume(Vector3 position, out float volumeScale)
    {
        volumeScale = 1f;

        if (!TryFindSupporterViewportCamera(out Camera cam))
            return false;

        if (cam == null || !cam.isActiveAndEnabled)
        {
            volumeScale = 0f;
            return true;
        }

        Vector3 viewport = cam.WorldToViewportPoint(position);
        if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
        {
            volumeScale = 0f;
            return true;
        }

        float centerDistance = Vector2.Distance(new Vector2(viewport.x, viewport.y), new Vector2(0.5f, 0.5f));
        float normalizedDistance = Mathf.Clamp01(centerDistance / 0.70710678f); // 화면 중심에서 모서리까지의 정규화 거리
        float falloff = Mathf.Pow(normalizedDistance, Mathf.Max(0.01f, supporterViewportFalloffPower));
        volumeScale = Mathf.Lerp(1f, supporterViewportEdgeVolume, falloff);
        return true;
    }

    // Supporter viewport 계산에 사용할 카메라 조회
    private bool TryFindSupporterViewportCamera(out Camera cam)
    {
        cam = FindActiveTopDownCamera();
        if (!IsLocalSupporter() && cam == null)
            return false;

        return true;
    }

    // 활성 TopDownCameraController 카메라 조회
    private Camera FindActiveTopDownCamera()
    {
        TopDownCameraController[] controllers = FindObjectsOfType<TopDownCameraController>();
        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] == null || !controllers[i].isActiveAndEnabled)
                continue;

            Camera controllerCamera = controllers[i].cam != null ? controllers[i].cam : controllers[i].GetComponent<Camera>();
            if (controllerCamera != null && controllerCamera.isActiveAndEnabled)
                return controllerCamera;
        }

        return null;
    }

    // 현재 클라이언트 Supporter 역할 여부 판정
    private bool IsLocalSupporter()
    {
        if (PhotonNetwork.LocalPlayer == null)
            return false;

        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Role", out object roleValue))
            return false;

        return roleValue as string == "Supporter";
    }

    // 게임 사운드 타입별 AudioClip 조회
    private AudioClip GetClip(GameSoundType soundType)
    {
        return soundType switch
        {
            GameSoundType.PingNormal => normalPingClip,
            GameSoundType.PingDanger => dangerPingClip,
            GameSoundType.PingHelp => helpPingClip,
            GameSoundType.BuildStructure => buildStructureClip,
            GameSoundType.SupplyItem => supplyItemClip,
            GameSoundType.TurretAttack => turretAttackClip,
            GameSoundType.BuildingDestroyed => buildingDestroyedClip,
            GameSoundType.SpawnCoreDestroyed => spawnCoreDestroyedClip,
            GameSoundType.SlowTowerActivated => slowTowerActivatedClip,
            GameSoundType.LightPylonActivated => lightPylonActivatedClip,
            GameSoundType.PurificationBeaconActivated => purificationBeaconActivatedClip,
            GameSoundType.ShooterDeath => shooterDeathClip,
            GameSoundType.ShooterRespawn => shooterRespawnClip,
            GameSoundType.GetItem => getItemClip,
            GameSoundType.ApplyHealthPack => applyHealthPackClip,
            GameSoundType.ApplyAmmoPack => applyAmmoPackClip,
            _ => null
        };
    }

    // 게임 사운드 타입별 개별 볼륨 조회
    private float GetVolume(GameSoundType soundType)
    {
        return soundType switch
        {
            GameSoundType.PingNormal => normalPingVolume,
            GameSoundType.PingDanger => dangerPingVolume,
            GameSoundType.PingHelp => helpPingVolume,
            GameSoundType.BuildStructure => buildStructureVolume,
            GameSoundType.SupplyItem => supplyItemVolume,
            GameSoundType.TurretAttack => turretAttackVolume,
            GameSoundType.BuildingDestroyed => buildingDestroyedVolume,
            GameSoundType.SpawnCoreDestroyed => spawnCoreDestroyedVolume,
            GameSoundType.SlowTowerActivated => slowTowerActivatedVolume,
            GameSoundType.LightPylonActivated => lightPylonActivatedVolume,
            GameSoundType.PurificationBeaconActivated => purificationBeaconActivatedVolume,
            GameSoundType.ShooterDeath => shooterDeathVolume,
            GameSoundType.ShooterRespawn => shooterRespawnVolume,
            GameSoundType.GetItem => getItemVolume,
            GameSoundType.ApplyHealthPack => applyHealthPackVolume,
            GameSoundType.ApplyAmmoPack => applyAmmoPackVolume,
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
