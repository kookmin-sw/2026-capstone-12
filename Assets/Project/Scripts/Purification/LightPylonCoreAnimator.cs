using UnityEngine;

public class LightPylonCoreAnimator : MonoBehaviour
{
    [SerializeField] private Transform targetCore; // 애니메이션 대상
    [SerializeField] private float bobAmplitude = 0.12f; // 부유 높이
    [SerializeField] private float bobFrequency = 0.3f; // 부유 빈도
    [SerializeField] private float rotationSpeed = 45f; // 회전 속도

    private Vector3 initialLocalPosition; // 초기 로컬 위치
    private Quaternion initialLocalRotation; // 초기 로컬 회전
    private float elapsedTime; // 경과 시간

    private void Awake()
    {
        if (targetCore == null)
            targetCore = transform;

        initialLocalPosition = targetCore.localPosition;
        initialLocalRotation = targetCore.localRotation;
    }

    private void OnEnable()
    {
        elapsedTime = 0f;
        if (targetCore == null)
            return;

        targetCore.localPosition = initialLocalPosition;
        targetCore.localRotation = initialLocalRotation;
    }

    private void Update()
    {
        if (targetCore == null)
            return;

        elapsedTime += Time.deltaTime;
        float bobOffset = Mathf.Sin(elapsedTime * bobFrequency * Mathf.PI * 2f) * bobAmplitude; // 부유 오프셋
        float rotationAngle = elapsedTime * rotationSpeed; // 회전 각도

        targetCore.localPosition = initialLocalPosition + Vector3.up * bobOffset;
        targetCore.localRotation = Quaternion.Euler(0f, rotationAngle, 0f) * initialLocalRotation;
    }

    private void OnValidate()
    {
        bobAmplitude = Mathf.Max(0f, bobAmplitude);
        bobFrequency = Mathf.Max(0f, bobFrequency);
    }
}
