using UnityEngine;
using UnityEditor;

public class MapSetupTool : EditorWindow
{
    [MenuItem("Tools/Map Setup - Add Colliders and Ground")]
    static void SetupMap()
    {
        // 1. Scene/Static 아래 모든 자식에 MeshCollider 추가
        GameObject sceneObj = GameObject.Find("Scene");
        if (sceneObj == null)
        {
            Debug.LogError("Scene 오브젝트를 찾을 수 없습니다.");
            return;
        }

        Transform staticTr = sceneObj.transform.Find("Static");
        if (staticTr == null)
        {
            Debug.LogError("Scene/Static 오브젝트를 찾을 수 없습니다.");
            return;
        }

        int colliderCount = 0;
        MeshFilter[] meshFilters = staticTr.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.GetComponent<MeshCollider>() == null)
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                colliderCount++;
            }
        }
        Debug.Log($"MeshCollider {colliderCount}개 추가 완료!");

        // 2. 바닥 Plane 생성 (BuildSurface 레이어)
        GameObject existingGround = GameObject.Find("MapGround");
        if (existingGround != null)
        {
            DestroyImmediate(existingGround);
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "MapGround";
        ground.layer = 9; // BuildSurface

        // Scene/Static의 bounds 계산해서 바닥 크기 결정
        Renderer[] renderers = staticTr.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            ground.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.01f, bounds.center.z);
            float scaleX = bounds.size.x / 10f * 1.2f; // Plane 기본 크기 10, 여유 20%
            float scaleZ = bounds.size.z / 10f * 1.2f;
            ground.transform.localScale = new Vector3(scaleX, 1f, scaleZ);
        }

        // 3. 전체 Scene 오브젝트를 Static으로 설정 (라이팅 최적화)
        GameObjectUtility.SetStaticEditorFlags(sceneObj, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        foreach (Transform child in staticTr)
        {
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        Debug.Log($"맵 설정 완료! 바닥 생성 (BuildSurface 레이어), Static 플래그 설정됨");

        // Scene 저장 표시
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Map Setup - Remove Foliage Colliders")]
    static void RemoveFoliageColliders()
    {
        GameObject sceneObj = GameObject.Find("Scene");
        if (sceneObj == null) { Debug.LogError("Scene 오브젝트를 찾을 수 없습니다."); return; }

        Transform staticTr = sceneObj.transform.Find("Static");
        if (staticTr == null) { Debug.LogError("Scene/Static 오브젝트를 찾을 수 없습니다."); return; }

        // 콜라이더 제거할 오브젝트 이름 패턴 (풀, 덤불, 이끼, 나무)
        string[] foliageNames = { "grass", "grasses", "bushes", "bush", "moss", "tree" };

        int removedCount = 0;
        foreach (Transform child in staticTr)
        {
            string lowerName = child.name.ToLower();
            bool isFoliage = false;
            foreach (string pattern in foliageNames)
            {
                if (lowerName.StartsWith(pattern))
                {
                    isFoliage = true;
                    break;
                }
            }

            if (isFoliage)
            {
                MeshCollider mc = child.GetComponent<MeshCollider>();
                if (mc != null)
                {
                    DestroyImmediate(mc);
                    removedCount++;
                }
            }
        }

        Debug.Log($"풀/덤불/이끼/나무에서 MeshCollider {removedCount}개 제거 완료!");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
