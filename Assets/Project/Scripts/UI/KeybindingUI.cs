using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 키 리바인딩 UI
/// - 각 KeyAction별 버튼 클릭 → 다음 키 입력으로 바인딩 변경
/// - 충돌 감지, 기본값 초기화, Apply/Cancel 지원
/// </summary>
public class KeybindingUI : MonoBehaviour
{
    [System.Serializable]
    public class KeybindRow
    {
        public KeyAction action;
        public Button button;
        public Text keyLabel;
    }

    [Header("Keybind Rows")]
    [SerializeField] private KeybindRow[] rows;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button resetButton;

    [Header("Conflict Warning")]
    [SerializeField] private GameObject conflictWarning;
    [SerializeField] private Text conflictText;

    private KeyAction? listeningAction = null;
    private Text listeningLabel = null;
    private string originalLabelText = null;

    private Dictionary<KeyAction, KeyCode> tempBindings = new Dictionary<KeyAction, KeyCode>();

    // ============================================================
    // Unity 생명주기
    // ============================================================
    void Start()
    {
        foreach (var row in rows)
        {
            var captured = row;
            captured.button.onClick.AddListener(() => StartListening(captured));
        }

        applyButton.onClick.AddListener(OnApply);
        cancelButton.onClick.AddListener(OnCancel);
        if (resetButton != null) resetButton.onClick.AddListener(OnReset);
    }

    void OnEnable()
    {
        LoadTempFromManager();
        RefreshAllLabels();
        if (conflictWarning != null) conflictWarning.SetActive(false);
        StopListening();
    }

    void OnDisable()
    {
        StopListening();
    }

    // ============================================================
    // 리바인딩 흐름
    // ============================================================
    private void StartListening(KeybindRow row)
    {
        // 이전 리스닝 취소
        StopListening();

        listeningAction = row.action;
        listeningLabel = row.keyLabel;
        originalLabelText = row.keyLabel.text;
        row.keyLabel.text = "...";
        if (conflictWarning != null) conflictWarning.SetActive(false);
    }

    private void StopListening()
    {
        if (listeningLabel != null && originalLabelText != null)
            listeningLabel.text = originalLabelText;
        listeningAction = null;
        listeningLabel = null;
        originalLabelText = null;
    }

    void OnGUI()
    {
        if (listeningAction == null) return;

        Event e = Event.current;
        if (e.type != EventType.KeyDown) return;
        if (e.keyCode == KeyCode.None) return;

        if (e.keyCode == KeyCode.Escape)
        {
            StopListening();
            return;
        }

        KeyCode newKey = e.keyCode;
        KeyAction action = listeningAction.Value;

        // 충돌 검사
        bool conflict = false;
        foreach (var kvp in tempBindings)
        {
            if (kvp.Key != action && kvp.Value == newKey)
            {
                conflict = true;
                break;
            }
        }

        if (conflict)
        {
            if (conflictWarning != null)
            {
                conflictWarning.SetActive(true);
                if (conflictText != null)
                    conflictText.text = $"'{newKey}' 키는 이미 다른 액션에 사용 중입니다.";
            }
            StopListening();
            return;
        }

        // 바인딩 갱신
        tempBindings[action] = newKey;
        if (listeningLabel != null) listeningLabel.text = newKey.ToString();
        if (conflictWarning != null) conflictWarning.SetActive(false);

        // 리스닝 상태만 클리어 (레이블은 이미 갱신됨)
        listeningAction = null;
        listeningLabel = null;
        originalLabelText = null;
    }

    // ============================================================
    // 버튼 이벤트
    // ============================================================
    private void OnApply()
    {
        if (KeybindingManager.Instance == null) return;

        foreach (var kvp in tempBindings)
            KeybindingManager.Instance.SetBinding(kvp.Key, kvp.Value);

        KeybindingManager.Instance.Save();
        gameObject.SetActive(false);
    }

    private void OnCancel()
    {
        gameObject.SetActive(false);
    }

    private void OnReset()
    {
        if (KeybindingManager.Instance == null) return;
        KeybindingManager.Instance.ResetToDefaults();
        LoadTempFromManager();
        RefreshAllLabels();
        if (conflictWarning != null) conflictWarning.SetActive(false);
        StopListening();
    }

    // ============================================================
    // 내부 유틸
    // ============================================================
    private void LoadTempFromManager()
    {
        if (KeybindingManager.Instance == null) return;
        tempBindings.Clear();
        foreach (KeyAction action in System.Enum.GetValues(typeof(KeyAction)))
            tempBindings[action] = KeybindingManager.Instance.GetKeyCode(action);
    }

    private void RefreshAllLabels()
    {
        foreach (var row in rows)
        {
            if (tempBindings.TryGetValue(row.action, out KeyCode key))
                row.keyLabel.text = key.ToString();
        }
    }
}
