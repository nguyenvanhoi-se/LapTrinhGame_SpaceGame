using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Kiểm tra tạm dừng và khả năng hoàn thành màn bằng prefab, scene thật với bàn phím mô phỏng.</summary>
[InitializeOnLoad]
public static class PauseBalanceVerification
{
    const string ActiveKey = "SpaceExplorer.PauseChecks.Active";
    const string Work = "Temp/PauseBalanceWork";
    static readonly Stack<IEnumerator> tasks = new Stack<IEnumerator>();
    static readonly List<string> results = new List<string>();
    static readonly List<string> errors = new List<string>();
    static int simulationFailures;
    static int lastFrame = -1;
    static Keyboard keyboard;
    static Keyboard originalKeyboard;
    static InputSettings.BackgroundBehavior originalBackground;
    static InputSettings.EditorInputBehaviorInPlayMode originalInputBehavior;
    static double deadline;

    /// <summary>Lưu thông tin scene đang mở để phục hồi môi trường Editor sau kiểm tra.</summary>
    [Serializable] class SceneInfo { public string path; public bool loaded, active; }
    /// <summary>Xác định cặp prefab tàu và số màn cần chạy lại trong kiểm tra cân bằng.</summary>
    [Serializable] class Trial { public string prefab; public int level; }
    /// <summary>Lưu trạng thái trước kiểm tra để khôi phục dữ liệu người chơi và môi trường Editor.</summary>
    [Serializable] class Backup
    {
        public bool hasShip, hasLevel, hasUnlocked;
        public string ship;
        public int level, unlocked, vSync, targetFps;
        public bool runInBackground;
        public List<SceneInfo> scenes = new List<SceneInfo>();
        public List<Trial> retryTrials = new List<Trial>();
    }
    /// <summary>Đăng ký callback đổi Play Mode và cập nhật Editor để điều phối kiểm tra.</summary>
    static PauseBalanceVerification()
    {
        EditorApplication.playModeStateChanged += OnModeChanged;
        EditorApplication.update += Tick;
    }
    /// <summary>Bắt đầu toàn bộ bộ kiểm tra tạm dừng và các lượt mô phỏng tàu.</summary>
    [MenuItem("Tools/Space Explorer/Verify Pause and Balance")]
    public static void Begin() => StartChecks(new List<Trial>());

