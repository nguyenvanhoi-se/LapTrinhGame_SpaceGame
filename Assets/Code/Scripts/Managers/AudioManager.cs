using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        PlayMusicForScene(
            SceneManager.GetActiveScene().name
        );
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

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

        // Không restart nếu đang phát đúng bài
        if (musicSource.clip == newMusic &&
            musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = newMusic;
        musicSource.Play();
    }

    public void PlayShootSound()
    {
        if (shootSound == null)
        {
            Debug.LogWarning(
                "AudioManager: Chưa gán Shoot Sound!"
            );
            return;
        }

        sfxSource.PlayOneShot(shootSound);
    }
}