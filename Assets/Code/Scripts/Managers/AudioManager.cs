using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Quản lý nhạc theo màn chơi, âm thanh bắn và việc tạm dừng âm thanh.</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip level1Music;
    [SerializeField] private AudioClip level2Music;
    [SerializeField] private AudioClip level3Music;
    [SerializeField] private AudioClip level4Music;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip shootSound;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)]
    private float musicVolume = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float sfxVolume = 0.8f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private bool gameplayPaused;
    private bool resumeMusic;
    private bool resumeSfx;

    public bool IsGameplayPaused => gameplayPaused;

    /// <summary>Giữ một bộ quản lý âm thanh xuyên scene và tạo nguồn phát riêng cho nhạc, hiệu ứng.</summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
    }

    /// <summary>Đăng ký sự kiện tải scene và phát nhạc phù hợp với scene hiện tại.</summary>
    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        PlayMusicForScene(
            SceneManager.GetActiveScene().name
        );
    }

    /// <summary>Hủy đăng ký sự kiện tải scene khi bộ quản lý bị hủy.</summary>
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>Bỏ trạng thái tạm dừng âm thanh và chọn nhạc cho scene vừa tải.</summary>
    /// <param name="scene">Scene vừa được tải.</param>
    /// <param name="mode">Chế độ tải scene do Unity truyền vào; hàm không sử dụng.</param>
    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        SetGameplayPaused(false);
        PlayMusicForScene(scene.name);
    }

    /// <summary>Chọn nhạc theo tên scene và chỉ phát lại nếu bản nhạc đó chưa chạy.</summary>
    /// <param name="sceneName">Tên scene dùng để chọn bản nhạc.</param>
    private void PlayMusicForScene(string sceneName)
    {
        AudioClip newMusic = null;

        switch (sceneName)
        {
            case "MainMenu":
                newMusic = menuMusic;
                break;

            case "Level1":
                newMusic = level1Music;
                break;

            case "Level2":
                newMusic = level2Music;
                break;

            case "Level3":
                newMusic = level3Music;
                break;

            case "Level4":
                newMusic = level4Music;
                break;

            default:
                return;
        }

        if (newMusic == null)
        {
            Debug.LogWarning(
                "AudioManager: Chưa gán nhạc cho " +
                sceneName
            );
            return;
        }

        // Giữ bài nhạc đang chạy để chuyển scene không làm nhạc bắt đầu lại.
        if (musicSource.clip == newMusic &&
            musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = newMusic;
        musicSource.Play();
    }

    /// <summary>Phát một lượt âm thanh bắn nếu game đang chạy và đã gán clip.</summary>
    public void PlayShootSound()
    {
        if (gameplayPaused || !GameManager.CanAdvanceGameplay) return;

        if (shootSound == null)
        {
            Debug.LogWarning(
                "AudioManager: Chưa gán Shoot Sound!"
            );
            return;
        }

        sfxSource.PlayOneShot(shootSound);
    }

    /// <summary>Tạm dừng các nguồn âm thanh và chỉ tiếp tục những nguồn đang phát trước đó.</summary>
    /// <param name="paused">True để tạm dừng; false để tiếp tục.</param>
    public void SetGameplayPaused(bool paused)
    {
        if (gameplayPaused == paused) return;
        gameplayPaused = paused;
        if (paused)
        {
            // Ghi nhớ nguồn nào đang phát để khi tiếp tục không bật nhầm âm thanh vốn đã dừng.
            resumeMusic = musicSource != null && musicSource.isPlaying;
            resumeSfx = sfxSource != null && sfxSource.isPlaying;
            if (musicSource != null) musicSource.Pause();
            if (sfxSource != null) sfxSource.Pause();
        }
        else
        {
            if (resumeMusic && musicSource != null) musicSource.UnPause();
            if (resumeSfx && sfxSource != null) sfxSource.UnPause();
            resumeMusic = resumeSfx = false;
        }
    }
}
