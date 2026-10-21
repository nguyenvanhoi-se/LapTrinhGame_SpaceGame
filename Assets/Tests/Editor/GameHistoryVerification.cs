using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Kiểm tra kho lịch sử và luồng giao diện qua các phiên Play Mode bằng dữ liệu tạm riêng.</summary>
[InitializeOnLoad]
public static class GameHistoryVerification
{
    const string Work = "Temp/GameHistoryWork";
    const string Active = "SpaceExplorer.HistoryTests.Active";
    const string Phase = "SpaceExplorer.HistoryTests.Phase";
    const string PendingPrefab = "SpaceExplorer.HistoryTests.PendingPrefab";
    const string Main = "Assets/Level/UI/MainMenu/MainMenu.unity";
    static readonly List<string> report = new List<string>();
    static readonly List<string> errors = new List<string>();
    static readonly Stack<IEnumerator> tasks = new Stack<IEnumerator>();
    static int frame;
    static double deadline;
    /// <summary>Lưu đường dẫn cùng trạng thái tải và kích hoạt của một scene để khôi phục.</summary>
    [Serializable] sealed class OpenScene { public string path; public bool active, loaded; }
    /// <summary>Lưu trạng thái trước kiểm tra để khôi phục dữ liệu người chơi và môi trường Editor.</summary>
    [Serializable] sealed class Backup
    {
        public string historyPath, realHistoryPath, realHistoryBytes, ship, prefabStagePath;
        public bool realFileExists, hasShip, hasLevel, hasUnlocked, hasSentinel, runInBackground;
        public int level, unlocked, sentinel;
        public List<OpenScene> scenes;
    }
    /// <summary>Đăng ký callback để tiếp tục kiểm tra khi Editor cập nhật hoặc đổi trạng thái Play Mode.</summary>
    static GameHistoryVerification()
    {
        EditorApplication.playModeStateChanged += OnMode;
        EditorApplication.update += Tick;
    }
    /// <summary>Kiểm tra scene đã lưu, sao lưu trạng thái, kiểm tra kho dữ liệu tạm rồi mở menu trong Play Mode.</summary>
    [MenuItem("Tools/Space Explorer/Verify Game History")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Run from Edit Mode after compiling.");
        var prefabStage=PrefabStageUtility.GetCurrentPrefabStage();
        if(prefabStage!=null&&prefabStage.scene.isDirty) throw new InvalidOperationException("Save the open prefab before running Play Mode checks.");
        if(prefabStage!=null)
        {
            SessionState.SetString(PendingPrefab,prefabStage.assetPath);
            StageUtility.GoToMainStage();
            // Chờ Editor ổn định lại bố cục Canvas sau khi rời chế độ chỉnh prefab riêng.
            EditorApplication.delayCall+=Begin;
            return;
        }
        string prefabStagePath=SessionState.GetString(PendingPrefab,"");
        SessionState.EraseString(PendingPrefab);
        for (int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty)
        {
            if(!string.IsNullOrEmpty(prefabStagePath)) PrefabStageUtility.OpenPrefab(prefabStagePath);
            throw new InvalidOperationException("Save open scenes before running history checks.");
        }
        Directory.CreateDirectory(Work);
        string real=Path.Combine(Application.persistentDataPath,"game_history.json");
        var backup=new Backup
        {
            historyPath=Path.GetFullPath(Work+"/test-"+Guid.NewGuid().ToString("N")+"/game_history.json"),
            realHistoryPath=real,realFileExists=File.Exists(real),realHistoryBytes=File.Exists(real)?Convert.ToBase64String(File.ReadAllBytes(real)):"",
            hasShip=PlayerPrefs.HasKey("SelectedPlayer"),ship=PlayerPrefs.GetString("SelectedPlayer"),
            hasLevel=PlayerPrefs.HasKey("LastLevel"),level=PlayerPrefs.GetInt("LastLevel"),
            hasUnlocked=PlayerPrefs.HasKey("UnlockedLevel"),unlocked=PlayerPrefs.GetInt("UnlockedLevel"),
            hasSentinel=PlayerPrefs.HasKey("HistoryTestSentinel"),sentinel=PlayerPrefs.GetInt("HistoryTestSentinel"),
            runInBackground=Application.runInBackground,
            prefabStagePath=prefabStagePath,
            scenes=EditorSceneManager.GetSceneManagerSetup().Select(s=>new OpenScene{path=s.path,active=s.isActive,loaded=s.isLoaded}).ToList()
        };
        File.WriteAllText(Work+"/test-backup.json",JsonUtility.ToJson(backup,true));
        report.Clear(); errors.Clear();
        StoreChecks(Path.GetDirectoryName(backup.historyPath));
        File.WriteAllLines(Work+"/results.txt",report);
        SessionState.SetInt(Phase,1); SessionState.SetBool(Active,true);
        EditorSceneManager.OpenScene(Main,OpenSceneMode.Single); EditorApplication.EnterPlaymode();
    }
    /// <summary>Đọc trạng thái đã sao lưu từ JSON trong thư mục kiểm tra.</summary>
    /// <returns>Thông tin cần khôi phục sau khi kiểm tra.</returns>
    static Backup ReadBackup()=>JsonUtility.FromJson<Backup>(File.ReadAllText(Work+"/test-backup.json"));
    /// <summary>Chuyển kho sang file tạm, điều phối hai phiên Play Mode và khôi phục scene, thiết lập ban đầu.</summary>
    /// <param name="mode">Trạng thái chuyển đổi Play Mode của Editor.</param>
    static void OnMode(PlayModeStateChange mode)
    {
        if(!SessionState.GetBool(Active,false)) return;
        if(mode==PlayModeStateChange.EnteredPlayMode)
        {
            report.Clear(); report.AddRange(File.ReadAllLines(Work+"/results.txt")); errors.Clear(); tasks.Clear(); frame=-1;
            var b=ReadBackup(); Application.runInBackground=true;
            // Chuyển sang kho tạm trước khi màn chơi đầu tiên có thể ghi kết quả.
            typeof(GameHistoryManager).GetField("store",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(GameHistoryManager.Instance,new GameHistoryStore(b.historyPath));
            Application.logMessageReceived+=Capture;
            deadline=EditorApplication.timeSinceStartup+180;
            tasks.Push(SessionState.GetInt(Phase,1)==1?Suite():RelaunchSuite());
        }
        if(mode==PlayModeStateChange.ExitingPlayMode)
        {
            Application.logMessageReceived-=Capture; Time.timeScale=1;
        }
        if(mode==PlayModeStateChange.EnteredEditMode)
        {
            if(SessionState.GetInt(Phase,0)==2)
            {
                EditorSceneManager.OpenScene(Main,OpenSceneMode.Single);
                EditorApplication.delayCall+=()=>EditorApplication.EnterPlaymode();
                return;
            }
            var b=ReadBackup();
            if(b.hasShip) PlayerPrefs.SetString("SelectedPlayer",b.ship); else PlayerPrefs.DeleteKey("SelectedPlayer");
            if(b.hasLevel) PlayerPrefs.SetInt("LastLevel",b.level); else PlayerPrefs.DeleteKey("LastLevel");
            if(b.hasUnlocked) PlayerPrefs.SetInt("UnlockedLevel",b.unlocked); else PlayerPrefs.DeleteKey("UnlockedLevel");
            if(b.hasSentinel) PlayerPrefs.SetInt("HistoryTestSentinel",b.sentinel); else PlayerPrefs.DeleteKey("HistoryTestSentinel");
            PlayerPrefs.Save(); Application.runInBackground=b.runInBackground;
            EditorSceneManager.RestoreSceneManagerSetup(b.scenes.Select(s=>new SceneSetup{path=s.path,isLoaded=s.loaded,isActive=s.active}).ToArray());
            if(!string.IsNullOrEmpty(b.prefabStagePath)) PrefabStageUtility.OpenPrefab(b.prefabStagePath);
            SessionState.SetBool(Active,false);
        }
    }
    /// <summary>Gom các log lỗi trong lúc chạy để đưa vào kết quả kiểm tra.</summary>
    /// <param name="message">Nội dung log.</param>
    /// <param name="stack">Dấu vết lời gọi tại thời điểm ghi log.</param>
    /// <param name="type">Loại log để lọc lỗi, ngoại lệ và assert.</param>
    static void Capture(string message,string stack,LogType type) { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert) errors.Add(message+"\n"+stack); }
    /// <summary>Tiến hành các coroutine kiểm tra theo frame và chuyển phiên hoặc kết thúc khi toàn bộ bước hoàn tất.</summary>
    static void Tick()
    {
        if(!EditorApplication.isPlaying||tasks.Count==0||frame==Time.frameCount) return; frame=Time.frameCount;
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"History checks timed out");
            if(!tasks.Peek().MoveNext()) tasks.Pop(); else if(tasks.Peek().Current is IEnumerator child) tasks.Push(child);
            if(tasks.Count==0)
            {
                Check(errors.Count==0,"Runtime error logs: "+string.Join("\n",errors));
                if(SessionState.GetInt(Phase,0)==1) { SessionState.SetInt(Phase,2); Write("PASS first runtime session; relaunching with fresh history manager"); }
                else { SessionState.SetInt(Phase,3); Write("PASS all Game History checks; original PlayerPrefs and scenes restored on exit"); }
                EditorApplication.ExitPlaymode();
            }
        }
        catch(Exception e) { tasks.Clear(); SessionState.SetInt(Phase,3); Write("FAIL "+e); EditorApplication.ExitPlaymode(); }
    }
    /// <summary>Dừng tình huống kiểm tra bằng ngoại lệ khi kết quả không như mong đợi.</summary>
    /// <param name="value">Điều kiện cần đúng.</param>
    /// <param name="message">Mô tả lỗi khi điều kiện sai.</param>
    static void Check(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
    /// <summary>Bổ sung dòng kết quả và ghi toàn bộ báo cáo ra file.</summary>
    /// <param name="line">Nội dung kết quả cần thêm.</param>
    static void Write(string line) { report.Add(line); File.WriteAllLines(Work+"/results.txt",report); }
    /// <summary>Đọc trường private bằng reflection để xác nhận trạng thái hoặc cấu hình bên trong.</summary>
    /// <param name="obj">Đối tượng cần đọc.</param>
    /// <param name="name">Tên trường private.</param>
    /// <returns>Giá trị trường dưới kiểu T.</returns>
    static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
    /// <summary>Lấy đúng một đối tượng đang hoạt động thuộc kiểu yêu cầu; báo lỗi nếu thiếu hoặc có nhiều hơn một.</summary>
    /// <returns>Đối tượng duy nhất có kiểu T.</returns>
    static T One<T>() where T:Object=>Object.FindObjectsByType<T>().Single();
    /// <summary>Tìm duy nhất nút có tên yêu cầu, kể cả nút đang ẩn.</summary>
    /// <param name="name">Tên GameObject chứa nút cần tìm.</param>
    /// <returns>Button có tên tương ứng.</returns>
    static Button Button(string name)=>Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(b=>b.name==name);
    /// <summary>Chờ một số frame để scene và giao diện xử lý xong các thay đổi.</summary>
    /// <param name="count">Số frame cần chờ.</param>
    /// <returns>Coroutine chờ theo frame.</returns>
    static IEnumerator Frames(int count=3) { for(int i=0;i<count;i++) yield return null; }
    /// <summary>Chờ theo thời gian thực để kiểm tra cả khi game bị tạm dừng.</summary>
    /// <param name="seconds">Khoảng thời gian thực cần chờ, tính bằng giây.</param>
    /// <returns>Coroutine chờ không phụ thuộc Time.timeScale.</returns>
    static IEnumerator Real(double seconds) { double end=Time.realtimeSinceStartupAsDouble+seconds; while(Time.realtimeSinceStartupAsDouble<end) yield return null; }
    /// <summary>Kiểm tra raycast tại tâm nút rồi gửi sự kiện nhấn, thả và click như thao tác chuột.</summary>
    /// <param name="button">Nút giao diện cần mô phỏng click.</param>
    static void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rt=(RectTransform)button.transform;
        var canvas=button.GetComponentInParent<Canvas>();
        var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rt.TransformPoint(rt.rect.center));
        var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
        // Xác nhận nút thực sự nhận được chuột, tránh bỏ sót trường hợp bị UI khác che.
        Check(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==button,"Button is obstructed: "+button.name+" at "+point+" screen="+Screen.width+"x"+Screen.height+" hits="+string.Join(",",hits.Take(5).Select(h=>h.gameObject.name)));
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    /// <summary>Tạo một bản ghi mẫu hợp lệ với ID riêng và các giá trị phụ thuộc chỉ số.</summary>
    /// <param name="index">Chỉ số dùng để tạo dữ liệu mẫu khác nhau.</param>
    /// <returns>Bản ghi thử nghiệm mới.</returns>
    static GameHistoryRecord Record(int index=0)=>new GameHistoryRecord
    {
        id=Guid.NewGuid().ToString("N"),level=index%4+1,score=index*10,totalScore=index*20,hasTotalScore=true,
        playedSeconds=288+index,result=GameHistoryRecord.Completed,endedAt=DateTimeOffset.Now.AddSeconds(index).ToString("O",CultureInfo.InvariantCulture)
    };
    /// <summary>Kiểm tra lưu, nạp, chống trùng, giới hạn bản ghi, phục hồi và lỗi file trong thư mục tạm.</summary>
    /// <param name="folder">Thư mục riêng chứa các file kiểm tra.</param>
    static void StoreChecks(string folder)
    {
        Directory.CreateDirectory(folder); string path=Path.Combine(folder,"store-checks.json");
        var store=new GameHistoryStore(path); Check(store.GetHistory().Count==0&&!File.Exists(path),"Missing file must be an empty read-only load");
        var first=Record(); Check(store.AddHistory(first),"Add rejected"); Check(!store.AddHistory(first),"Duplicate ID added");
        var wrong=first.Copy(); wrong.result=GameHistoryRecord.GameOver; Check(!store.AddHistory(wrong),"Game Over replaced completed run");
        var copy=store.GetHistory()[0]; copy.score=99999; Check(store.GetHistory()[0].score!=99999,"Mutable snapshot leaked");
        for(int i=1;i<110;i++) store.AddHistory(Record(i));
        store=new GameHistoryStore(path); Check(store.GetHistory().Count==100&&store.GetHistory()[0].score==1090,"Limit, reload or ordering failed");
        Write("PASS storage: missing file, JSON reload, schema, copied snapshots, duplicate/outcome guard, newest-first, 100-record limit");
        string before=File.ReadAllText(path); var pending=Record(120);
        using(var locked=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
        { Check(store.AddHistory(pending)&&store.HasPendingChanges&&store.LastError!=null,"Locked write did not retain pending run"); }
        Check(File.ReadAllText(path)==before,"Failed save damaged original"); Check(store.SaveHistory()&&!store.HasPendingChanges,"Save retry failed");
        Write("PASS locked-file save failure: original intact, run retained in memory, retry persisted");
        string corrupt=Path.Combine(folder,"corrupt.json"); File.WriteAllText(corrupt,"{broken");
        var broken=new GameHistoryStore(corrupt); Check(broken.GetHistory().Count==0&&File.ReadAllText(corrupt)=="{broken","Corruption erased original");
        Check(broken.AddHistory(Record()),"Recovery add rejected"); Check(Directory.GetFiles(folder,"corrupt.json.unreadable-*").Any(p=>File.ReadAllText(p)=="{broken"),"Corrupt bytes not preserved");
        string empty=Path.Combine(folder,"empty.json"); File.WriteAllText(empty,""); Check(new GameHistoryStore(empty).GetHistory().Count==0&&File.ReadAllText(empty)=="","Empty file handling failed");
        string future=Path.Combine(folder,"future.json"); string futureJson="{\"schemaVersion\":99,\"records\":[]}"; File.WriteAllText(future,futureJson);
        var newer=new GameHistoryStore(future); newer.AddHistory(Record()); Check(newer.HasPendingChanges&&File.ReadAllText(future)==futureJson,"Future schema overwritten");
        Check(store.ClearHistory()&&new GameHistoryStore(path).GetHistory().Count==0,"Clear/reload failed");
        File.WriteAllText(path,"{broken again"); Check(new GameHistoryStore(path).GetHistory().Count==0,"Clear resurrected old records from backup");
        Write("PASS empty/corrupt/future files preserved; recovery archive; clear also empties recovery history");
    }
    /// <summary>Tải scene theo số màn, chờ khởi tạo rồi xác nhận đúng màn.</summary>
    /// <param name="level">Số màn cần tải để kiểm tra.</param>
    /// <returns>Coroutine chờ màn mới sẵn sàng.</returns>
    static IEnumerator Load(int level) { SceneManager.LoadScene("Level"+level); yield return Frames(); Check(GameManager.Instance.GetCurrentLevel()==level,"Wrong level"); }
    /// <summary>Kiểm tra ghi kết quả từng màn, loại thời gian tạm dừng, chơi lại, xác nhận xóa và danh sách cuộn.</summary>
    /// <returns>Coroutine chạy bộ tình huống của phiên Play Mode đầu.</returns>
    static IEnumerator Suite()
    {
        yield return Frames(); var manager=GameHistoryManager.Instance;
        var ui=One<GameHistoryUI>(); Click(Button("HistoryButton"));
        yield return Frames();
        Check(Field<GameObject>(ui,"panel").activeSelf&&Field<TMP_Text>(ui,"emptyText").gameObject.activeSelf,"History empty state not opened");
        Check(!Button("ClearHistoryButton").interactable,"Empty history clear should be disabled"); Click(Button("HistoryBackButton"));
        Write("PASS MainMenu HISTORY/BACK raycast clicks and empty state");
        PlayerPrefs.SetInt("HistoryTestSentinel",9876);
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetAllScore();
        int total=0;
        for(int level=1;level<=4;level++)
        {
            yield return Load(level); var game=GameManager.Instance; string id=Field<string>(game,"historyRunId");
            double begin=Time.realtimeSinceStartupAsDouble; int count=manager.GetHistory().Count;
            ScoreManager.Instance.AddScore(17); yield return Real(.18);
            game.PauseGame(); double pausedAt=Time.realtimeSinceStartupAsDouble; yield return Real(.6); double pause=Time.realtimeSinceStartupAsDouble-pausedAt;
            game.ResumeGame(); Check(Field<string>(game,"historyRunId")==id,"Resume replaced run ID"); yield return Real(.18);
            int target=game.GetRequiredScore(); ScoreManager.Instance.AddScore(target-17); total+=target;
            double end=Time.realtimeSinceStartupAsDouble;
            typeof(GameManager).GetMethod("LevelComplete",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
            typeof(GameManager).GetMethod("LevelComplete",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null); game.GameOver();
            Check(manager.GetHistory().Count==count+1,"Completion duplicated history");
            var row=manager.GetHistory().Single(r=>r.id==id);
            Check(row.level==level&&row.score==target&&row.totalScore==total&&row.hasTotalScore&&row.result==GameHistoryRecord.Completed,"Wrong completed data before score reset");
            Check(row.playedSeconds>=.3&&Math.Abs(row.playedSeconds-(end-begin-pause))<.25,"Pause included in real play time");
            var date=DateTimeOffset.Parse(row.endedAt,CultureInfo.InvariantCulture);
            Check(Math.Abs((DateTimeOffset.Now-date).TotalSeconds)<2&&date.Offset==DateTimeOffset.Now.Offset,"Machine date/offset incorrect");
            yield return Frames();
            Check(level==4?GameManager.Instance.GetGameState()==GameManager.GameState.LevelComplete:SceneManager.GetActiveScene().name=="Level"+(level+1)&&LevelManager.Instance.IsLevelUnlocked(level+1),"Completion/unlock flow changed");
            Write("PASS Level"+level+": one COMPLETED record, correct ID/level/local score/total/date; real duration excludes pause; completion/unlock preserved");
        }
        yield return Load(2); ScoreManager.Instance.AddScore(23); var losing=GameManager.Instance; string lossId=Field<string>(losing,"historyRunId");
        One<PlayerHealth>().TakeDamage(10000); losing.GameOver(); yield return Frames();
        Check(SceneManager.GetActiveScene().name=="EndGame"&&manager.GetHistory().Single(r=>r.id==lossId).score==23&&manager.GetHistory().Single(r=>r.id==lossId).result==GameHistoryRecord.GameOver,"Game Over history or transition failed");
        yield return Load(3); int previous=manager.GetHistory().Count; string abandoned=Field<string>(GameManager.Instance,"historyRunId");
        ScoreManager.Instance.AddScore(29); var pauseUI=One<PauseMenuUI>(); pauseUI.Pause(); yield return Frames(); Click(Button("RestartButton")); yield return Frames();
        string restarted=Field<string>(GameManager.Instance,"historyRunId"); Check(abandoned!=restarted&&manager.GetHistory().Count==previous&&ScoreManager.Instance.GetLevelScore()==0,"Restart mixed runs or recorded abandonment");
        ScoreManager.Instance.AddScore(7); GameManager.Instance.GameOver(); yield return Frames(); Check(manager.GetHistory().Single(r=>r.id==restarted).score==7,"Restart score mixed with old run");
        yield return Load(1); previous=manager.GetHistory().Count; pauseUI=One<PauseMenuUI>(); pauseUI.Pause(); yield return Frames(); Click(Button("MainMenuButton")); yield return Frames();
        Check(SceneManager.GetActiveScene().name=="MainMenu"&&Time.timeScale>0&&manager.GetHistory().Count==previous,"Pause/MainMenu saved unfinished run");
        Write("PASS GAME OVER once, EndGame, independent Restart IDs/scores, Resume ID, abandoned Pause/MainMenu ignored");
        ui=One<GameHistoryUI>(); Click(Button("HistoryButton")); yield return Frames();
        Check(Field<RectTransform>(ui,"content").GetComponentsInChildren<GameHistoryRowUI>().Length==manager.GetHistory().Count,"Wrong history row count");
        int unlocked=PlayerPrefs.GetInt("UnlockedLevel"); string ship=PlayerPrefs.GetString("SelectedPlayer");
        ui.DeleteHistory(); Check(manager.GetHistory().Count==previous,"Unconfirmed delete worked");
        Click(Button("ClearHistoryButton")); Check(Field<GameObject>(ui,"confirmation").activeSelf,"Confirmation missing"); yield return Frames(); Click(Button("CancelHistoryDeleteButton")); Check(manager.GetHistory().Count==previous,"Cancel deleted history");
        yield return Frames(); Click(Button("ClearHistoryButton")); yield return Frames(); Click(Button("ConfirmHistoryDeleteButton"));
        Check(manager.GetHistory().Count==0&&new GameHistoryStore(manager.FilePath).GetHistory().Count==0,"Confirmed clear did not persist");
        Check(PlayerPrefs.GetInt("UnlockedLevel")==unlocked&&PlayerPrefs.GetString("SelectedPlayer")==ship&&PlayerPrefs.GetInt("HistoryTestSentinel")==9876,"History clear changed PlayerPrefs");
        var isolatedStore=Field<GameHistoryStore>(manager,"store");
        for(int i=0;i<105;i++) isolatedStore.AddHistory(Record(i));
        manager.LoadHistory();
        yield return Frames(); Canvas.ForceUpdateCanvases(); var content=Field<RectTransform>(ui,"content"); var scroll=Field<ScrollRect>(ui,"scrollRect");
        Check(manager.GetHistory().Count==100&&content.childCount==100&&content.rect.height>scroll.viewport.rect.height,"Scroll list limit/layout failed");
        float start=content.anchoredPosition.y; scroll.verticalNormalizedPosition=0; Canvas.ForceUpdateCanvases(); Check(content.anchoredPosition.y>start,"Scroll did not move");
        foreach(var text in Field<GameObject>(ui,"panel").GetComponentsInChildren<TMP_Text>(false)) { text.ForceMeshUpdate(); Check(!text.isTextOverflowing,"History text overflow: "+text.name); }
        foreach(var transform in ui.GetComponentsInChildren<Transform>(true)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"Missing history script");
        Write("PASS confirmed DELETE/CANCEL, disk clear, preserved unlock/ship/unrelated prefs; 100 rows, scrolling, 9:16 text fit, no missing history scripts");
        manager.ClearHistory(); var saved=Record(1); manager.AddHistory(saved); SessionState.SetString("SpaceExplorer.HistoryTests.SavedId",saved.id);
        Click(Button("HistoryBackButton"));
    }
    /// <summary>Xác nhận lịch sử còn sau khi khởi chạy lại và file lịch sử thật của người chơi không bị đổi.</summary>
    /// <returns>Coroutine kiểm tra phiên Play Mode thứ hai.</returns>
    static IEnumerator RelaunchSuite()
    {
        yield return Frames(); var manager=GameHistoryManager.Instance; string id=SessionState.GetString("SpaceExplorer.HistoryTests.SavedId","");
        Check(manager.GetHistory().Count==1&&manager.GetHistory()[0].id==id,"History did not survive runtime shutdown/relaunch");
        Click(Button("HistoryButton")); yield return Frames(); Check(One<GameHistoryRowUI>().GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="SCORE: 10"),"Relaunched UI data incorrect");
        var b=ReadBackup(); Check(File.Exists(b.realHistoryPath)==b.realFileExists&&(!b.realFileExists||Convert.ToBase64String(File.ReadAllBytes(b.realHistoryPath))==b.realHistoryBytes),"Tests touched real player history");
        Write("PASS second independent Play Mode session loaded JSON into fresh manager and UI; real player history untouched");
    }
    /// <summary>Tạo ảnh dọc cho menu, lịch sử rỗng, có dữ liệu và xác nhận xóa bằng các preview scene tạm.</summary>
    [MenuItem("Tools/Space Explorer/Render Game History Preview")]
    public static void RenderPreview()
    {
        Directory.CreateDirectory(Work+"/renders");
        foreach(string mode in new[]{"menu","empty","populated","confirmation"})
        {
            var scene=EditorSceneManager.OpenPreviewScene(Main);
            try
            {
                var roots=scene.GetRootGameObjects(); var ui=roots.SelectMany(r=>r.GetComponentsInChildren<GameHistoryUI>(true)).Single();
                if(mode!="menu")
                {
                    var so=new SerializedObject(ui); var elements=so.FindProperty("menuElements"); for(int i=0;i<elements.arraySize;i++) ((GameObject)elements.GetArrayElementAtIndex(i).objectReferenceValue).SetActive(false);
                    Field<GameObject>(ui,"panel").SetActive(true); Field<GameObject>(ui,"confirmation").SetActive(mode=="confirmation");
                    Field<TMP_Text>(ui,"emptyText").gameObject.SetActive(mode=="empty");
                    if(mode!="empty") for(int i=0;i<8;i++)
                    {
                        var row=Object.Instantiate(Field<GameHistoryRowUI>(ui,"rowPrefab"),Field<RectTransform>(ui,"content"),false);
                        var entry=Record(i); entry.level=4-i%4; entry.score=(4-i%4)*100; entry.playedSeconds=288-i*12; entry.result=i%3==0?GameHistoryRecord.GameOver:GameHistoryRecord.Completed; row.Bind(entry);
                    }
                }
                var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First();
                var target=RenderTexture.GetTemporary(1080,1920,24); camera.targetTexture=target; camera.aspect=9f/16; camera.scene=scene;
                foreach(var canvas in roots.SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).Where(c=>c.isRootCanvas))
                { canvas.GetComponent<CanvasScaler>().enabled=false; canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1; canvas.scaleFactor=1; }
                Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(Field<RectTransform>(ui,"content"));
                foreach(var text in roots.SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true))) text.ForceMeshUpdate(true,true);
                Canvas.ForceUpdateCanvases(); camera.Render(); var old=RenderTexture.active; RenderTexture.active=target;
                var picture=new Texture2D(1080,1920,TextureFormat.RGB24,false); picture.ReadPixels(new Rect(0,0,1080,1920),0,0); picture.Apply();
                File.WriteAllBytes(Work+"/renders/"+mode+".png",picture.EncodeToPNG()); Object.DestroyImmediate(picture); RenderTexture.active=old; camera.targetTexture=null; RenderTexture.ReleaseTemporary(target);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
