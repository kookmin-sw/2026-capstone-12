using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PurificationLightSource))]
public class PermanentPurifiedZoneNet : MonoBehaviour
{
    private const string PrefabResourcePath = "Prefabs/Purification/PermanentPurifiedZone";

    // Core 파괴 RPC가 중복 적용되어도 같은 Core의 영구 정화 구역은 하나만 유지
    private static readonly Dictionary<int, PermanentPurifiedZoneNet> zonesByCoreId = new();

    [Header("Permanent Purified Zone")]
    [SerializeField] private int coreId;
    [SerializeField] private float radius = 26f;
    [SerializeField] private Light zoneLight;

    private PurificationLightSource lightSource;

    // SpawnCore 파괴 알림은 모든 클라이언트에 도착하므로 각 클라이언트가 같은 프리팹을 로컬 생성
    public static void SpawnOrUpdate(int sourceCoreId, Vector3 position)
    {
        if (zonesByCoreId.TryGetValue(sourceCoreId, out PermanentPurifiedZoneNet existing) && existing != null)
        {
            existing.transform.position = position;
            existing.Configure(sourceCoreId);
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"{nameof(PermanentPurifiedZoneNet)} prefab missing at Resources/{PrefabResourcePath}.");
            return;
        }

        GameObject zoneObject = Instantiate(prefab, position, Quaternion.identity);
        zoneObject.name = $"PermanentPurifiedZone_{sourceCoreId}";

        PermanentPurifiedZoneNet zone = zoneObject.GetComponent<PermanentPurifiedZoneNet>();
        if (zone == null)
        {
            Debug.LogError($"{nameof(PermanentPurifiedZoneNet)} prefab does not contain {nameof(PermanentPurifiedZoneNet)}.");
            Destroy(zoneObject);
            return;
        }

        zone.Configure(sourceCoreId);
    }

    private void Awake()
    {
        lightSource = GetComponent<PurificationLightSource>();
        ApplyZoneState();
    }

    private void OnDestroy()
    {
        if (zonesByCoreId.TryGetValue(coreId, out PermanentPurifiedZoneNet zone) && zone == this)
            zonesByCoreId.Remove(coreId);
    }

    public void Configure(int sourceCoreId)
    {
        coreId = sourceCoreId;
        radius = Mathf.Max(0f, radius);
        zonesByCoreId[coreId] = this;
        ApplyZoneState();
    }

    private void ApplyZoneState()
    {
        if (lightSource == null)
            lightSource = GetComponent<PurificationLightSource>();

        if (lightSource != null)
        {
            lightSource.sourceType = PurificationLightSourceType.PermanentPurifiedZone;
            lightSource.radius = radius;
            lightSource.strength = 1f;
            lightSource.preventsDarknessExposure = radius > 0f;
            lightSource.isPermanent = true;
        }

        if (zoneLight != null)
        {
            zoneLight.range = radius;
            zoneLight.enabled = radius > 0f;
        }
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0f, radius);
    }
}
