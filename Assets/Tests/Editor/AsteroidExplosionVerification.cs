using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Kiểm tra hiệu ứng nổ bằng prefab thật trong một scene Play Mode riêng.</summary>
[InitializeOnLoad]
public static class AsteroidExplosionVerification
{
    const string Key = "SpaceExplorer.ExplosionVerification";
    const string Work = "Temp/AsteroidExplosionVerification";
    static readonly List<string> results = new List<string>();
    static readonly List<string> errors = new List<string>();
    static readonly Stack<IEnumerator> tasks = new Stack<IEnumerator>();
    static int lastFrame = -1;
    static double deadline;
    static bool originalBackground;
    static Camera camera;

    /// <summary>Giữ danh sách scene đang mở để khôi phục sau khi kiểm tra hiệu ứng.</summary>
    [Serializable] class SavedScenes { public SceneSetup[] scenes; }

    /// <summary>Đăng ký theo dõi trạng thái Play Mode và tiến trình kiểm tra ngay khi Editor nạp lớp.</summary>
    static AsteroidExplosionVerification()
    {
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.update += Tick;
    }

    /// <summary>Lưu trạng thái scene, tạo lại tài nguyên hiệu ứng và vào Play Mode trong scene trống để kiểm tra.</summary>
    [MenuItem("Tools/Space Explorer/Verify Asteroid Explosions")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Run from Edit Mode after compilation.");
        var scenes = EditorSceneManager.GetSceneManagerSetup();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            Check(!UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty, "Save open scenes before verification.");
        Directory.CreateDirectory(Work);
        AsteroidExplosionAssets.Build();
        File.WriteAllText(Work + "/results.txt", "RUNNING\n");
        SessionState.SetString(Key + ".Scenes", JsonUtility.ToJson(new SavedScenes { scenes = scenes }));
        SessionState.SetBool(Key, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    /// <summary>Bắt đầu bộ kiểm tra khi vào Play Mode và khôi phục thiết lập, scene khi kết thúc.</summary>
    /// <param name="state">Trạng thái chuyển đổi Play Mode do Editor phát ra.</param>
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            results.Clear(); errors.Clear(); tasks.Clear(); lastFrame = -1;
            deadline = EditorApplication.timeSinceStartup + 120;
            originalBackground = Application.runInBackground;
            Application.runInBackground = true;
            Application.logMessageReceived += Log;
            tasks.Push(Suite());
        }
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Application.logMessageReceived -= Log;
            Application.runInBackground = originalBackground;
            Time.timeScale = 1;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            var saved = JsonUtility.FromJson<SavedScenes>(SessionState.GetString(Key + ".Scenes", ""));
            SessionState.SetBool(Key, false);
            EditorSceneManager.RestoreSceneManagerSetup(saved.scenes);
        }
    }

    /// <summary>Thu thập log lỗi, ngoại lệ và assert để đánh giá kết quả kiểm tra.</summary>
    /// <param name="message">Nội dung log.</param>
    /// <param name="stack">Dấu vết lời gọi dẫn đến log.</param>
    /// <param name="type">Loại log do Unity cung cấp.</param>
    static void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            errors.Add(message + "\n" + stack);
    }

    /// <summary>Chạy các bước kiểm tra tối đa một lần mỗi frame, xử lý coroutine lồng nhau và dừng khi hoàn tất hoặc lỗi.</summary>
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || tasks.Count == 0 || Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        try
        {
            Check(EditorApplication.timeSinceStartup < deadline, "Verification timed out");
            var current = tasks.Peek();
            if (!current.MoveNext()) tasks.Pop();
            // Tự chạy coroutine con trước khi quay lại bước cha vì đây là vòng cập nhật Editor.
            else if (current.Current is IEnumerator nested) tasks.Push(nested);
            if (tasks.Count == 0)
            {
                Check(errors.Count == 0, "Unity runtime errors: " + string.Join("\n", errors));
                Write("PASS: all asteroid explosion checks; no runtime errors.");
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception exception)
        {
            tasks.Clear(); Write("FAIL: " + exception); EditorApplication.ExitPlaymode();
        }
    }

    /// <summary>Ném ngoại lệ nếu điều kiện không đạt để kết thúc kiểm tra với lý do cụ thể.</summary>
    /// <param name="condition">Điều kiện cần đúng.</param>
    /// <param name="message">Thông báo dùng khi điều kiện sai.</param>
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    /// <summary>Thêm một dòng vào kết quả trong bộ nhớ và ghi lại báo cáo kiểm tra.</summary>
    /// <param name="line">Dòng kết quả cần ghi.</param>
    static void Write(string line) { results.Add(line); File.WriteAllLines(Work + "/results.txt", results); }
    /// <summary>Đọc trường private của đối tượng bằng reflection để kiểm tra cấu hình bên trong.</summary>
    /// <param name="source">Đối tượng chứa trường cần đọc.</param>
    /// <param name="name">Tên trường private.</param>
    /// <returns>Giá trị trường được ép sang kiểu T.</returns>
    static T Field<T>(object source, string name) => (T)source.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(source);
    /// <summary>Chờ đủ số frame để Unity xử lý cập nhật, sự kiện hoặc hủy đối tượng.</summary>
    /// <param name="count">Số frame cần chờ.</param>
    /// <returns>Coroutine trả quyền điều khiển sau mỗi frame.</returns>
    static IEnumerator Frames(int count = 3) { for (int i = 0; i < count; i++) yield return null; }
    /// <summary>Chờ một khoảng thời gian theo đồng hồ game nên chịu ảnh hưởng của Time.timeScale.</summary>
    /// <param name="duration">Số giây trong game cần chờ.</param>
    /// <returns>Coroutine chờ đến thời điểm đã tính.</returns>
    static IEnumerator Seconds(float duration) { float end = Time.time + duration; while (Time.time < end) yield return null; }
    /// <summary>Tìm các vụ nổ đang hoạt động trong scene kiểm tra.</summary>
    /// <returns>Mảng hiệu ứng nổ tìm được, không yêu cầu sắp xếp.</returns>
    static AsteroidExplosion[] Explosions() => Object.FindObjectsByType<AsteroidExplosion>(FindObjectsSortMode.None);
    /// <summary>Nạp prefab thiên thạch theo loại và tạo bản sao để kiểm tra.</summary>
    /// <param name="size">Tên loại Small, Medium hoặc Large.</param>
    /// <param name="position">Vị trí tạo thiên thạch trong thế giới.</param>
    /// <returns>Đối tượng thiên thạch vừa tạo.</returns>
    static GameObject Spawn(string size, Vector3 position) => Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Level/Prefabs/Enemy/Asteroid_" + size + ".prefab"), position, Quaternion.identity);
    /// <summary>Duyệt cả các đối tượng con đang ẩn và xác nhận không có component bị mất script.</summary>
    /// <param name="root">Đối tượng gốc cần kiểm tra.</param>
    static void NoMissing(GameObject root)
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script: " + transform.name);
    }

    /// <summary>Kiểm tra tài nguyên, sát thương, số vụ nổ, tạm dừng và thu hồi; cuối cùng tạo ảnh xem trước theo các mốc thời gian.</summary>
    /// <returns>Coroutine thực hiện tuần tự các tình huống kiểm tra.</returns>
    static IEnumerator Suite()
    {
        Time.timeScale = 1;
        camera = new GameObject("Verification Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true; camera.orthographicSize = 5;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.015f, .025f, .06f, 1);
        var shader = Shader.Find("Space Explorer/Asteroid Particle");
        Check(shader != null && !ShaderUtil.ShaderHasError(shader), "Particle shader compilation failed");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AsteroidExplosionAssets.PrefabPath);
        NoMissing(prefab);
        foreach (var system in prefab.GetComponentsInChildren<ParticleSystem>())
        {
            Check(!system.main.loop && !system.main.playOnAwake && system.emission.rateOverTime.constant == 0, "Continuous emitter: " + system.name);
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            Check(renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture != null && renderer.sharedMaterial.shader == shader, "Missing material/texture/shader");
        }
        Check(prefab.GetComponentsInChildren<Collider2D>().Length == 0, "VFX must not participate in gameplay physics");
        Write("PASS: compiled shader, four one-shot systems, all prefab/material/texture references valid.");

        string[] names = { "Small", "Medium", "Large" };
        int[] hp = { 10, 30, 50 }, count = { 7, 12, 20 }, damage = { 10, 20, 30 };
        for (int i = 0; i < names.Length; i++)
        {
            string size = names[i];
            var asteroid = Spawn(size, new Vector3(.7f, .4f, 0));
            NoMissing(asteroid);
            asteroid.GetComponent<AsteroidController>().enabled = false;
            var health = asteroid.GetComponent<Health>();
            Check(health.GetMaxHealth() == hp[i] && asteroid.GetComponent<AsteroidCollision>().GetDamage() == damage[i], "Gameplay stats changed: " + size);
            var death = asteroid.GetComponent<AsteroidDeathVfx>();
            Check(death != null && Field<AsteroidExplosion>(death, "explosionPrefab") != null, "Missing death prefab reference: " + size);
            if (hp[i] > 10) health.TakeDamage(hp[i] - 10);
            yield return Frames();
            Check(Explosions().Length == 0 && health.GetCurrentHealth() == 10, "Nonlethal damage spawned VFX: " + size);
            Vector3 deathPosition = asteroid.transform.position;
            float diameter = Mathf.Max(asteroid.GetComponent<SpriteRenderer>().bounds.size.x, asteroid.GetComponent<SpriteRenderer>().bounds.size.y);
            var bullet = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Level/Prefabs/Bullet/Bullet.prefab"), deathPosition, Quaternion.identity).GetComponent<Bullet>();
            bullet.Initialize(10);
            typeof(Bullet).GetField("speed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bullet, 0f);
            yield return Seconds(.08f);
            Check(asteroid == null && bullet == null, "Real bullet trigger did not destroy asteroid/bullet: " + size);
            Check(Explosions().Length == 1, "Expected exactly one explosion: " + size);
            var effect = Explosions()[0];
            Check(Vector3.Distance(effect.transform.position, deathPosition) < .001f, "Wrong explosion position");
            Check(Mathf.Abs(effect.transform.localScale.x - diameter / .486f) < .001f, "Wrong explosion scale");
            var fragments = effect.transform.Find("Fragments").GetComponent<ParticleSystem>();
            Check(fragments.emission.GetBurst(0).count.constant == count[i] && fragments.particleCount == count[i], "Wrong fragment count: " + size);
            yield return Seconds(1.2f);
            Check(Explosions().Length == 0 && Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length == 0, "Explosion leaked objects: " + size);
            Write("PASS " + size + ": real bullet kill, nonlethal silence, exact position, scale=" + (diameter / .486f).ToString("F3") + ", fragments=" + count[i] + ", complete cleanup.");

            asteroid = Spawn(size, new Vector3(0, -5.1f, 0));
            yield return Seconds(.08f);
            Check(asteroid == null && Explosions().Length == 0, "Bottom exit spawned explosion: " + size);

            var player = new GameObject("Collision Verification Player");
            player.AddComponent<PlayerConfig>().maxHP = 100;
            var playerHealth = player.AddComponent<PlayerHealth>();
            var rb = player.AddComponent<Rigidbody2D>(); rb.gravityScale = 0; rb.constraints = RigidbodyConstraints2D.FreezeAll;
            player.AddComponent<BoxCollider2D>().size = Vector2.one * .1f;
            asteroid = Spawn(size, Vector3.zero);
            yield return Seconds(.1f);
            Check(asteroid == null && Explosions().Length == 1, "Player collision must spawn exactly one explosion: " + size);
            var collisionEffect = Explosions()[0];
            var collisionFragments = collisionEffect.transform.Find("Fragments").GetComponent<ParticleSystem>();
            Check(collisionFragments.emission.GetBurst(0).count.constant == count[i], "Wrong collision fragment count: " + size);
            Check(playerHealth.GetCurrentHealth() == 100 - damage[i], "Collision damage changed: " + size);
            Object.Destroy(player);
            yield return Seconds(1.2f);
            Check(Explosions().Length == 0, "Collision explosion leaked objects: " + size);
            Write("PASS " + size + ": bottom exit without VFX; real Player collision emits once with matching fragments and complete cleanup; damage unchanged.");
        }

        var repeat = Spawn("Large", Vector3.zero);
        repeat.GetComponent<AsteroidController>().enabled = false;
        repeat.GetComponent<Health>().TakeDamage(50);
        repeat.GetComponent<Health>().TakeDamage(10);
        Check(Explosions().Length == 1, "Multiple lethal hits duplicated effect");
        yield return Frames();
        Time.timeScale = 0;
        yield return Frames(5);
        Check(Explosions().Length == 1, "Effect did not respect pause");
        Time.timeScale = 1;
        yield return Seconds(1.2f);
        Check(Explosions().Length == 0, "Effect leaked after resume");
        Write("PASS: repeated lethal hit emits once; pause/resume retains and then cleans effect.");

        // Render bằng pipeline của project và cố định tuổi hạt để so sánh các mốc hiệu ứng.
        camera.orthographicSize = 1.1f;
        camera.aspect = 2.8f;
        var preview = new List<GameObject>();
        for (int i = 0; i < names.Length; i++)
        {
            var asteroid = Spawn(names[i], new Vector3((i - 1) * 1.65f, 0, 0));
            asteroid.GetComponent<AsteroidController>().enabled = false;
            preview.Add(asteroid);
        }
        Capture("01-asteroids");
        foreach (var asteroid in preview) asteroid.GetComponent<Health>().TakeDamage(100);
        yield return Frames();
        foreach (float age in new[] { .04f, .18f, .45f })
        {
            foreach (var effect in Explosions())
                foreach (var system in effect.GetComponentsInChildren<ParticleSystem>())
                {
                    system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    system.useAutoRandomSeed = false; system.randomSeed = (uint)(system.name.Length * 31);
                    system.Simulate(age, false, true, false); system.Pause(false);
                }
            Capture("explosions-" + age.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        }
        foreach (var effect in Explosions()) Object.Destroy(effect.gameObject);
        yield return Frames();
        Check(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length == 0, "Preview leaked child objects");
        Write("PASS: rendered original sprites and 0.04/0.18/0.45s VFX through the project render pipeline; preview cleaned.");
    }

    /// <summary>Render camera kiểm tra ra ảnh PNG và giải phóng texture tạm sau khi chụp.</summary>
    /// <param name="name">Tên ảnh đầu ra trong thư mục kiểm tra, chưa gồm đuôi .png.</param>
    static void Capture(string name)
    {
        var target = new RenderTexture(1400, 500, 24);
        var image = new Texture2D(1400, 500, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1400, 500), 0, 0); image.Apply();
            File.WriteAllBytes(Work + "/" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous;
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
        }
    }
}
