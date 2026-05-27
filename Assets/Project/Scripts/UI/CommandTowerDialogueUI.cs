using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// 게임 이벤트에 따라 커맨드 타워 대사를 역할별로 표시한다.
/// - 서포터 캔버스: CommandTowerStatus 옆에 배치
/// - 슈터 캔버스: 오른쪽 패널에 CommandTowerStatus와 함께 배치
/// </summary>
public class CommandTowerDialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text messageText;

    [Header("Thresholds")]
    [SerializeField] private float hpLowThreshold = 0.35f;
    [SerializeField] private int ammoLowTotalThreshold = 60;

    [Header("Cooldowns (seconds)")]
    [SerializeField] private float towerAttackCooldown = 8f;
    [SerializeField] private float hpLowCooldown = 15f;
    [SerializeField] private float ammoLowCooldown = 10f;
    [SerializeField] private float darknessCooldown = 12f;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float holdDuration = 4f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float minDisplayDuration = 1.5f;

    // ── Runtime state ─────────────────────────────────────────────────────
    private RoleType localRole = RoleType.Shooter;
    private bool roleResolved;

    private BuildingHealthNet towerHealthNet;
    private float previousTowerHp = float.MaxValue;
    private bool waitingForTower;
    private bool towerSubscribed;

    private HealthManager shooterHealth;
    private AmmoManager   shooterAmmo;
    private DarknessExposureController darknessCtrl;
    private bool playerSubscribed;

    private float lastTowerAttackTime = -100f;
    private float lastHpLowTime       = -100f;
    private float lastAmmoLowTime     = -100f;
    private float lastDarknessTime    = -100f;

    private Coroutine showCoroutine;
    private string pendingMessage;
    private float messageStartTime;

    // ── Unity lifecycle ───────────────────────────────────────────────────
    private void Start()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        ResolveRole();
        BindCommandTower();
        StartCoroutine(BindPlayerComponents());
    }

    private void OnDestroy()
    {
        if (waitingForTower)
            CommandTower.ActiveTowerChanged -= HandleActiveTowerChanged;

        if (towerSubscribed && towerHealthNet != null)
            towerHealthNet.OnHpChanged -= HandleTowerHpChanged;

        if (playerSubscribed)
            UnsubscribePlayer();
    }

    // ── Role resolution ───────────────────────────────────────────────────
    private void ResolveRole()
    {
        if (TryReadRole(out RoleType role))
        {
            localRole = role;
            roleResolved = true;
        }
        else
        {
            StartCoroutine(WaitForRole());
        }
    }

    private IEnumerator WaitForRole()
    {
        while (true)
        {
            if (TryReadRole(out RoleType role))
            {
                localRole = role;
                roleResolved = true;
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private bool TryReadRole(out RoleType role)
    {
        role = RoleType.Shooter;
        if (PhotonNetwork.LocalPlayer == null) return false;
        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Role", out object val)) return false;
        role = (val as string) == "Supporter" ? RoleType.Supporter : RoleType.Shooter;
        return true;
    }

    // ── CommandTower binding ──────────────────────────────────────────────
    private void BindCommandTower()
    {
        if (CommandTower.ActiveTower != null)
        {
            BuildingHealthNet net = CommandTower.ActiveTower.GetComponent<BuildingHealthNet>();
            if (net != null) { SubscribeToTower(net); return; }
        }

        waitingForTower = true;
        CommandTower.ActiveTowerChanged += HandleActiveTowerChanged;
    }

    private void HandleActiveTowerChanged(CommandTower tower)
    {
        if (tower == null) return;
        BuildingHealthNet net = tower.GetComponent<BuildingHealthNet>();
        if (net == null) return;

        CommandTower.ActiveTowerChanged -= HandleActiveTowerChanged;
        waitingForTower = false;
        SubscribeToTower(net);
    }

    private void SubscribeToTower(BuildingHealthNet net)
    {
        if (towerSubscribed && towerHealthNet != null)
            towerHealthNet.OnHpChanged -= HandleTowerHpChanged;

        towerHealthNet = net;
        previousTowerHp = net.CurrentHp;
        towerHealthNet.OnHpChanged += HandleTowerHpChanged;
        towerSubscribed = true;
    }

    // ── Player component binding ──────────────────────────────────────────
    private IEnumerator BindPlayerComponents()
    {
        while (true)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                HealthManager  health = playerObj.GetComponent<HealthManager>();
                AmmoManager    ammo   = playerObj.GetComponent<AmmoManager>();
                DarknessExposureController dark = playerObj.GetComponent<DarknessExposureController>();

                if (health != null && ammo != null)
                {
                    if (playerSubscribed) UnsubscribePlayer();

                    shooterHealth = health;
                    shooterAmmo   = ammo;
                    darknessCtrl  = dark;

                    shooterHealth.OnHealthChanged.AddListener(HandleShooterHpChanged);
                    shooterAmmo.OnAmmoChanged += HandleAmmoChanged;
                    if (darknessCtrl != null)
                        darknessCtrl.OnExposureChanged += HandleExposureChanged;

                    playerSubscribed = true;
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void UnsubscribePlayer()
    {
        if (shooterHealth != null) shooterHealth.OnHealthChanged.RemoveListener(HandleShooterHpChanged);
        if (shooterAmmo   != null) shooterAmmo.OnAmmoChanged   -= HandleAmmoChanged;
        if (darknessCtrl  != null) darknessCtrl.OnExposureChanged -= HandleExposureChanged;
    }

    // ── Event handlers ────────────────────────────────────────────────────
    private void HandleTowerHpChanged(BuildingHealthNet net, float currentHp, float maxHp)
    {
        if (net != towerHealthNet) return;
        bool tookDamage = currentHp < previousTowerHp;
        previousTowerHp = currentHp;
        if (tookDamage) TriggerTowerUnderAttack();
    }

    private void HandleShooterHpChanged(float ratio)
    {
        if (ratio < hpLowThreshold && ratio > 0f)
            TriggerShooterLowHealth();
    }

    private void HandleAmmoChanged(int current, int reserve)
    {
        if (current + reserve < ammoLowTotalThreshold)
            TriggerShooterNoAmmo();
    }

    private void HandleExposureChanged(float exposure, float max, bool warningVisible)
    {
        if (warningVisible)
            TriggerShooterInDarkness();
    }

    // ── Trigger logic ─────────────────────────────────────────────────────
    private void TriggerTowerUnderAttack()
    {
        if (Time.time - lastTowerAttackTime < towerAttackCooldown) return;
        lastTowerAttackTime = Time.time;

        string msg = localRole == RoleType.Supporter
            ? "커맨드타워가 공격 받고 있습니다.\n타워 주변에 방어 구조물을 설치하거나\n파손된 구조물을 수리해주세요."
            : "커맨드타워가 공격 받고 있습니다.\n적을 막아 타워를 지키세요.";
        ShowMessage(msg);
    }

    private void TriggerShooterLowHealth()
    {
        if (Time.time - lastHpLowTime < hpLowCooldown) return;
        lastHpLowTime = Time.time;

        string msg = localRole == RoleType.Supporter
            ? "슈터의 체력이 낮습니다.\nHealthPack을 제공하세요."
            : "체력이 낮습니다.\n서포터에게 HealthPack을 요청하세요.";
        ShowMessage(msg);
    }

    private void TriggerShooterNoAmmo()
    {
        if (Time.time - lastAmmoLowTime < ammoLowCooldown) return;
        lastAmmoLowTime = Time.time;

        string msg = localRole == RoleType.Supporter
            ? "슈터의 탄약이 부족합니다.\nAmmoPack을 제공하세요."
            : "탄약이 부족합니다.\n서포터에게 AmmoPack을 요청하세요.";
        ShowMessage(msg);
    }

    private void TriggerShooterInDarkness()
    {
        if (Time.time - lastDarknessTime < darknessCooldown) return;
        lastDarknessTime = Time.time;

        string msg = localRole == RoleType.Supporter
            ? "슈터가 어둠에 오염되고 있습니다.\n정화 구조물이나 아이템을 배치하세요."
            : "어둠에 오염되고 있습니다.\n적을 죽여 정화 에너지를 충전하거나\n정화 구역으로 돌아가세요.";
        ShowMessage(msg);
    }

    // ── Display ───────────────────────────────────────────────────────────
    private void ShowMessage(string message)
    {
        if (messageText == null || canvasGroup == null) return;

        // 현재 대사가 minDisplayDuration 미만으로 표시됐으면 큐에 보관
        if (showCoroutine != null && Time.time - messageStartTime < minDisplayDuration)
        {
            pendingMessage = message;
            return;
        }

        pendingMessage = null;
        messageText.text = message;
        messageStartTime = Time.time;

        if (showCoroutine != null) StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        // fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // hold — 대기 중 pending 메시지가 있으면 최소 표시 시간 이후 즉시 교체
        float held = 0f;
        while (held < holdDuration)
        {
            held += Time.deltaTime;

            if (pendingMessage != null && Time.time - messageStartTime >= minDisplayDuration)
            {
                messageText.text = pendingMessage;
                pendingMessage = null;
                messageStartTime = Time.time;
                held = 0f; // hold 타이머 리셋
            }

            yield return null;
        }

        // fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        showCoroutine = null;
    }
}
