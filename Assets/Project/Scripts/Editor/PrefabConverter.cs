using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrefabConverter : EditorWindow
{
    private static readonly (string objName, string savePath)[] TargetObjects =
    {
        ("Player",          "Assets/Resources/Prefabs/Player/ShooterPlayer.prefab"),
        ("Supporter",       "Assets/Resources/Prefabs/Player/SupporterPlayer.prefab"),
        ("Networks",        "Assets/Resources/Prefabs/Managers/Networks.prefab"),
        ("Systems",         "Assets/Resources/Prefabs/Managers/Systems.prefab"),
        ("GameManager",     "Assets/Resources/Prefabs/Managers/GameManager.prefab"),
        ("ResourceManager", "Assets/Resources/Prefabs/Managers/ResourceManager.prefab"),
        ("EnemyManager",    "Assets/Resources/Prefabs/Managers/EnemyManager.prefab"),
        ("RoleManager",     "Assets/Resources/Prefabs/Managers/RoleManager.prefab"),
        ("SpawnCoreManager","Assets/Resources/Prefabs/Managers/SpawnCoreManager.prefab"),
        ("GridManager",     "Assets/Resources/Prefabs/Managers/GridManager.prefab"),
        ("BuildSystem",     "Assets/Resources/Prefabs/Managers/BuildSystem.prefab"),
        ("CombatUIManager", "Assets/Resources/Prefabs/Managers/CombatUIManager.prefab"),
        ("SuppoterCanvas",  "Assets/Resources/Prefabs/UI/SuppoterCanvas.prefab"),
        ("SharedCanvas",    "Assets/Resources/Prefabs/UI/SharedCanvas.prefab"),
        ("VoiceChatCanvas", "Assets/Resources/Prefabs/UI/VoiceChatCanvas.prefab"),
    };

    private readonly Dictionary<string, bool> _selected = new();
    private Vector2 _scroll;
    private string _statusMsg = "";

    [MenuItem("KMU Tools/Prefab Converter")]
    public static void Open()
    {
        GetWindow<PrefabConverter>("Prefab Converter").Show();
    }

    private void OnEnable()
    {
        foreach (var (name, _) in TargetObjects)
            _selected[name] = false;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("씬 오브젝트 → 프리팹 변환", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "체크한 오브젝트를 프리팹으로 변환합니다.\n" +
            "씬 오브젝트는 프리팹 인스턴스로 자동 연결됩니다.\n" +
            "이미 프리팹인 오브젝트는 건너뜁니다.",
            MessageType.Info);

        EditorGUILayout.Space(4);

        if (GUILayout.Button("전체 선택"))
            SetAll(true);
        if (GUILayout.Button("전체 해제"))
            SetAll(false);

        EditorGUILayout.Space(4);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        string lastCategory = "";
        foreach (var (name, path) in TargetObjects)
        {
            string category = GetCategory(path);
            if (category != lastCategory)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField(category, EditorStyles.miniBoldLabel);
                lastCategory = category;
            }

            bool inScene = FindInScene(name) != null;
            EditorGUI.BeginDisabledGroup(!inScene);
            _selected[name] = EditorGUILayout.ToggleLeft(
                inScene ? name : $"{name}  (씬에 없음)",
                _selected[name]);
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(4);

        EditorGUI.BeginDisabledGroup(!AnySelected());
        if (GUILayout.Button("선택한 오브젝트 프리팹 변환", GUILayout.Height(30)))
            Convert();
        EditorGUI.EndDisabledGroup();

        if (!string.IsNullOrEmpty(_statusMsg))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_statusMsg, MessageType.None);
        }
    }

    private void Convert()
    {
        int ok = 0, skip = 0;
        var log = new System.Text.StringBuilder();

        foreach (var (name, savePath) in TargetObjects)
        {
            if (!_selected.TryGetValue(name, out bool sel) || !sel) continue;

            GameObject obj = FindInScene(name);
            if (obj == null)
            {
                log.AppendLine($"[건너뜀] {name}: 씬에서 찾을 수 없음");
                skip++;
                continue;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(obj))
            {
                log.AppendLine($"[건너뜀] {name}: 이미 프리팹 인스턴스");
                skip++;
                continue;
            }

            EnsureDirectory(savePath);

            if (File.Exists(savePath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "프리팹 이미 존재",
                    $"{savePath}\n이미 존재합니다. 덮어쓰시겠습니까?",
                    "덮어쓰기", "건너뜀");

                if (!overwrite)
                {
                    log.AppendLine($"[건너뜀] {name}: 덮어쓰기 거부");
                    skip++;
                    continue;
                }
            }

            bool success;
            PrefabUtility.SaveAsPrefabAssetAndConnect(obj, savePath, InteractionMode.UserAction, out success);

            if (success)
            {
                log.AppendLine($"[완료] {name} → {savePath}");
                ok++;
            }
            else
            {
                log.AppendLine($"[실패] {name}: 저장 실패");
                skip++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _statusMsg = $"완료 {ok}개 / 건너뜀 {skip}개\n{log}";
        Debug.Log("[PrefabConverter]\n" + log);
    }

    private static GameObject FindInScene(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
                Transform child = root.transform.Find(name);
                if (child != null) return child.gameObject;
            }
        }
        return null;
    }

    private static void EnsureDirectory(string assetPath)
    {
        string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(dir)) return;
        if (!AssetDatabase.IsValidFolder(dir))
            Directory.CreateDirectory(dir);
    }

    private static string GetCategory(string path)
    {
        if (path.Contains("/Player/")) return "── 플레이어";
        if (path.Contains("/Managers/")) return "── 매니저";
        if (path.Contains("/UI/")) return "── UI";
        return "── 기타";
    }

    private void SetAll(bool val)
    {
        foreach (var (name, _) in TargetObjects)
        {
            if (FindInScene(name) != null)
                _selected[name] = val;
        }
    }

    private bool AnySelected()
    {
        foreach (var v in _selected.Values)
            if (v) return true;
        return false;
    }
}
