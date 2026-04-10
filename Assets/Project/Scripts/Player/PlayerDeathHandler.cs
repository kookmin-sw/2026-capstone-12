using Photon.Pun;
using UnityEngine;

public class PlayerDeathHandler : MonoBehaviourPun
{
    [SerializeField] private HealthManager healthManager;
    [SerializeField] private Behaviour[] disableOnDeath; // 죽을때 비활성화 할 컴포넌트: PlayerController, WeaponManager 등

    private void Awake()
    {
        if (healthManager == null) healthManager = GetComponent<HealthManager>();
    }

    private void OnEnable()
    {
        if (healthManager != null) healthManager.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (healthManager != null) healthManager.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        // 로컬 플레이어만 입력/카메라 끄기
        if (photonView.IsMine)
        {
            for (int i = 0; i < disableOnDeath.Length; i++)
                if (disableOnDeath[i] != null) disableOnDeath[i].enabled = false;
        }

        Debug.Log("Player Died! Triggering Game Over...");
        GameManager.Instance.TriggerGameOver();
        InputLock.Lock();
    }
}