    /// <summary>Đọc các cặp tàu, màn thất bại từ báo cáo cũ, giữ bản báo cáo đó rồi chạy lại các cặp đã chọn.</summary>
    [MenuItem("Tools/Space Explorer/Retry Failed Ship Trials")]
    public static void RetryFailedTrials()
    {
        string report = File.ReadAllText(Work + "/playmode-results.txt");
        var trials = Regex.Matches(report, @"FAIL simulation (.+)/Level([1-4]):")
            .Cast<Match>().Select(m => new Trial { prefab = m.Groups[1].Value, level = int.Parse(m.Groups[2].Value) }).ToList();
        if (trials.Count == 0) throw new InvalidOperationException("The last report has no failed reference ship trials.");
        File.WriteAllText(Work + "/initial-matrix-results.txt", report);
        StartChecks(trials);
    }
    /// <summary>Lưu tiến trình, thiết lập và scene hiện tại rồi mở menu trong Play Mode để chạy kiểm tra.</summary>
    /// <param name="retryTrials">Danh sách cặp cần chạy lại; rỗng để chạy toàn bộ bộ kiểm tra.</param>
    static void StartChecks(List<Trial> retryTrials)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Start checks from Edit Mode after compilation.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the open scenes before running checks.");
        var backup = new Backup
        {
            hasShip = PlayerPrefs.HasKey("SelectedPlayer"), ship = PlayerPrefs.GetString("SelectedPlayer", ""),
            hasLevel = PlayerPrefs.HasKey("LastLevel"), level = PlayerPrefs.GetInt("LastLevel"),
            hasUnlocked = PlayerPrefs.HasKey("UnlockedLevel"), unlocked = PlayerPrefs.GetInt("UnlockedLevel"),
            vSync = QualitySettings.vSyncCount, targetFps = Application.targetFrameRate,
            runInBackground = Application.runInBackground,
            retryTrials = retryTrials,
            scenes = EditorSceneManager.GetSceneManagerSetup().Select(s => new SceneInfo { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToList()
        };
        Directory.CreateDirectory(Work);
        File.WriteAllText(Work + "/test-backup.json", JsonUtility.ToJson(backup, true));
        File.WriteAllText(Work + "/playmode-results.txt", "RUNNING\n");
        SessionState.SetBool(ActiveKey, true);
        EditorSceneManager.OpenScene("Assets/Level/UI/MainMenu/MainMenu.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
    /// <summary>Thiết lập bàn phím và kho lịch sử tạm khi bắt đầu, sau đó khôi phục thiết bị, tiến trình và scene khi thoát.</summary>
    /// <param name="change">Trạng thái chuyển đổi Play Mode của Editor.</param>
    static void OnModeChanged(PlayModeStateChange change)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            results.Clear(); errors.Clear(); tasks.Clear(); lastFrame = -1; simulationFailures = 0;
            originalKeyboard = Keyboard.current;
            originalBackground = InputSystem.settings.backgroundBehavior;
            originalInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("PauseBalanceTestKeyboard"); keyboard.MakeCurrent();
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 120;
            deadline = EditorApplication.timeSinceStartup + 900;
            Application.logMessageReceived += CaptureLog;
            // Dùng kho tạm để lượt mô phỏng không được ghi vào lịch sử thật của người chơi.
            typeof(GameHistoryManager).GetField("store", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(GameHistoryManager.Instance, new GameHistoryStore(Path.GetFullPath(Work + "/isolated-history-" + Guid.NewGuid().ToString("N") + ".json")));
            var backup = JsonUtility.FromJson<Backup>(File.ReadAllText(Work + "/test-backup.json"));
            tasks.Push(backup.retryTrials.Count == 0 ? Suite() : RetrySuite(backup.retryTrials));
        }
        if (change == PlayModeStateChange.ExitingPlayMode)
        {
            Application.logMessageReceived -= CaptureLog;
            Time.timeScale = 1;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
            InputSystem.settings.backgroundBehavior = originalBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = originalInputBehavior;
            RestorePrefs();
        }
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            RestorePrefs();
            var backup = JsonUtility.FromJson<Backup>(File.ReadAllText(Work + "/test-backup.json"));
            QualitySettings.vSyncCount = backup.vSync; Application.targetFrameRate = backup.targetFps;
            Application.runInBackground = backup.runInBackground;
            EditorSceneManager.RestoreSceneManagerSetup(backup.scenes.Select(s => new SceneSetup { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
            SessionState.SetBool(ActiveKey, false);
        }
    }
    /// <summary>Khôi phục các khóa lựa chọn tàu và tiến trình đúng như trước khi kiểm tra.</summary>
    static void RestorePrefs()
    {
        var b = JsonUtility.FromJson<Backup>(File.ReadAllText(Work + "/test-backup.json"));
        if (b.hasShip) PlayerPrefs.SetString("SelectedPlayer", b.ship); else PlayerPrefs.DeleteKey("SelectedPlayer");
        if (b.hasLevel) PlayerPrefs.SetInt("LastLevel", b.level); else PlayerPrefs.DeleteKey("LastLevel");
        if (b.hasUnlocked) PlayerPrefs.SetInt("UnlockedLevel", b.unlocked); else PlayerPrefs.DeleteKey("UnlockedLevel");
        PlayerPrefs.Save();
    }
    /// <summary>Thu thập log lỗi để bộ kiểm tra không bỏ sót ngoại lệ lúc game chạy.</summary>
    /// <param name="message">Nội dung log.</param>
    /// <param name="stack">Dấu vết lời gọi.</param>
    /// <param name="type">Loại log do Unity cung cấp.</param>
    static void CaptureLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack);
    }
    /// <summary>Chạy các bước và coroutine lồng nhau mỗi frame, kiểm soát hạn chờ rồi ghi kết quả khi hoàn tất hoặc thất bại.</summary>
    static void Tick()
    {
        if (!EditorApplication.isPlaying || tasks.Count == 0 || Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        try
        {
            Check(EditorApplication.timeSinceStartup < deadline, "Play Mode checks timed out");
            if (!tasks.Peek().MoveNext()) tasks.Pop();
            else if (tasks.Peek().Current is IEnumerator nested) tasks.Push(nested);
            if (tasks.Count == 0)
            {
                Check(errors.Count == 0, "Unity logged runtime errors: " + string.Join("\n", errors));
                Write("PASS: all automated checks completed");
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception e)
        {
            tasks.Clear(); Write("FAIL: " + e); EditorApplication.ExitPlaymode();
        }
    }
    /// <summary>Ném ngoại lệ khi điều kiện kiểm tra không đạt.</summary>
    /// <param name="value">Điều kiện cần đúng.</param>
    /// <param name="message">Thông báo giải thích thất bại.</param>
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    /// <summary>Thêm thông báo vào kết quả và ghi lại báo cáo Play Mode.</summary>
    /// <param name="message">Dòng thông báo cần lưu.</param>
    static void Write(string message) { results.Add(message); File.WriteAllLines(Work + "/playmode-results.txt", results); }
    /// <summary>Tìm đúng một đối tượng đang hoạt động của kiểu yêu cầu, báo lỗi khi số lượng khác một.</summary>
    /// <returns>Đối tượng duy nhất có kiểu T.</returns>
    static T One<T>() where T : Object => Object.FindObjectsByType<T>().Single();
    /// <summary>Đọc trường private qua reflection để kiểm tra dữ liệu mà không đổi API của game.</summary>
    /// <param name="target">Đối tượng chứa trường.</param>
    /// <param name="name">Tên trường private cần đọc.</param>
    /// <returns>Giá trị trường dưới kiểu T.</returns>
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    /// <summary>Tìm duy nhất nút có tên yêu cầu trong cây giao diện tạm dừng, kể cả đối tượng đang ẩn.</summary>
    /// <param name="menu">Giao diện chứa nút cần tìm.</param>
    /// <param name="name">Tên GameObject của nút.</param>
    /// <returns>Button có tên tương ứng.</returns>
    static Button Button(PauseMenuUI menu, string name) => menu.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
    /// <summary>Chờ số frame yêu cầu để Unity xử lý cập nhật và chuyển scene.</summary>
    /// <param name="count">Số frame cần chờ.</param>
    /// <returns>Coroutine chờ theo frame.</returns>
    static IEnumerator Frames(int count = 2) { for (int i = 0; i < count; i++) yield return null; }
    /// <summary>Chờ theo đồng hồ game, có chịu ảnh hưởng của tốc độ hoặc tạm dừng thời gian.</summary>
    /// <param name="duration">Số giây trong game cần chờ.</param>
    /// <returns>Coroutine chờ theo Time.time.</returns>
    static IEnumerator Scaled(float duration) { float end = Time.time + duration; while (Time.time < end) yield return null; }
    /// <summary>Chờ theo đồng hồ thực để kiểm tra trạng thái game lúc timeScale bằng 0.</summary>
    /// <param name="duration">Số giây thực cần chờ.</param>
    /// <returns>Coroutine chờ không phụ thuộc Time.timeScale.</returns>
    static IEnumerator Real(float duration) { float end = Time.realtimeSinceStartup + duration; while (Time.realtimeSinceStartup < end) yield return null; }
    /// <summary>Khôi phục timeScale, tải màn và xác nhận bộ quản lý, máu ban đầu, giao diện tạm dừng cùng tỉ lệ màn hình.</summary>
    /// <param name="level">Số màn cần tải để kiểm tra.</param>
    /// <returns>Coroutine chờ màn mới khởi tạo.</returns>
    static IEnumerator Load(int level)
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Level" + level); yield return Frames(3);
        Check(GameManager.Instance != null && GameManager.Instance.GetCurrentLevel() == level, "Wrong level loaded");
        Check(Object.FindObjectsByType<PauseMenuUI>().Length == 1, "Expected one pause UI");
        Check(One<PlayerHealth>().GetCurrentHealth() == One<PlayerConfig>().maxHP, "Player HP not initialized from config");
        Check(Mathf.Abs(Camera.main.aspect - 9f / 16f) < .02f, "Run these gameplay trials in a portrait 9:16 Game View");
    }
    /// <summary>Gửi trạng thái nhấn rồi thả một phím vào bàn phím mô phỏng, chờ vài frame giữa hai thao tác.</summary>
    /// <param name="key">Phím cần mô phỏng.</param>
    /// <returns>Coroutine thực hiện đủ một lần nhấn và thả.</returns>
    static IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return Frames(2);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return Frames(2);
    }
    /// <summary>Kiểm tra tạm dừng, âm thanh, giao diện, chuyển màn và chạy mô phỏng mỗi tàu qua bốn màn.</summary>
    /// <returns>Coroutine thực hiện toàn bộ bộ kiểm tra.</returns>
    static IEnumerator Suite()
    {
        yield return Frames(4);
        Check(ShipManager.Instance != null && LevelManager.Instance != null && AudioManager.Instance != null, "MainMenu bootstrap missing");
        var musicManager = AudioManager.Instance;
        var originalAudio = musicManager;
        Check(musicManager.GetComponents<AudioSource>().Length == 2, "Audio source count must stay two");
        foreach (int level in new[] { 1, 2, 3, 4 })
        {
            UnityEngine.Random.InitState(1000 + level);
            yield return Load(level);
            var game = GameManager.Instance; var player = One<PlayerHealth>(); var menu = One<PauseMenuUI>();
            foreach (var transform in menu.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing pause UI script");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/Audiowide-Regular SDF.asset");
            foreach (var text in menu.GetComponentsInChildren<TMP_Text>(true)) Check(text.font == font, "Wrong pause UI font");
            Check(((Color32)menu.GetComponentsInChildren<TMP_Text>(true).Single(t => t.text == "PAUSED").color).Equals(new Color32(0xF4, 0xF8, 0xFF, 255)), "Wrong title color");
            Check(game.GetRequiredScore() == level * 100, "Wrong required score");
            // Chờ đủ đối tượng đang chạy vì súng tự bắn có thể đã phá thiên thạch đầu tiên.
            float probeDeadline = Time.time + 12f;
            while (!(Object.FindAnyObjectByType<AsteroidController>() != null &&
                Object.FindAnyObjectByType<Star>() != null && Object.FindAnyObjectByType<Bullet>() != null))
            {
                Check(Time.time < probeDeadline, "Could not obtain live asteroid/star/bullet probes");
                yield return null;
            }
            var objects = Object.FindObjectsByType<Transform>()
                .Where(t => t.GetComponent<PlayerHealth>() != null || t.GetComponent<AsteroidController>() != null || t.GetComponent<Star>() != null || t.GetComponent<Bullet>() != null)
                .ToDictionary(t => t, t => t.position);
            Check(objects.Keys.Any(t => t.GetComponent<AsteroidController>() != null) && objects.Keys.Any(t => t.GetComponent<Star>() != null) && objects.Keys.Any(t => t.GetComponent<Bullet>() != null), "Pause test needs live asteroid, star and bullet");
            int score = ScoreManager.Instance.GetLevelScore(), hp = player.GetCurrentHealth();
            float clock = Time.time;
            var asteroidSpawn = One<AsteroidSpawner>(); var starSpawn = One<StarSpawner>();
            float nextAsteroid = Field<float>(asteroidSpawn, "nextSpawnTime"), nextStar = Field<float>(starSpawn, "nextSpawnTime");
            var music = Field<AudioSource>(musicManager, "musicSource"); bool wasPlaying = music.isPlaying;
            Button(menu, "PauseButton").onClick.Invoke();
            Check(game.IsPaused && Time.timeScale == 0 && Field<GameObject>(menu, "pausePanel").activeSelf, "Pause button did not freeze/open panel");
            var labelState = Button(menu, "RestartButton").GetComponent<ButtonTextState>();
            var label = labelState.GetComponentInChildren<TMP_Text>();
            var pointer = new PointerEventData(EventSystem.current);
            labelState.OnDeselect(pointer); labelState.OnPointerExit(pointer);
            Check(((Color32)label.color).Equals(new Color32(255, 255, 255, 255)), "Wrong normal color");
            labelState.OnPointerEnter(pointer);
            Check(((Color32)label.color).Equals(new Color32(0x66, 0xE6, 0xFF, 255)), "Wrong hover color");
            labelState.OnPointerExit(pointer); labelState.OnSelect(pointer);
            Check(((Color32)label.color).Equals(new Color32(0x66, 0xE6, 0xFF, 255)), "Wrong focus color");
            labelState.OnDeselect(pointer);
            Check(((Color32)label.color).Equals(new Color32(255, 255, 255, 255)), "Label color did not reset");
            menu.Pause(); Check(Time.timeScale == 0, "Duplicate pause changed time scale");
            ScoreManager.Instance.AddScore(9999); player.TakeDamage(9999);
            var rock = objects.Keys.Select(t => t.GetComponent<Health>()).FirstOrDefault(h => h != null);
            int rockHp = rock.GetCurrentHealth(); rock.TakeDamage(9999);
            musicManager.PlayShootSound();
            yield return Real(.4f);
            Check(Time.time == clock, "Scaled gameplay timer advanced during pause");
            Check(ScoreManager.Instance.GetLevelScore() == score && player.GetCurrentHealth() == hp && rock.GetCurrentHealth() == rockHp, "Score or HP changed while paused");
            foreach (var pair in objects) Check(pair.Key != null && Vector3.Distance(pair.Key.position, pair.Value) < .001f, "Object moved/destroyed while paused");
            Check(Field<float>(asteroidSpawn, "nextSpawnTime") == nextAsteroid && Field<float>(starSpawn, "nextSpawnTime") == nextStar, "Spawn timer advanced while paused");
            Check(musicManager.IsGameplayPaused && !music.isPlaying && !Field<AudioSource>(musicManager, "sfxSource").isPlaying, "Audio did not pause");
            Button(menu, "ResumeButton").onClick.Invoke(); yield return Frames(2);
            Check(!game.IsPaused && Time.timeScale == 1 && !Field<GameObject>(menu, "pausePanel").activeSelf, "Resume failed at timeScale zero");
            Check(!wasPlaying || music.isPlaying, "Music did not resume");
            ScoreManager.Instance.AddScore(10); player.TakeDamage(1);
            Check(ScoreManager.Instance.GetLevelScore() == score + 10 && player.GetCurrentHealth() == hp - 1, "Score/HP did not resume");
            var texts = Object.FindObjectsByType<TMP_Text>();
            Check(texts.Single(t => t.name == "ScoreText").text == "SCORE: " + (score + 10), "Score HUD did not update");
            Check(texts.Single(t => t.name == "HPText").text.Contains((hp - 1) + "/"), "HP HUD did not update");
            Time.timeScale = .6f; menu.Pause(); menu.Resume(); Check(Mathf.Approximately(Time.timeScale, .6f), "Resume did not restore prior scale"); Time.timeScale = 1;
            yield return Press(Key.Escape); Check(game.IsPaused, "Escape did not pause");
            yield return Press(Key.P); Check(!game.IsPaused, "P did not resume");
            menu.Pause(); var oldPlayer = player; Button(menu, "RestartButton").onClick.Invoke();
            Check(Time.timeScale > 0, "Restart left timeScale zero"); yield return Frames(3);
            Check(SceneManager.GetActiveScene().name == "Level" + level && One<PlayerHealth>() != oldPlayer && ScoreManager.Instance.GetLevelScore() == 0, "Restart did not reset the current level");
            Check(AudioManager.Instance == originalAudio && AudioManager.Instance.GetComponents<AudioSource>().Length == 2, "Restart duplicated/lost AudioManager");
            menu = One<PauseMenuUI>(); menu.Pause(); Button(menu, "MainMenuButton").onClick.Invoke(); yield return Frames(3);
            Check(SceneManager.GetActiveScene().name == "MainMenu" && Time.timeScale > 0 && AudioManager.Instance == originalAudio, "Main Menu transition failed");
            yield return Load(level);
            PlayerPrefs.SetInt("UnlockedLevel", level);
            ScoreManager.Instance.AddScore(GameManager.Instance.GetRequiredScore()); yield return Frames(3);
            Check(Time.timeScale > 0, "Level Complete left timeScale zero");
            if (level < 4) Check(SceneManager.GetActiveScene().name == "Level" + (level + 1) && LevelManager.Instance.IsLevelUnlocked(level + 1), "Completion/unlock failed");
            else Check(GameManager.Instance.GetGameState() == GameManager.GameState.LevelComplete && !GameManager.Instance.PauseGame(), "Pause allowed after Level Complete");
            yield return Load(level); One<PlayerHealth>().TakeDamage(10000); yield return Frames(3);
            Check(SceneManager.GetActiveScene().name == "EndGame" && Time.timeScale > 0, "HP death/Game Over transition failed");
            yield return Load(level); GameManager.Instance.PauseGame(); GameManager.Instance.GameOver(); yield return Frames(3);
            Check(SceneManager.GetActiveScene().name == "EndGame" && Time.timeScale > 0 && !musicManager.IsGameplayPaused, "External Game Over did not release pause");
            Write("PASS Level" + level + ": live movement/bullet/star/spawn/timers/score/HP frozen; audio/resume/prior scale/Escape/P/restart/menu/completion/unlock/Game Over OK");
        }
        var prefabs = ShipManager.Instance.GetPlayerPrefabs().Where(p => p != null).ToArray();
        foreach (var prefab in prefabs)
        foreach (int level in new[] { 1, 2, 3, 4 })
        {
            ShipManager.Instance.SelectPlayer(prefab);
            UnityEngine.Random.InitState(20261020 + Array.IndexOf(prefabs, prefab) * 100 + level);
            yield return Load(level);
            yield return BotRun(prefab, level);
        }
        Check(simulationFailures == 0, simulationFailures + " reference keyboard simulations failed; review the detailed results");
        Write("PASS no runtime error/exception logs; PlayerPrefs and original scene setup will be restored on exit");
    }
    /// <summary>Thử thêm tối đa ba hạt giống ngẫu nhiên cho từng cặp tàu, màn đã thất bại và yêu cầu có ít nhất một lần hoàn thành.</summary>
    /// <param name="trials">Các cặp tàu và màn cần kiểm tra bổ sung.</param>
    /// <returns>Coroutine chạy lại các tình huống đã chọn.</returns>
    static IEnumerator RetrySuite(List<Trial> trials)
    {
        yield return Frames(4);
        var prefabs = ShipManager.Instance.GetPlayerPrefabs().Where(p => p != null).ToArray();
        foreach (var trial in trials)
        {
            var prefab = prefabs.Single(p => p.name == trial.prefab);
            bool completed = false;
            for (int attempt = 1; attempt <= 3 && !completed; attempt++)
            {
                int seed = 20261020 + Array.IndexOf(prefabs, prefab) * 100 + trial.level + attempt * 10000;
                Write("Additional trial " + trial.prefab + "/Level" + trial.level + "; seed=" + seed);
                ShipManager.Instance.SelectPlayer(prefab);
                UnityEngine.Random.InitState(seed);
                yield return Load(trial.level);
                int losses = simulationFailures;
                yield return BotRun(prefab, trial.level);
                completed = losses == simulationFailures;
            }
            Check(completed, "No witnessed completion for " + trial.prefab + "/Level" + trial.level + " across three additional seeds");
        }
        Write("PASS additional feasibility trials; initial losses remain recorded in initial-matrix-results.txt");
    }

    /// <summary>Điều khiển tàu bằng phím mô phỏng để nhắm thiên thạch, nhặt sao và tránh va chạm, rồi ghi kết quả lượt thử.</summary>
    /// <param name="prefab">Prefab tàu dùng để đối chiếu chỉ số và ghi báo cáo.</param>
    /// <param name="level">Màn đang được thử.</param>
    /// <returns>Coroutine mô phỏng đến khi kết thúc màn hoặc hết 180 giây trong game.</returns>
    static IEnumerator BotRun(GameObject prefab, int level)
    {
        var player = One<PlayerHealth>(); var config = player.GetComponent<PlayerConfig>();
        Check(config.maxHP == prefab.GetComponent<PlayerConfig>().maxHP && config.damage == prefab.GetComponent<PlayerConfig>().damage && config.bulletCount == prefab.GetComponent<PlayerConfig>().bulletCount, "Player prefab stats changed");
        float fireRate = Field<float>(player.GetComponentInChildren<WeaponAim>(), "fireRate");
        var spawner = One<AsteroidSpawner>(); int maxActive = 0, spawned = 0;
        var seen = new HashSet<AsteroidController>();
        float begin = Time.time; Time.timeScale = 2;
        int remainingHp = player.GetCurrentHealth(), stationaryFrames = 0;
        while (SceneManager.GetActiveScene().name == "Level" + level && GameManager.Instance != null && GameManager.Instance.GetGameState() == GameManager.GameState.Playing && Time.time - begin < 180)
        {
            var rocks = Object.FindObjectsByType<AsteroidController>();
            remainingHp = player.GetCurrentHealth();
            maxActive = Mathf.Max(maxActive, spawner.ActiveCount);
            Check(spawner.ActiveCount <= spawner.MaxActiveAsteroids, "Active asteroid cap exceeded");
            foreach (var rock in rocks) if (seen.Add(rock)) spawned++;
            Vector2 position = player.transform.position, target = new Vector2(position.x, -4f);
            var camera = Camera.main;
            AsteroidController urgent = null; float urgency = float.MaxValue;
            foreach (var rock in rocks)
            {
                // Súng chỉ bắn lên trên nên bỏ qua thiên thạch đã thấp hơn vùng ngắm trước tàu.
                if (rock.transform.position.y < position.y + 1.2f) continue;
                float shots = Mathf.Ceil(rock.GetComponent<Health>().GetCurrentHealth() / (float)(config.damage * config.bulletCount));
                float align = Mathf.Abs(rock.transform.position.x - position.x) / config.moveSpeed;
                float flight = Mathf.Max(0, rock.transform.position.y - position.y) / (10 + rock.CurrentMoveSpeed);
                // Thời gian còn dư = thời gian thiên thạch tới gần tàu trừ thời gian căn hướng, đạn bay và bắn.
                float slack = (rock.transform.position.y - position.y - 1.2f) / rock.CurrentMoveSpeed - align - flight - shots * fireRate;
                if (slack < urgency) { urgency = slack; urgent = rock; }
            }
            var star = Object.FindObjectsByType<Star>().Where(s => s.transform.position.y < -1.5f && s.transform.position.y > -4.8f).OrderBy(s => s.transform.position.y).FirstOrDefault();
            if (urgent != null && urgency < 1f) target.x = urgent.transform.position.x;
            else if (star != null) target.x = star.transform.position.x;
            else if (urgent != null) target.x = urgent.transform.position.x;
            // Ưu tiên né thiên thạch sắp va chạm trước khi tiếp tục di chuyển để ngắm bắn.
            foreach (var rock in rocks)
            {
                Vector2 future = (Vector2)rock.transform.position + Vector2.down * rock.CurrentMoveSpeed * .3f;
                if (Mathf.Abs(future.y - position.y) > 1.3f || Mathf.Abs(future.x - position.x) > 1.2f) continue;
                float left = future.x - 1.6f, right = future.x + 1.6f;
                float limit = camera.orthographicSize * camera.aspect - .65f;
                if (left < -limit) target.x = right;
                else if (right > limit) target.x = left;
                else target.x = Mathf.Abs(position.x - left) < Mathf.Abs(position.x - right) ? left : right;
                target.y = position.y;
                break;
            }
            var keys = new List<Key>();
            // Dùng ngưỡng lệch nhỏ để tàu một nòng vẫn có thể căn bắn trúng.
            const float tolerance = .08f;
            if (target.x - position.x > tolerance) keys.Add(Key.D); else if (position.x - target.x > tolerance) keys.Add(Key.A);
            if (target.y - position.y > tolerance) keys.Add(Key.W); else if (position.y - target.y > tolerance) keys.Add(Key.S);
            stationaryFrames = keys.Count == 0 ? stationaryFrames + 1 : 0;
            Time.timeScale = stationaryFrames > 3 ? 2f : Mathf.Abs(target.x - position.x) < .7f ? 1f : 2f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
            yield return null;
        }
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        Time.timeScale = 1;
        // Chờ thêm frame để lệnh tải scene hoàn tất trước khi đọc kết quả.
        yield return Frames(3);
        bool complete = level < 4 ? SceneManager.GetActiveScene().name == "Level" + (level + 1) : GameManager.Instance != null && GameManager.Instance.GetGameState() == GameManager.GameState.LevelComplete;
        Write((complete ? "PASS" : "FAIL") + " simulation " + prefab.name + "/Level" + level + ": completed=" + complete + "; scene=" + SceneManager.GetActiveScene().name + "; elapsed=" + (Time.time - begin).ToString("F1") + "s; spawned=" + spawned + "; peakActive=" + maxActive + "; remainingHP=" + remainingHp);
        if (!complete) simulationFailures++;
    }
}
