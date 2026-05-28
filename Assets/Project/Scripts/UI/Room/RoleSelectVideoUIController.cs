using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class RoleSelectVideoUIController : MonoBehaviour
{
    // 현재 마우스가 머무는 고정 HitArea에 따라 패널 비율과 영상 상태를 결정
    private enum HoverSide
    {
        None,
        Shooter,
        Supporter
    }

    [System.Serializable]
    // 역할별 패널 표시 요소를 하나의 Inspector 그룹으로 관리
    private class RolePanel
    {
        public RectTransform root; // 패널 폭 애니메이션 대상
        public Image thumbnailImage; // 영상이 없을 때 유지할 기본 이미지
        public RawImage videoRawImage; // RenderTexture 영상 출력 대상
        public Image darkOverlay; // hover 강조/비강조 밝기 조절
        public TMP_Text titleText; // 역할 이름 표시
        public TMP_Text descriptionText1; // 역할 설명 첫 번째 줄/블록
        public TMP_Text descriptionText2; // 역할 설명 두 번째 줄/블록
        public GameObject selectedFrame; // 로컬 플레이어 선택 역할 표시
        public GameObject lockedOverlay; // 상대 플레이어가 선점한 역할 표시
        public VideoPlayer videoPlayer; // hover 중 미리보기 영상 재생
        public VideoClip previewClip = null; // Inspector에서 나중에 할당할 역할 영상
        public bool firstFrameReady; // 기본 화면으로 사용할 첫 프레임 준비 여부
    }

    [Header("References")]
    [SerializeField] private RoomManager roomManager; // 기존 역할/Ready 네트워크 상태 소유자
    [SerializeField] private RolePanel shooterPanel = new RolePanel(); // Shooter 쪽 시각 패널
    [SerializeField] private RolePanel supporterPanel = new RolePanel(); // Supporter 쪽 시각 패널
    [SerializeField] private RectTransform leftHitArea; // Shooter hover/click 고정 감지 영역
    [SerializeField] private RectTransform centerHitArea; // 기본 50:50 복귀 감지 영역
    [SerializeField] private RectTransform rightHitArea; // Supporter hover/click 고정 감지 영역
    [SerializeField] private Button readyButton; // 역할 클릭 제외 판정에 사용할 Ready 버튼
    [SerializeField] private RectTransform leaveButtonArea; // 역할 클릭 처리에서 제외할 나가기 버튼 영역
    [SerializeField] private RectTransform playerListPanel; // 역할 클릭 처리에서 제외할 플레이어 목록 영역

    [Header("Layout")]
    [SerializeField, Range(0.5f, 0.8f)] private float expandedWidthRatio = 0.8f; // hover된 패널의 목표 가로 비율
    [SerializeField] private float layoutLerpSpeed = 10f; // 패널 비율 전환 속도

    [Header("Text Fade")]
    [SerializeField, Range(0f, 1f)] private float idleTextAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float highlightedTextAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float dimmedTextAlpha = 0.07f;

    private const float DefaultPanelWidthRatio = 0.5f;

    private HoverSide hoverSide; // 현재 hover 상태
    private float targetShooterMaxX = DefaultPanelWidthRatio; // Shooter/Supporter 경계의 목표 anchor X

    // 시작 시 Scene 이름 기반 참조와 영상 기본 옵션을 보정
    private void Awake()
    {
        ResolveReferences();
        ConfigureVideoPlayer(shooterPanel);
        ConfigureVideoPlayer(supporterPanel);
        EnsureVideoOutputTexture(shooterPanel);
        EnsureVideoOutputTexture(supporterPanel);
        ShowPreviewStandby(shooterPanel);
        ShowPreviewStandby(supporterPanel);
    }

    // 활성화될 때 RoomManager 상태 이벤트를 구독하고 초기 표시를 맞춤
    private void OnEnable()
    {
        ResolveReferences();

        if (roomManager != null)
            roomManager.RoleStateChanged += RefreshFromRoomState;

        ResetHover();
        RefreshFromRoomState();
    }

    // 비활성화 시 영상과 이벤트 구독을 정리
    private void OnDisable()
    {
        PausePreview(shooterPanel);
        PausePreview(supporterPanel);

        if (roomManager != null)
            roomManager.RoleStateChanged -= RefreshFromRoomState;
    }

    // 고정 HitArea 기준 hover, 레이아웃, 클릭을 매 프레임 반영
    private void Update()
    {
        UpdateHoverByMouse();
        UpdateLayout();
        UpdateClick();
        UpdateVideoCrop(shooterPanel);
        UpdateVideoCrop(supporterPanel);
    }

    // Photon Custom Property 상태를 읽어 Selected/Locked/Ready 강조를 갱신
    public void RefreshFromRoomState()
    {
        if (roomManager == null)
            ResolveReferences();

        string localRole = roomManager != null ? roomManager.LocalRole : null; // 로컬 선택 역할
        bool shooterLocked = roomManager != null && roomManager.IsRoleReadyByOtherPlayer(RoomManager.RoleShooter); // 상대 Shooter Ready 완료 여부
        bool supporterLocked = roomManager != null && roomManager.IsRoleReadyByOtherPlayer(RoomManager.RoleSupporter); // 상대 Supporter Ready 완료 여부

        SetRoleState(shooterPanel, localRole == RoomManager.RoleShooter, shooterLocked);
        SetRoleState(supporterPanel, localRole == RoomManager.RoleSupporter, supporterLocked);
        SetReadyEmphasis(roomManager != null && roomManager.LocalReady);
    }

    // Scene 기존 이름을 우선 사용해 Inspector 수동 연결 부담을 줄임
    private void ResolveReferences()
    {
        if (roomManager == null)
            roomManager = RoomManager.Instance != null ? RoomManager.Instance : FindObjectOfType<RoomManager>();

        ResolvePanel(shooterPanel, "ShooterVisualPanel");
        ResolvePanel(supporterPanel, "SupporterVisualPanel");
        StretchPanelVisuals(shooterPanel);
        StretchPanelVisuals(supporterPanel);
        DisableNonButtonRaycastTargets();

        if (leftHitArea == null)
            leftHitArea = FindChildRect("LeftOverHitArea");
        if (centerHitArea == null)
            centerHitArea = FindChildRect("CenterRestHitArea");
        if (rightHitArea == null)
            rightHitArea = FindChildRect("RightHoverHitArea");
        if (readyButton == null)
            readyButton = FindChildComponent<Button>("ReadyButton");
        if (leaveButtonArea == null)
            leaveButtonArea = FindChildRect("LeaveButton");
        if (playerListPanel == null)
            playerListPanel = FindChildRect("PlayerListPanel");
    }

    // 역할 패널 내부의 표준 자식 이름을 찾아 표시 요소를 연결
    private void ResolvePanel(RolePanel panel, string panelName)
    {
        if (panel.root == null)
            panel.root = FindChildRect(panelName);

        if (panel.root == null)
            return;

        if (panel.thumbnailImage == null)
            panel.thumbnailImage = FindChildComponent<Image>(panel.root, "ThumbnailImage");
        if (panel.videoRawImage == null)
            panel.videoRawImage = FindChildComponent<RawImage>(panel.root, "VideoRawImage");
        if (panel.darkOverlay == null)
            panel.darkOverlay = FindChildComponent<Image>(panel.root, "DarkOverlay");
        if (panel.titleText == null)
            panel.titleText = FindChildComponent<TMP_Text>(panel.root, "TitleText");
        if (panel.descriptionText1 == null)
            panel.descriptionText1 = FindChildComponent<TMP_Text>(panel.root, "DescriptionText_1");
        if (panel.descriptionText2 == null)
            panel.descriptionText2 = FindChildComponent<TMP_Text>(panel.root, "DescriptionText_2");
        if (panel.selectedFrame == null)
        {
            Transform selectedFrame = FindChild(panel.root, "SelectedFrame"); // 로컬 선택 표시 오브젝트
            panel.selectedFrame = selectedFrame != null ? selectedFrame.gameObject : null;
        }
        if (panel.lockedOverlay == null)
        {
            Transform lockedOverlay = FindChild(panel.root, "LockedOverlay"); // 상대 선점 표시 오브젝트
            panel.lockedOverlay = lockedOverlay != null ? lockedOverlay.gameObject : null;
        }
        if (panel.videoPlayer == null)
            panel.videoPlayer = panel.root.GetComponentInChildren<VideoPlayer>(true);
    }

    // 패널은 클리핑 창으로 쓰고, 축소되는 시각 요소는 기본 폭을 유지해 눌려 보이지 않게 함
    private void StretchPanelVisuals(RolePanel panel)
    {
        if (panel.root == null)
            return;

        EnsureClipMask(panel.root);
        StretchToParent(panel.thumbnailImage != null ? panel.thumbnailImage.rectTransform : null);
        StretchToParent(panel.videoRawImage != null ? panel.videoRawImage.rectTransform : null);
        StretchToParent(panel.darkOverlay != null ? panel.darkOverlay.rectTransform : null);

        RectTransform selectedFrameRect = panel.selectedFrame != null ? panel.selectedFrame.transform as RectTransform : null; // 선택 표시 확장 대상
        RectTransform lockedOverlayRect = panel.lockedOverlay != null ? panel.lockedOverlay.transform as RectTransform : null; // 잠금 표시 확장 대상
        StretchToParent(selectedFrameRect);
        StretchToParent(lockedOverlayRect);
    }

    private void EnsureClipMask(RectTransform root)
    {
        if (root.GetComponent<RectMask2D>() == null)
            root.gameObject.AddComponent<RectMask2D>();
    }

    // RectTransform을 부모 영역 전체에 맞춰 고정 크기 UI가 이동처럼 보이지 않게 함
    private void StretchToParent(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    // 영상 패널과 투명 HitArea가 Ready/Leave 버튼 클릭을 가로막지 않게 함
    private void DisableNonButtonRaycastTargets()
    {
        DisablePanelRaycastTargets(shooterPanel);
        DisablePanelRaycastTargets(supporterPanel);
        SetImageRaycastTarget(leftHitArea, false);
        SetImageRaycastTarget(centerHitArea, false);
        SetImageRaycastTarget(rightHitArea, false);
    }

    // 역할 패널의 시각 요소는 입력 대상이 아니므로 Raycast를 끔
    private void DisablePanelRaycastTargets(RolePanel panel)
    {
        if (panel.thumbnailImage != null)
            panel.thumbnailImage.raycastTarget = false;
        if (panel.videoRawImage != null)
            panel.videoRawImage.raycastTarget = false;
        if (panel.darkOverlay != null)
            panel.darkOverlay.raycastTarget = false;
        SetTextRaycastTarget(panel.titleText, false);
        SetTextRaycastTarget(panel.descriptionText1, false);
        SetTextRaycastTarget(panel.descriptionText2, false);
        if (panel.selectedFrame != null)
            SetChildGraphicRaycastTargets(panel.selectedFrame.transform, false);
        if (panel.lockedOverlay != null)
            SetChildGraphicRaycastTargets(panel.lockedOverlay.transform, false);
    }

    // 투명 HitArea Image는 좌표 판정용 RectTransform만 사용하고 UI 클릭은 통과시킴
    private void SetImageRaycastTarget(RectTransform rect, bool raycastTarget)
    {
        Image image = rect != null ? rect.GetComponent<Image>() : null; // HitArea의 투명 Image
        if (image != null)
            image.raycastTarget = raycastTarget;
    }

    private void SetTextRaycastTarget(TMP_Text text, bool raycastTarget)
    {
        if (text != null)
            text.raycastTarget = raycastTarget;
    }

    // Selected/Locked 하위 그래픽이 버튼 입력을 막지 않도록 일괄 설정
    private void SetChildGraphicRaycastTargets(Transform root, bool raycastTarget)
    {
        if (root == null)
            return;

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = raycastTarget;
    }

    // 미리보기 영상이 UI/BGM과 충돌하지 않도록 무음 반복 재생으로 설정
    private void ConfigureVideoPlayer(RolePanel panel)
    {
        if (panel.videoPlayer == null)
            return;

        panel.videoPlayer.playOnAwake = false;
        panel.videoPlayer.waitForFirstFrame = true;
        panel.videoPlayer.isLooping = true;
        panel.videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        panel.videoPlayer.sendFrameReadyEvents = true;

        if (panel.previewClip != null)
            panel.videoPlayer.clip = panel.previewClip;
    }

    // VideoPlayer 출력 RenderTexture를 RawImage에 연결해 영상이 UI에 표시되게 함
    private void EnsureVideoOutputTexture(RolePanel panel)
    {
        if (panel.videoPlayer == null || panel.videoRawImage == null)
            return;

        if (panel.videoRawImage.texture == null && panel.videoPlayer.targetTexture != null)
            panel.videoRawImage.texture = panel.videoPlayer.targetTexture;
    }

    // 기본 상태에서도 영상 첫 프레임이 보이도록 미리 디코딩을 요청
    private void PrepareFirstFrame(RolePanel panel)
    {
        if (panel.videoPlayer == null || panel.videoPlayer.clip == null)
            return;

        if (panel.videoRawImage != null)
            panel.videoRawImage.enabled = true;

        panel.videoPlayer.frameReady -= HandleFirstFrameReady;
        panel.videoPlayer.frameReady += HandleFirstFrameReady;
        panel.videoPlayer.Prepare();
        panel.videoPlayer.Play();
    }

    // 첫 프레임이 렌더링되면 정지 상태로 두어 썸네일처럼 사용
    private void HandleFirstFrameReady(VideoPlayer source, long frameIdx)
    {
        if (frameIdx != 0)
            return;

        RolePanel panel = GetPanelByVideoPlayer(source);
        if (panel == null)
            return;

        panel.firstFrameReady = true;
        source.Pause();
        source.frameReady -= HandleFirstFrameReady;
    }

    // VideoPlayer 콜백에서 원래 패널 상태를 찾음
    private RolePanel GetPanelByVideoPlayer(VideoPlayer source)
    {
        if (shooterPanel.videoPlayer == source)
            return shooterPanel;
        if (supporterPanel.videoPlayer == source)
            return supporterPanel;

        return null;
    }

    // 패널 자체가 아니라 고정 투명 HitArea로 hover 상태를 판정
    private void UpdateHoverByMouse()
    {
        if (!Application.isFocused)
            return;

        Vector2 mousePosition = Input.mousePosition; // 현재 포인터 위치

        if (IsPointerInside(leftHitArea, mousePosition))
        {
            SetHover(HoverSide.Shooter);
            return;
        }

        if (IsPointerInside(rightHitArea, mousePosition))
        {
            SetHover(HoverSide.Supporter);
            return;
        }

        if (IsPointerInside(centerHitArea, mousePosition))
            ResetHover();
    }

    // Ready/Leave/PlayerList 위 클릭은 제외하고 역할 영역 클릭만 전달
    private void UpdateClick()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Vector2 mousePosition = Input.mousePosition; // 현재 클릭 위치

        if (IsPointerOverExcludedClickArea(mousePosition))
            return;

        if (IsPointerInside(leftHitArea, mousePosition))
            roomManager?.RequestSelectShooterFromRolePanel();
        else if (IsPointerInside(rightHitArea, mousePosition))
            roomManager?.RequestSelectSupporterFromRolePanel();
    }

    // hover된 역할만 강조하고 해당 역할 영상만 재생
    private void SetHover(HoverSide side)
    {
        if (hoverSide == side)
            return;

        hoverSide = side;

        if (side == HoverSide.Shooter)
        {
            PlayPreview(shooterPanel);
            ShowPreviewStandby(supporterPanel);
            SetHoverVisual(shooterPanel, highlightedTextAlpha, true);
            SetHoverVisual(supporterPanel, dimmedTextAlpha, false);
        }
        else if (side == HoverSide.Supporter)
        {
            PlayPreview(supporterPanel);
            ShowPreviewStandby(shooterPanel);
            SetHoverVisual(supporterPanel, highlightedTextAlpha, true);
            SetHoverVisual(shooterPanel, dimmedTextAlpha, false);
        }
    }

    // 중앙 영역 진입 시 기본 50:50 레이아웃과 썸네일 상태로 복귀
    private void ResetHover()
    {
        hoverSide = HoverSide.None;
        ShowPreviewStandby(shooterPanel);
        ShowPreviewStandby(supporterPanel);
        SetHoverVisual(shooterPanel, idleTextAlpha, false);
        SetHoverVisual(supporterPanel, idleTextAlpha, false);
    }

    // 현재 hover 상태에 맞춰 두 패널의 경계 anchor를 보간
    private void UpdateLayout()
    {
        if (hoverSide == HoverSide.Shooter)
            targetShooterMaxX = expandedWidthRatio;
        else if (hoverSide == HoverSide.Supporter)
            targetShooterMaxX = 1f - expandedWidthRatio;
        else
            targetShooterMaxX = DefaultPanelWidthRatio;

        ApplyPanelAnchors(Mathf.Lerp(GetCurrentShooterMaxX(), targetShooterMaxX, Time.unscaledDeltaTime * layoutLerpSpeed));
    }

    // 현재 Shooter 패널의 오른쪽 경계 anchor를 읽음
    private float GetCurrentShooterMaxX()
    {
        if (shooterPanel.root == null)
            return DefaultPanelWidthRatio;

        return shooterPanel.root.anchorMax.x;
    }

    // Shooter와 Supporter가 화면 전체를 나눠 갖도록 anchor를 갱신
    private void ApplyPanelAnchors(float shooterMaxX)
    {
        if (shooterPanel.root != null)
        {
            shooterPanel.root.anchorMin = new Vector2(0f, 0f);
            shooterPanel.root.anchorMax = new Vector2(shooterMaxX, 1f);
            shooterPanel.root.offsetMin = Vector2.zero;
            shooterPanel.root.offsetMax = Vector2.zero;
        }

        if (supporterPanel.root != null)
        {
            supporterPanel.root.anchorMin = new Vector2(shooterMaxX, 0f);
            supporterPanel.root.anchorMax = new Vector2(1f, 1f);
            supporterPanel.root.offsetMin = Vector2.zero;
            supporterPanel.root.offsetMax = Vector2.zero;
        }

        ApplyPanelVisualWindow(shooterPanel, shooterMaxX, true);
        ApplyPanelVisualWindow(supporterPanel, 1f - shooterMaxX, false);
    }

    // 50%보다 작아지는 쪽은 이미지 자체 폭을 유지하고 부모 RectMask2D로 보이는 범위만 줄임
    private void ApplyPanelVisualWindow(RolePanel panel, float visibleWidthRatio, bool alignLeft)
    {
        float safeVisibleWidth = Mathf.Max(0.001f, visibleWidthRatio);
        float visualWidthRatio = Mathf.Max(safeVisibleWidth, DefaultPanelWidthRatio);
        float visualWidthInPanel = visualWidthRatio / safeVisibleWidth;

        ApplyVisualRect(panel.thumbnailImage != null ? panel.thumbnailImage.rectTransform : null, visualWidthInPanel, alignLeft);
        ApplyVisualRect(panel.videoRawImage != null ? panel.videoRawImage.rectTransform : null, visualWidthInPanel, alignLeft);
        ApplyVisualRect(panel.darkOverlay != null ? panel.darkOverlay.rectTransform : null, visualWidthInPanel, alignLeft);
    }

    private void ApplyVisualRect(RectTransform rect, float widthInPanel, bool alignLeft)
    {
        if (rect == null)
            return;

        if (alignLeft)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(widthInPanel, 1f);
        }
        else
        {
            rect.anchorMin = new Vector2(1f - widthInPanel, 0f);
            rect.anchorMax = Vector2.one;
        }

        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    // hover 여부에 따라 텍스트와 오버레이 강조도를 조절
    private void SetHoverVisual(RolePanel panel, float textAlpha, bool highlighted)
    {
        if (panel.darkOverlay != null)
            panel.darkOverlay.color = highlighted ? new Color(0f, 0f, 0f, 0.25f) : new Color(0f, 0f, 0f, 0.9490196f);

        SetTextAlpha(panel.titleText, textAlpha);
        SetTextAlpha(panel.descriptionText1, textAlpha);
        SetTextAlpha(panel.descriptionText2, textAlpha);
    }

    private void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
            return;

        text.alpha = alpha;
    }

    // 역할 선택 상태와 상대 선점 상태를 패널 표시에 반영
    private void SetRoleState(RolePanel panel, bool selected, bool locked)
    {
        if (panel.selectedFrame != null)
            panel.selectedFrame.SetActive(selected);
        if (panel.lockedOverlay != null)
            panel.lockedOverlay.SetActive(locked);
    }

    // Clip이 있을 때만 영상 미리보기를 시작해 null 영상 오류를 방지
    // Ready가 실제 완료된 상태에서만 준비 버튼 강조를 유지
    private void SetReadyEmphasis(bool ready)
    {
        if (readyButton == null || readyButton.targetGraphic == null)
            return;

        readyButton.targetGraphic.color = ready ? new Color(0.55f, 1f, 0.72f, 1f) : Color.white;
    }

    private void PlayPreview(RolePanel panel)
    {
        if (panel.videoPlayer == null || panel.videoPlayer.clip == null)
            return;

        if (panel.videoRawImage != null)
            panel.videoRawImage.enabled = true;
        if (panel.thumbnailImage != null)
            panel.thumbnailImage.enabled = false;

        if (!panel.videoPlayer.isPlaying)
            panel.videoPlayer.Play();
    }

    // hover 해제 시 썸네일을 우선 표시하고, 썸네일이 없을 때만 첫 프레임을 대체 화면으로 사용
    private void ShowPreviewStandby(RolePanel panel)
    {
        bool hasThumbnail = panel.thumbnailImage != null && panel.thumbnailImage.sprite != null; // 기본 화면용 썸네일 존재 여부

        if (panel.thumbnailImage != null)
            panel.thumbnailImage.enabled = hasThumbnail;
        if (panel.videoRawImage != null)
            panel.videoRawImage.enabled = !hasThumbnail && panel.videoPlayer != null && panel.videoPlayer.clip != null;

        if (panel.videoPlayer == null || panel.videoPlayer.clip == null)
            return;

        panel.videoPlayer.Pause();
        panel.videoPlayer.frame = 0;

        if (!hasThumbnail && !panel.firstFrameReady)
            PrepareFirstFrame(panel);
    }

    // 오브젝트 비활성화 시 재생 중인 영상만 멈추고 텍스처 연결은 유지
    private void PausePreview(RolePanel panel)
    {
        if (panel.videoPlayer != null)
            panel.videoPlayer.Pause();
    }

    // 패널 크기와 영상 비율이 달라도 찌그러지지 않도록 중앙 크롭 UV를 계산
    private void UpdateVideoCrop(RolePanel panel)
    {
        if (panel.videoRawImage == null || panel.videoPlayer == null || panel.videoPlayer.clip == null)
            return;

        RectTransform videoRect = panel.videoRawImage.rectTransform; // 영상이 표시되는 UI 영역
        float rectAspect = videoRect.rect.width / Mathf.Max(1f, videoRect.rect.height); // UI 영역 비율
        float videoAspect = (float)panel.videoPlayer.clip.width / Mathf.Max(1f, panel.videoPlayer.clip.height); // 영상 원본 비율

        if (videoAspect > rectAspect)
        {
            float visibleWidth = rectAspect / videoAspect; // 좌우를 잘라낼 때 보이는 UV 폭
            panel.videoRawImage.uvRect = new Rect((1f - visibleWidth) * 0.5f, 0f, visibleWidth, 1f);
        }
        else
        {
            float visibleHeight = videoAspect / rectAspect; // 상하를 잘라낼 때 보이는 UV 높이
            panel.videoRawImage.uvRect = new Rect(0f, (1f - visibleHeight) * 0.5f, 1f, visibleHeight);
        }
    }

    // 화면 좌표가 특정 RectTransform 안에 있는지 확인
    private bool IsPointerInside(RectTransform rect, Vector2 screenPosition)
    {
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition);
    }

    // 기존 상호작용 UI 위 클릭이 역할 선택으로 중복 처리되지 않도록 차단
    private bool IsPointerOverExcludedClickArea(Vector2 screenPosition)
    {
        RectTransform readyButtonArea = readyButton != null ? readyButton.transform as RectTransform : null; // Ready 버튼 클릭 제외 영역

        return IsPointerInside(readyButtonArea, screenPosition) ||
               IsPointerInside(leaveButtonArea, screenPosition) ||
               IsPointerInside(playerListPanel, screenPosition);
    }

    // Canvas 하위에서 이름이 일치하는 RectTransform을 찾음
    private RectTransform FindChildRect(string childName)
    {
        return FindChild(transform, childName) as RectTransform;
    }

    // Canvas 하위 이름 기준으로 특정 컴포넌트를 찾음
    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = FindChild(transform, childName); // 검색된 대상 Transform
        return child != null ? child.GetComponent<T>() : null;
    }

    // 특정 패널 하위 이름 기준으로 컴포넌트를 찾음
    private T FindChildComponent<T>(RectTransform parent, string childName) where T : Component
    {
        Transform child = FindChild(parent, childName); // 검색된 대상 Transform
        return child != null ? child.GetComponent<T>() : null;
    }

    // 비활성 자식까지 포함해 이름이 일치하는 첫 Transform을 찾음
    private Transform FindChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }
}
