using System.Collections.Generic;
using UnityEngine;

public class PurificationZoneRegistry : MonoBehaviour
{
    private static PurificationZoneRegistry instance;

    // 현재 씬에서 활성화된 정화 광원 목록
    private readonly List<PurificationLightSource> sources = new List<PurificationLightSource>();

    public static PurificationZoneRegistry Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<PurificationZoneRegistry>();

            return instance;
        }
    }

    // 외부 시스템용 활성 정화 광원 읽기 전용 목록
    public IReadOnlyList<PurificationLightSource> Sources => sources;

    // 정화 광원 등록 진입점
    public static void Register(PurificationLightSource source)
    {
        if (source == null)
            return;

        PurificationZoneRegistry registry = Instance;
        if (registry == null)
        {
            Debug.LogError($"{nameof(PurificationZoneRegistry)}: Scene registry is missing.", source);
            return;
        }

        registry.RegisterInternal(source);
    }

    // 정화 광원 등록 해제 진입점
    public static void Unregister(PurificationLightSource source)
    {
        if (source == null || instance == null)
            return;

        instance.sources.Remove(source);
    }

    // 지정 월드 좌표의 정화 영역 포함 여부 확인
    public bool IsPositionPurified(Vector3 worldPosition)
    {
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            PurificationLightSource source = sources[i];
            if (source == null)
            {
                sources.RemoveAt(i);
                continue;
            }

            if (source.preventsDarknessExposure && source.Contains(worldPosition))
                return true;
        }

        return false;
    }

    // 지정 월드 좌표에 적용되는 최대 정화 강도 반환
    public float GetPurificationStrength(Vector3 worldPosition)
    {
        float highestStrength = 0f;

        for (int i = sources.Count - 1; i >= 0; i--)
        {
            PurificationLightSource source = sources[i];
            if (source == null)
            {
                sources.RemoveAt(i);
                continue;
            }

            if (source.Contains(worldPosition))
                highestStrength = Mathf.Max(highestStrength, source.strength);
        }

        return highestStrength;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void RegisterInternal(PurificationLightSource source)
    {
        // 동일 정화 광원 중복 등록 방지
        if (!sources.Contains(source))
            sources.Add(source);
    }
}
