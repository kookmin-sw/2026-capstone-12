using System.Collections;
using Photon.Pun;
using UnityEngine;

public class SupportPickupItem : MonoBehaviour
{
    [SerializeField] private SupportItemKind kind; // 회복 효과 선택용 아이템 종류
    [SerializeField] private SupporterItemSO item; // 소비 효과 데이터
    [SerializeField] private float floatAmplitude = 0.18f; // 둥실거림 높이
    [SerializeField] private float floatSpeed = 1.6f; // 둥실거림 속도
    [SerializeField] private float rotateSpeed = 45f; // 아이템 회전 속도

    private int supportId = -1; // 네트워크 소비 동기화용 고유 ID
    private bool consuming; // 중복 소비 방지 상태
    private bool applySoundPlayed; // 단계별 회복 중 효과음 반복 방지
    private Vector3 basePosition; // 부유 애니메이션 기준 위치

    public int SupportId => supportId; // 네트워크 조회용 고유 ID

    // 부유 기준 위치와 충돌 설정 초기화
    private void Awake()
    {
        basePosition = transform.position;
        EnsurePickupPhysics();
    }

    // 아이템 부유와 회전 애니메이션 처리
    private void Update()
    {
        if (consuming)
            return;

        float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude; // 시간 기반 상하 이동량
        transform.position = basePosition + Vector3.up * offset;
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    // 슈터 충돌 시 소비 요청
    private void OnTriggerEnter(Collider other)
    {
        if (consuming || !other.CompareTag("Player"))
            return;

        if (!HasLocalShooterAuthority(other.gameObject))
            return;

        if (BuildNetManager.Instance != null && supportId >= 0)
        {
            BuildNetManager.Instance.RequestConsumeSupport(supportId);
        }
        else
        {
            StartConsuming();
        }
    }

    // 네트워크 생성 후 아이템 종류 설정
    public void Configure(int id, SupportItemKind itemKind)
    {
        supportId = id;
        kind = itemKind;
        item = null;
        basePosition = transform.position;
        EnsurePickupPhysics();
    }

    // 네트워크 생성 후 SO 기반 아이템 정보 설정
    public void Configure(int id, SupporterItemSO itemDefinition)
    {
        supportId = id;
        item = itemDefinition;
        if (itemDefinition != null)
            kind = itemDefinition.kind;

        basePosition = transform.position;
        EnsurePickupPhysics();
    }

    // 아이템 소비 연출과 회복 시작
    public void StartConsuming()
    {
        if (consuming)
            return;

        StartCoroutine(ConsumeRoutine());
    }

    // 단계별 회복과 축소 제거 처리
    private IEnumerator ConsumeRoutine()
    {
        consuming = true;
        PlayGetItemSound();
        PlayApplySoundOnce();

        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;

        float duration = item != null ? Mathf.Max(0.1f, item.effectDuration) : 1f;
        int steps = Mathf.Max(1, Mathf.CeilToInt(duration / 0.2f)); // 느린 회복을 위한 분할 횟수
        float stepDelay = duration / steps; // 각 회복 단계 간격

        for (int i = 0; i < steps; i++)
        {
            ApplyStepEffect(item, steps);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 1f / (steps - i));
            yield return new WaitForSeconds(stepDelay);
        }

        Destroy(gameObject);
    }

    // 회복 단계별 효과 적용
    private void ApplyStepEffect(SupporterItemSO itemDefinition, int steps)
    {
        if (!IsLocalShooter())
            return;

        if (itemDefinition == null)
            return;

        if (itemDefinition.healAmount > 0f)
        {
            if (ShooterHealthNet.Instance != null)
            {
                ShooterHealthNet.Instance.RequestHealShooter(itemDefinition.healAmount / steps);
            }
            else
            {
                HealthManager healthManager = FindShooterHealthManager();
                if (healthManager != null)
                {
                    float maxHp = healthManager.MaxHp; // 체력 상한 보정 기준
                    float hp = Mathf.Min(maxHp, healthManager.CurrentHp + itemDefinition.healAmount / steps); // 단계별 회복 후 체력
                    healthManager.SetHpFromNetwork(hp, maxHp);
                }
            }
        }

        if (itemDefinition.ammoAmount > 0)
        {
            AmmoManager ammoManager = FindShooterAmmoManager();
            if (ammoManager == null)
                return;

            int amount = Mathf.CeilToInt((float)itemDefinition.ammoAmount / steps); // 단계별 탄약 회복량
            ammoManager.AddReserveAmmo(amount);
            ShooterAmmoNet.Instance?.SyncAmmoFromShooter(ammoManager.CurrentAmmo, ammoManager.ReserveAmmo, ammoManager.MaxAmmo);
        }
    }

    // Support 아이템 획득음 로컬 재생
    private void PlayGetItemSound()
    {
        if (SoundNet.Instance != null)
        {
            SoundNet.Instance.PlayLocalAt(GameSoundType.GetItem, transform.position);
            return;
        }

        GameEventSoundPlayer.Instance?.PlayAt(GameSoundType.GetItem, transform.position);
    }

    // 로컬 슈터에게만 Support 아이템 효과음 1회 재생
    private void PlayApplySoundOnce()
    {
        if (applySoundPlayed || !IsLocalShooter() || item == null)
            return;

        applySoundPlayed = true;
        if (item.healAmount > 0f)
        {
            PlayLocalGameSound(GameSoundType.ApplyHealthPack);
            return;
        }

        if (item.ammoAmount > 0)
            PlayLocalGameSound(GameSoundType.ApplyAmmoPack);
    }

    // SoundNet이 없는 단독 실행 경로를 포함한 로컬 게임 사운드 재생
    private void PlayLocalGameSound(GameSoundType soundType)
    {
        if (SoundNet.Instance != null)
        {
            SoundNet.Instance.PlayLocal(soundType);
            return;
        }

        GameEventSoundPlayer.Instance?.Play(soundType);
    }

    // 로컬 슈터 권한 판정
    private bool HasLocalShooterAuthority(GameObject player)
    {
        if (!PhotonNetwork.IsConnected)
            return true;

        PhotonView photonView = player.GetComponent<PhotonView>();
        return photonView == null || photonView.IsMine;
    }

    // 로컬 슈터 소비 가능 여부 판정
    private bool IsLocalShooter()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null && HasLocalShooterAuthority(player);
    }

    // 슈터 탄약 관리자 조회
    private AmmoManager FindShooterAmmoManager()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        return player.GetComponent<AmmoManager>();
    }

    // 슈터 체력 관리자 조회
    private HealthManager FindShooterHealthManager()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        return player.GetComponent<HealthManager>();
    }

    // 픽업 충돌 물리 설정
    private void EnsurePickupPhysics()
    {
        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.isTrigger = true;

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }
}
