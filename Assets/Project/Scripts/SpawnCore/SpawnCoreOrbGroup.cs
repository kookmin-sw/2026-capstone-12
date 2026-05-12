using UnityEngine;

public class SpawnCoreOrbGroup : MonoBehaviour
{
    [Header("Orbs")]
    [SerializeField] private SpawnCoreOrb[] orbs;

    [Header("Sound")]
    [SerializeField] private Transform soundOrigin;

    [Header("Camera Facing")]
    [SerializeField] private bool rotateTowardCamera = true;
    [SerializeField] private Transform orbVisualRoot;
    [SerializeField] private float rotateLerpSpeed = 8f;

    private int currentBrokenCount;
    private BuildingHealthNet buildingHealth;

    private void Awake()
    {
        if (soundOrigin == null)
            soundOrigin = transform;
    }

    private void Start()
    {
        buildingHealth = GetComponent<BuildingHealthNet>();
        if (buildingHealth != null)
        {
            buildingHealth.OnHpChanged += HandleHpChanged;
            UpdateByHp(buildingHealth.CurrentHp, buildingHealth.MaxHp, false);
        }
        else
            SetBrokenCount(0, false);
    }

    private void OnDestroy()
    {
        if (buildingHealth != null)
            buildingHealth.OnHpChanged -= HandleHpChanged;
    }

    private void LateUpdate()
    {
        RotateVisualRootTowardCamera();
    }

    public void UpdateByHp(float currentHp, float maxHp)
    {
        UpdateByHp(currentHp, maxHp, true);
    }

    public void UpdateByHp(float currentHp, float maxHp, bool playEffects)
    {
        if (maxHp <= 0f)
            return;

        float hpRate = Mathf.Clamp01(currentHp / maxHp);
        SetBrokenCount(CalculateBrokenCount(hpRate), playEffects);
    }

    public void SetBrokenCount(int brokenCount, bool playEffects)
    {
        if (orbs == null || orbs.Length == 0)
            return;

        brokenCount = Mathf.Clamp(brokenCount, 0, orbs.Length);

        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] == null)
                continue;

            if (i < brokenCount)
            {
                bool shouldPlayEffect = playEffects && i >= currentBrokenCount;
                orbs[i].SetBroken(shouldPlayEffect);

                if (shouldPlayEffect)
                    PlayOrbBreakSound();
            }
            else
            {
                orbs[i].SetAlive(!playEffects);
            }
        }

        currentBrokenCount = brokenCount;
    }

    private void HandleHpChanged(BuildingHealthNet source, float currentHp, float maxHp)
    {
        if (source != buildingHealth)
            return;

        UpdateByHp(currentHp, maxHp, true);
    }

    private static int CalculateBrokenCount(float hpRate)
    {
        if (hpRate <= 0f)
            return 5;

        if (hpRate <= 0.2f)
            return 4;

        if (hpRate <= 0.4f)
            return 3;

        if (hpRate <= 0.6f)
            return 2;

        if (hpRate <= 0.8f)
            return 1;

        return 0;
    }

    private void PlayOrbBreakSound()
    {
        Vector3 position = soundOrigin != null ? soundOrigin.position : transform.position;

        if (SoundNet.Instance == null)
            return;

        SoundNet.Instance.PlayLocalAt(GameSoundType.SpawnCoreOrbBreak, position);
    }

    private void RotateVisualRootTowardCamera()
    {
        if (!rotateTowardCamera || orbVisualRoot == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 toCamera = cam.transform.position - orbVisualRoot.position;
        toCamera.y = 0f;

        if (toCamera.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        orbVisualRoot.rotation = Quaternion.Slerp(
            orbVisualRoot.rotation,
            targetRotation,
            Time.deltaTime * rotateLerpSpeed
        );
    }
}
