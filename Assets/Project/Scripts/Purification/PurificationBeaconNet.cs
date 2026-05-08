using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PurificationLightSource))]
public class PurificationBeaconNet : MonoBehaviour
{
    [Header("Beacon")]
    [SerializeField] private float radius = 12f; // Beacon 정화 반경
    [SerializeField] private float duration = 10f; // Beacon 유지 시간
    [SerializeField] private Light beaconLight; // Beacon 시각화용 조명

    private PurificationLightSource lightSource;
    private float expireTime;

    private void Awake()
    {
        lightSource = GetComponent<PurificationLightSource>();
        EnsureLight();
    }

    private void OnEnable()
    {
        expireTime = Time.time + duration;
        ApplyBeaconState();
    }

    private void Update()
    {
        if (duration <= 0f || Time.time < expireTime)
            return;

        DestroyBeacon();
    }

    public void Configure(float beaconRadius, float beaconDuration)
    {
        radius = Mathf.Max(0f, beaconRadius);
        duration = Mathf.Max(0.1f, beaconDuration);
        expireTime = Time.time + duration;
        ApplyBeaconState();
    }

    private void ApplyBeaconState()
    {
        if (lightSource == null)
            lightSource = GetComponent<PurificationLightSource>();

        if (lightSource != null)
        {
            lightSource.sourceType = PurificationLightSourceType.Beacon;
            lightSource.radius = radius;
            lightSource.strength = 1f;
            lightSource.preventsDarknessExposure = radius > 0f;
            lightSource.isPermanent = false;
        }

        if (beaconLight != null)
        {
            beaconLight.range = radius;
            beaconLight.intensity = 2.2f;
            beaconLight.enabled = radius > 0f;
        }
    }

    private void EnsureLight()
    {
        if (beaconLight != null)
            return;

        GameObject lightObject = new GameObject("PurificationBeaconLight");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = Vector3.up * 1.2f;

        beaconLight = lightObject.AddComponent<Light>();
        beaconLight.type = LightType.Point;
        beaconLight.color = new Color(1f, 0.93f, 0.6f, 1f);
        beaconLight.shadows = LightShadows.None;
    }

    private void DestroyBeacon()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            PhotonView photonView = GetComponent<PhotonView>();
            if (photonView != null)
            {
                PhotonNetwork.Destroy(gameObject);
                return;
            }
        }

        Destroy(gameObject);
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0f, radius);
        duration = Mathf.Max(0.1f, duration);
    }
}
