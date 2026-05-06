using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBgmPlayer : MonoBehaviour
{
    private static MainMenuBgmPlayer instance;

    [SerializeField] private AudioClip bgmClip;
    [SerializeField] [Range(0f, 1f)] private float bgmVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Play();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    // 메뉴 계열 씬에서만 BGM 재생 상태 갱신
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu" || scene.name == "Lobby" || scene.name == "RoomScene")
        {
            Play();
            return;
        }

        Stop();
    }

    // 메뉴 BGM 반복 재생
    private void Play()
    {
        if (bgmClip == null)
            return;

        EnsureAudioSource();

        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = bgmVolume;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    // BGM 재생용 AudioSource 보장
    private void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    // 게임 씬 진입 중 BGM 정지
    private void Stop()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }
}
