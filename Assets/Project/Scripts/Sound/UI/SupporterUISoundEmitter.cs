using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SupporterUISoundEmitter : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private bool playHover = true;
    [SerializeField] private bool playClick = true;
    [SerializeField] private SupporterUISoundType clickSoundType = SupporterUISoundType.Click;
    [SerializeField] private bool ignoreIfNotInteractable = true;
    [SerializeField] private Selectable selectable;
    [FormerlySerializedAs("manager")]
    [SerializeField] private SupporterUISoundPlayer player;

    private void Awake()
    {
        if (selectable == null)
            selectable = GetComponent<Selectable>();

        if (player == null)
            player = GetComponentInParent<SupporterUISoundPlayer>(true);
    }

    // 포인터 진입 시 hover 사운드 재생
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHover)
            return;

        GetPlayer()?.Play(SupporterUISoundType.Hover);
    }

    // 활성 클릭 가능 상태의 click 사운드 재생
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!playClick || eventData.button != PointerEventData.InputButton.Left || !CanPlayClick())
            return;

        GetPlayer()?.Play(clickSoundType);
    }

    // 버튼별 click 사운드 타입 설정
    public void ConfigureClickSound(SupporterUISoundType soundType)
    {
        clickSoundType = soundType;
    }

    // click 사운드 재생 가능 여부 확인
    private bool CanPlayClick()
    {
        return !ignoreIfNotInteractable || selectable == null || selectable.interactable;
    }

    // 계층 또는 전역 Supporter 사운드 매니저 조회
    private SupporterUISoundPlayer GetPlayer()
    {
        if (player != null)
            return player;

        player = SupporterUISoundPlayer.Instance;
        if (player != null)
            return player;

        return GetComponentInParent<SupporterUISoundPlayer>(true);
    }
}
