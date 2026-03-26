using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 보이스 챗 자동 셋업 에디터 도구
/// 메뉴: Tools → Voice Chat Setup
/// </summary>
public class VoiceChatSetup : Editor
{
    // ============================================================
    // 1) MainMenu 씬에 VoiceChatManager 생성
    // ============================================================
    [MenuItem("Tools/Voice Chat Setup/1. MainMenu - VoiceChatManager 생성")]
    static void SetupVoiceChatManager()
    {
        VoiceChatManager existing = FindObjectOfType<VoiceChatManager>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Voice Chat Setup",
                "VoiceChatManager가 이미 씬에 존재합니다!\n오브젝트: " + existing.gameObject.name,
                "확인");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject voiceManager = new GameObject("VoiceChatManager");
        voiceManager.AddComponent<VoiceChatManager>();

        Selection.activeGameObject = voiceManager;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Voice Chat Setup",
            "VoiceChatManager 생성 완료!\n\n" +
            "- PunVoiceClient (자동 추가)\n" +
            "- Recorder (자동 추가)\n\n" +
            "씬을 저장해주세요 (Ctrl+S)",
            "확인");
    }

    // ============================================================
    // 2) MultiPlayScene에 VoiceChatUI 생성 (자체 Canvas 포함)
    // ============================================================
    [MenuItem("Tools/Voice Chat Setup/2. MultiPlayScene - VoiceChatUI 생성")]
    static void SetupVoiceChatUI()
    {
        VoiceChatUI existing = FindObjectOfType<VoiceChatUI>(true);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Voice Chat Setup",
                "VoiceChatUI가 이미 씬에 존재합니다!\n오브젝트: " + existing.gameObject.name +
                "\n\n기존 것을 삭제 후 다시 시도해주세요.",
                "확인");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // ── 자체 Canvas 생성 (역할 무관, 항상 표시) ──
        GameObject canvasObj = new GameObject("VoiceChatCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 다른 UI 위에 표시
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // ── VoiceChatUI 패널 ──
        GameObject voicePanel = new GameObject("VoiceChatUI");
        voicePanel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = voicePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(20f, 20f);
        panelRect.sizeDelta = new Vector2(200f, 100f);

        // ── 마이크 버튼 ──
        GameObject btnObj = new GameObject("MuteButton");
        btnObj.transform.SetParent(voicePanel.transform, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 0.5f);
        btnRect.anchorMax = new Vector2(0f, 0.5f);
        btnRect.pivot = new Vector2(0f, 0.5f);
        btnRect.anchoredPosition = new Vector2(10f, 15f);
        btnRect.sizeDelta = new Vector2(50f, 50f);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.8f, 0.2f, 0.9f);
        Button muteBtn = btnObj.AddComponent<Button>();

        // 버튼 텍스트
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = btnTextObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        Text btnText = btnTextObj.AddComponent<Text>();
        btnText.text = "MIC";
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.fontSize = 14;
        btnText.fontStyle = FontStyle.Bold;
        btnText.color = Color.white;
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ── Speaking 인디케이터 ──
        GameObject speakingObj = new GameObject("SpeakingIndicator");
        speakingObj.transform.SetParent(voicePanel.transform, false);

        RectTransform speakRect = speakingObj.AddComponent<RectTransform>();
        speakRect.anchorMin = new Vector2(0f, 0.5f);
        speakRect.anchorMax = new Vector2(0f, 0.5f);
        speakRect.pivot = new Vector2(0f, 0.5f);
        speakRect.anchoredPosition = new Vector2(70f, 15f);
        speakRect.sizeDelta = new Vector2(120f, 25f);

        Image speakBg = speakingObj.AddComponent<Image>();
        speakBg.color = new Color(0f, 0f, 0f, 0.5f);

        GameObject speakTextObj = new GameObject("Text");
        speakTextObj.transform.SetParent(speakingObj.transform, false);
        RectTransform spkTxtRect = speakTextObj.AddComponent<RectTransform>();
        spkTxtRect.anchorMin = Vector2.zero;
        spkTxtRect.anchorMax = Vector2.one;
        spkTxtRect.sizeDelta = Vector2.zero;
        Text speakText = speakTextObj.AddComponent<Text>();
        speakText.text = "Speaking...";
        speakText.alignment = TextAnchor.MiddleCenter;
        speakText.fontSize = 13;
        speakText.color = Color.yellow;
        speakText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        speakingObj.SetActive(false);

        // ── 볼륨 슬라이더 ──
        GameObject sliderObj = CreateVoiceSlider(voicePanel.transform);

        // ── VoiceChatUI 컴포넌트 추가 및 참조 연결 ──
        VoiceChatUI voiceUI = voicePanel.AddComponent<VoiceChatUI>();

        SerializedObject so = new SerializedObject(voiceUI);
        so.FindProperty("muteButton").objectReferenceValue = muteBtn;
        so.FindProperty("muteIcon").objectReferenceValue = btnImage;
        so.FindProperty("speakingIndicator").objectReferenceValue = speakingObj;
        so.FindProperty("voiceVolumeSlider").objectReferenceValue = sliderObj.GetComponent<Slider>();
        so.ApplyModifiedProperties();

        Selection.activeGameObject = canvasObj;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Voice Chat Setup",
            "VoiceChatUI 생성 완료!\n\n" +
            "- 자체 Canvas (역할 무관, 양쪽 모두 표시)\n" +
            "- MuteButton (마이크 ON/OFF)\n" +
            "- SpeakingIndicator (말하는 중 표시)\n" +
            "- VoiceVolumeSlider (상대 음성 볼륨)\n\n" +
            "위치: 화면 좌측 하단\n" +
            "씬을 저장해주세요 (Ctrl+S)",
            "확인");
    }

    // ============================================================
    // 볼륨 슬라이더 생성 헬퍼
    // ============================================================
    static GameObject CreateVoiceSlider(Transform parent)
    {
        GameObject sliderObj = new GameObject("VoiceVolumeSlider");
        sliderObj.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(0f, 0f);
        sliderRect.pivot = new Vector2(0f, 0f);
        sliderRect.anchoredPosition = new Vector2(10f, -15f);
        sliderRect.sizeDelta = new Vector2(180f, 20f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.7f, 1f, 0.9f);

        // Handle Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-20f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20f, 0f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        // 라벨
        GameObject label = new GameObject("Label");
        label.transform.SetParent(sliderObj.transform, false);
        RectTransform labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 2f);
        labelRect.sizeDelta = new Vector2(0f, 15f);
        Text labelText = label.AddComponent<Text>();
        labelText.text = "Voice Volume";
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.fontSize = 11;
        labelText.color = Color.white;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return sliderObj;
    }
}
