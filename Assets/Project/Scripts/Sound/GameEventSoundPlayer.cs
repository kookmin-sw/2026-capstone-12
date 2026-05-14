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
    [SerializeField] private AudioClip spawnCoreOrbBreakClip;
    [SerializeField] private AudioClip shooterDeathClip;
    [SerializeField] private AudioClip shooterRespawnClip;
    [SerializeField] private AudioClip shooterFireClip; // Shooter 발사 클립
    [SerializeField] private AudioClip shooterReloadClip; // Shooter 재장전 클립
    [SerializeField] private AudioClip shooterWalkClip; // Shooter 걷기 클립
    [SerializeField] private AudioClip shooterRunClip; // Shooter 달리기 클립
    [SerializeField] private AudioClip getItemClip;
    [SerializeField] private AudioClip applyHealthPackClip;
    [SerializeField] private AudioClip applyAmmoPackClip;
    [SerializeField] private AudioClip basicFastEnemyGrowlClip; // Basic/Fast Enemy 울음 클립
    [SerializeField] private AudioClip tankEnemyGrowlClip; // Tank Enemy 울음 클립
    [SerializeField] private AudioClip structureAttackedClip; // 구조물 피격 클립

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
    [SerializeField] [Range(0f, 1f)] private float spawnCoreOrbBreakVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float shooterDeathVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float shooterRespawnVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float shooterFireVolume = 1f; // Shooter 발사 볼륨
    [SerializeField] [Range(0f, 1f)] private float shooterReloadVolume = 1f; // Shooter 재장전 볼륨
    [SerializeField] [Range(0f, 1f)] private float shooterWalkVolume = 1f; // Shooter 걷기 볼륨
    [SerializeField] [Range(0f, 1f)] private float shooterRunVolume = 1f; // Shooter 달리기 볼륨
    [SerializeField] [Range(0f, 1f)] private float getItemVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float applyHealthPackVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float applyAmmoPackVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float basicFastEnemyGrowlVolume = 1f; // Basic/Fast Enemy 울음 볼륨
    [SerializeField] [Range(0f, 1f)] private float tankEnemyGrowlVolume = 1f; // Tank Enemy 울음 볼륨
    [SerializeField] [Range(0f, 1f)] private float structureAttackedVolume = 1f; // 구조물 피격 볼륨
    [SerializeField] private float spatialMinDistance = 2f; // 위치 기반 사운드가 줄어들기 시작하는 거리
    [SerializeField] private float spatialMaxDistance = 10f; // 위치 기반 사운드가 들리는 최대 거리
    [SerializeField] [Range(0f, 1f)] private float supporterViewportEdgeVolume = 0.4f; // Supporter 화면 가장자리 기준 볼륨
    [SerializeField] private float supporterViewportFalloffPower = 1.25f; // Supporter 화면 중심 거리 감쇠 곡선
    [SerializeField] private Vector2 enemyGrowlPitchRange = new Vector2(0.5f, 1.5f); // 적 울음 피치 범위
    [SerializeField] private Vector2 enemyGrowlVolumeRange = new Vector2(0.82f, 1.12f); // 적 울음 볼륨 범위

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

            PlayViewportOneShot(soundType, clip, supporterVolumeScale);
            return;
        }

        GameObject soundObject = new GameObject($"OneShot_{soundType}");
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = masterVolume * GetVolume(soundType) * GetVolumeVariation(soundType);
        source.pitch = GetPitch(soundType);
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = Mathf.Max(0.01f, spatialMinDistance);
        source.maxDistance = Mathf.Max(source.minDistance, spatialMaxDistance);
        source.playOnAwake = false;
        source.loop = false;
        source.Play();

        Destroy(soundObject, clip.length + 0.1f);
    }

    /// <summary>
    /// Supporter 화면 기준 one-shot 사운드 재생
    /// </summary>
    private void PlayViewportOneShot(GameSoundType soundType, AudioClip clip, float volumeScale)
    {
        float pitch = GetPitch(soundType);
        float volume = masterVolume * GetVolume(soundType) * GetVolumeVariation(soundType) * volumeScale;
        if (Mathf.Approximately(pitch, 1f))
        {
            EnsureAudioSource();
            audioSource.PlayOneShot(clip, volume);
            return;
        }

        GameObject soundObject = new GameObject($"OneShot_{soundType}_Viewport");
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        source.loop = false;
        source.Play();

        Destroy(soundObject, (clip.length / Mathf.Max(0.01f, pitch)) + 0.1f);
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

            Camera controllerCamera = controllers[i].Cam != null ? controllers[i].Cam : controllers[i].GetComponent<Camera>();
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
            GameSoundType.SpawnCoreOrbBreak => spawnCoreOrbBreakClip,
            GameSoundType.ShooterDeath => shooterDeathClip,
            GameSoundType.ShooterRespawn => shooterRespawnClip,
            GameSoundType.ShooterFire => shooterFireClip,
            GameSoundType.ShooterReload => shooterReloadClip,
            GameSoundType.ShooterWalk => shooterWalkClip,
            GameSoundType.ShooterRun => shooterRunClip,
            GameSoundType.GetItem => getItemClip,
            GameSoundType.ApplyHealthPack => applyHealthPackClip,
            GameSoundType.ApplyAmmoPack => applyAmmoPackClip,
            GameSoundType.BasicFastEnemyGrowl => basicFastEnemyGrowlClip,
            GameSoundType.TankEnemyGrowl => tankEnemyGrowlClip,
            GameSoundType.StructureAttacked => structureAttackedClip,
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
            GameSoundType.SpawnCoreOrbBreak => spawnCoreOrbBreakVolume,
            GameSoundType.ShooterDeath => shooterDeathVolume,
            GameSoundType.ShooterRespawn => shooterRespawnVolume,
            GameSoundType.ShooterFire => shooterFireVolume,
            GameSoundType.ShooterReload => shooterReloadVolume,
            GameSoundType.ShooterWalk => shooterWalkVolume,
            GameSoundType.ShooterRun => shooterRunVolume,
            GameSoundType.GetItem => getItemVolume,
            GameSoundType.ApplyHealthPack => applyHealthPackVolume,
            GameSoundType.ApplyAmmoPack => applyAmmoPackVolume,
            GameSoundType.BasicFastEnemyGrowl => basicFastEnemyGrowlVolume,
            GameSoundType.TankEnemyGrowl => tankEnemyGrowlVolume,
            GameSoundType.StructureAttacked => structureAttackedVolume,
            _ => 1f
        };
    }

    /// <summary>
    /// 사운드 타입별 피치 조회
    /// </summary>
    private float GetPitch(GameSoundType soundType)
    {
        if (!IsEnemyGrowl(soundType))
            return 1f;

        return Random.Range(enemyGrowlPitchRange.x, enemyGrowlPitchRange.y);
    }

    /// <summary>
    /// 사운드 타입별 볼륨 변조 조회
    /// </summary>
    private float GetVolumeVariation(GameSoundType soundType)
    {
        if (!IsEnemyGrowl(soundType))
            return 1f;

        return Random.Range(enemyGrowlVolumeRange.x, enemyGrowlVolumeRange.y);
    }

    /// <summary>
    /// 적 울음 사운드 여부
    /// </summary>
    private bool IsEnemyGrowl(GameSoundType soundType)
    {
        return soundType == GameSoundType.BasicFastEnemyGrowl || soundType == GameSoundType.TankEnemyGrowl;
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
