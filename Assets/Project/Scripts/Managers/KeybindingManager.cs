using System.Collections.Generic;
using UnityEngine;

public enum KeyAction
{
    Jump,
    Sprint,
    Reload,
    Ping
}

public class KeybindingManager : MonoBehaviour
{
    public static KeybindingManager Instance { get; private set; }

    private static readonly Dictionary<KeyAction, KeyCode> Defaults = new Dictionary<KeyAction, KeyCode>
    {
        { KeyAction.Jump,   KeyCode.Space },
        { KeyAction.Sprint, KeyCode.LeftShift },
        { KeyAction.Reload, KeyCode.R },
        { KeyAction.Ping,   KeyCode.G }
    };

    private Dictionary<KeyAction, KeyCode> bindings = new Dictionary<KeyAction, KeyCode>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    // ============================================================
    // 정적 입력 헬퍼 (Instance 없을 때 기본값으로 폴백)
    // ============================================================
    public static bool GetKeyStatic(KeyAction action)
    {
        if (Instance != null) return Instance.GetKey(action);
        return Input.GetKey(Defaults[action]);
    }

    public static bool GetKeyDownStatic(KeyAction action)
    {
        if (Instance != null) return Instance.GetKeyDown(action);
        return Input.GetKeyDown(Defaults[action]);
    }

    public static bool GetKeyUpStatic(KeyAction action)
    {
        if (Instance != null) return Instance.GetKeyUp(action);
        return Input.GetKeyUp(Defaults[action]);
    }

    // ============================================================
    // 인스턴스 입력 메서드
    // ============================================================
    public KeyCode GetKeyCode(KeyAction action) => bindings[action];
    public bool GetKey(KeyAction action)     => Input.GetKey(bindings[action]);
    public bool GetKeyDown(KeyAction action) => Input.GetKeyDown(bindings[action]);
    public bool GetKeyUp(KeyAction action)   => Input.GetKeyUp(bindings[action]);

    // ============================================================
    // 바인딩 수정
    // ============================================================
    public void SetBinding(KeyAction action, KeyCode key) => bindings[action] = key;

    public bool HasConflict(KeyAction action, KeyCode newKey)
    {
        foreach (var kvp in bindings)
            if (kvp.Key != action && kvp.Value == newKey) return true;
        return false;
    }

    public void ResetToDefaults()
    {
        foreach (var kvp in Defaults)
            bindings[kvp.Key] = kvp.Value;
    }

    // ============================================================
    // 저장 / 로드
    // ============================================================
    public void Save()
    {
        foreach (var kvp in bindings)
            PlayerPrefs.SetInt("Keybind_" + kvp.Key, (int)kvp.Value);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        foreach (var kvp in Defaults)
            bindings[kvp.Key] = (KeyCode)PlayerPrefs.GetInt("Keybind_" + kvp.Key, (int)kvp.Value);
    }
}
