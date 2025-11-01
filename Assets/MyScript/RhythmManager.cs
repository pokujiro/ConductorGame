using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public enum VolumeLevel
{
    Small,
    Medium,
    Large
}
public enum NoteValue
{
    Half,       // 2分音符
    Quarter,    // 4分音符
    Eighth,     // 8分音符
    Sixteenth   // 16分音符
}

public enum MusicType
{
    JingleBells,
    Beethoven,
    // 追加したい曲をここに
}

public enum DifficultyType
{
    Easy,
    Normal,
    Hard
}



public class BeatInfo
{
    public float time;                 // このビート（拍）が来る理想のタイミング
    public bool judged = false;        // 既に判定済みか
    public bool isActive = false;      // 判定ウィンドウ内か
    public VolumeLevel volume;  // 音量レベル：大中小
}

[System.Serializable]
public class RhythmNote
{
    public NoteValue noteValue = NoteValue.Quarter;
    public VolumeLevel volume = VolumeLevel.Medium;
}

// createから作れる
[CreateAssetMenu(menuName = "RhythmGame/RhythmPatternData")]
public class RhythmPatternData : ScriptableObject
{
    public MusicType musicType;
    public DifficultyType difficultyType;
    public float bpm;
    public float songDuration;
    public List<RhythmNote> notes = new List<RhythmNote>();
}

public class RhythmManager : MonoBehaviour
{
    [Header("曲パターンデータリスト")]
    public List<RhythmPatternData> allPatternData; // ScriptableObjectリスト（Inspectorで割当）

    // RhythmManager.cs
    [Header("GameController")]
    public GameController gameController; // Inspectorで割当

    [Header("リズムパラメータ")]
    public float bpm = 130f;
    public float songDuration = 30f;
    public float windowSize = 0.3f; //秒 有効幅 
    public float noteTravelTime = 2.0f; //秒 ノーツ出現→判定まで何秒かけて動くか
    public float perfectDiff = 0.03f; //秒 perfectの閾値
    public float goodDiff = 0.15f; //秒 goodの閾値
                                   // RhythmManager内

    [Header("UI配置")]
    public RectTransform canvasParent; // World Space CanvasのRectTransform
    public GameObject noteUIPrefab;    // 丸ノーツ用Imageプレハブ
    public GameObject judgeLineUIPrefab;   // 判定ライン用Imageプレハブ（細長いRectTransform）
    public GameObject judgeCircleUIPrefab; // 判定丸用Imageプレハブ（円形）
    public GameObject laneBGPrefab; // Inspectorで帯ImageのPrefab
    public GameObject perfectZonePrefab; // 細長いImage（帯）Prefab
    public GameObject goodZonePrefab;    // 同上
    public GameObject niceZonePrefab; // さらに太い帯（Image）Prefab

    private GameObject judgeLineUI;
    private GameObject judgeCircleUI;
    private GameObject perfectZoneUI;
    private GameObject goodZoneUI;
    private GameObject niceZoneUI;

    [Header("ノーツ流れる範囲(UI座標)")]
    public float startX_UI = 2.0f;   // ノーツ出現位置（Canvas上X座標）
    public float judgeX_UI = 0f;     // 判定ライン位置（Canvas上X座標）
    public float y_UI = 0f;          // ノーツ・ライン・丸のY位置（Canvas上）

    [Header("BGM")]
    public AudioSource audioSource;      // InspectorでBGMのAudioSourceをセット
    public float startVolume = 0.5f;

    [Header("確認用(曲の拍タイミングの音源)")]
    public AudioSource metronomeAudioSource; // 拍音用

    [Header("同期のための遅延時間")]
    public float SyncDelay = 0f;

    private int nextBeatIndex = 0;

    private List<BeatInfo> beatList = new List<BeatInfo>();
    private List<GameObject> noteUIList = new List<GameObject>();



    private bool isGameStarted = false; // ゲームフラグ
    private bool isStartStarted = false; // スタートからGameまでの時間
    private float startTime = 0f;

    [Header("カウントダウンUI")]
    public int countDown = 3;
    public float startToStartTime = 0.7f;
    public GameObject countdownTextObj; // InspectorでCountdownTextを割り当て
    private TextMeshProUGUI countdownText;
    private float startBeforeStart = 0f;

    [Header("エフェクトUI")]
    public GameObject judgementTextPrefab; // Inspectorで割り当て
    public GameObject totalScoreTextObj; // InspectorでTextを割り当て
    private TextMeshProUGUI totalScoreText;

    [Header("デバック")]
    public bool isShowNoteNomber = false;

    [Header("パターン読込方式")]
    public bool useInspectorPattern = true;

    [Header("Inspectorで編集（リズム＋音量セット）")]
    public float inspectorBpm = 130f;
    public float inspectorSongDuration = 30f;
    public List<RhythmNote> rhythmPattern = new List<RhythmNote>();

    // ↓ 選択された曲・難易度名をGameControllerからセット
    public MusicType selectedMusic;
    public DifficultyType selectedDifficulty;

    // 結果
    private int totalScore = 0;
    private int perfectCount = 0, goodCount = 0, niceCount = 0, missCount = 0;
    private int perfactScore = 100;
    private int goodScore = 70;
    private int niceScore = 50;
    // private int missScore = 0;
    private int sameVolumeScore = 50;
    private int diffVolumeScore = 20;

    void Start()
    {
        // 2. 判定ラインUI設置
        judgeLineUI = Instantiate(judgeLineUIPrefab, canvasParent);
        judgeLineUI.GetComponent<RectTransform>().anchoredPosition = new Vector2(judgeX_UI, y_UI);
        judgeLineUI.SetActive(true);

        // 3. 判定用丸UI設置
        judgeCircleUI = Instantiate(judgeCircleUIPrefab, canvasParent);
        judgeCircleUI.GetComponent<RectTransform>().anchoredPosition = new Vector2(judgeX_UI, y_UI);
        judgeCircleUI.SetActive(true);


        // 判定帯の可視化
        float speedPerSec = Mathf.Abs(startX_UI - judgeX_UI) / noteTravelTime;

        // Perfect帯
        perfectZoneUI = Instantiate(perfectZonePrefab, canvasParent);
        perfectZoneUI.GetComponent<RectTransform>().anchoredPosition = new Vector2(judgeX_UI, y_UI);
        float perfectWidth = perfectDiff * 2f * speedPerSec;
        perfectZoneUI.GetComponent<RectTransform>().sizeDelta = new Vector2(perfectWidth, 0.08f);
        perfectZoneUI.GetComponent<Image>().color = new Color(1f, 1f, 0f, 0.45f); // 黄色半透明

        // Good帯
        goodZoneUI = Instantiate(goodZonePrefab, canvasParent);
        goodZoneUI.GetComponent<RectTransform>().anchoredPosition = new Vector2(judgeX_UI, y_UI);
        float goodWidth = goodDiff * 2f * speedPerSec;
        goodZoneUI.GetComponent<RectTransform>().sizeDelta = new Vector2(goodWidth, 0.08f);
        goodZoneUI.GetComponent<Image>().color = new Color(0f, 1f, 1f, 0.25f); // 水色半透明

        // 判定ウィンドウ(Nice)帯
        niceZoneUI = Instantiate(niceZonePrefab, canvasParent);
        niceZoneUI.GetComponent<RectTransform>().anchoredPosition = new Vector2(judgeX_UI, y_UI);
        float windowWidth = windowSize * 2f * speedPerSec;
        niceZoneUI.GetComponent<RectTransform>().sizeDelta = new Vector2(windowWidth, 0.08f);
        niceZoneUI.GetComponent<Image>().color = new Color(1f, 0f, 0f, 0.15f); // 例：赤みがかった半透明

        // Total Score の初期表示
        totalScoreText = totalScoreTextObj.GetComponent<TextMeshProUGUI>();
        UpdateTotalScoreUI();

    }

    public void StartGame()
    {
        // 初期化を行う
        InitRhythm();
        // カウントダウン前に現在時刻記録
        startTime = Time.time;
        isStartStarted = true;
        StartCoroutine(CountdownAndStart());
    }

    IEnumerator CountdownAndStart()
    {
        for (int i = (int)countDown; i >= 1; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "Start!";
        yield return new WaitForSeconds(startToStartTime);
        // ゲームを実際に開始
        audioSource.time = SyncDelay;  // SyncDelay秒後から再生開始
        audioSource.volume = startVolume;
        isGameStarted = true;
        isStartStarted = false;
        audioSource.Play();
        startTime = Time.time; // スタートタイムの記録→onConductで使用
        // 必要ならstartTime = Time.time;など
    }

    void Update()
    {
        float interval = 60f / bpm;

        float timeFromStart = 0;

        // ★★★ここでtimeFromStartを工夫★★★
        if (isGameStarted)
        {
            timeFromStart = audioSource.time;
        }
        else if (isStartStarted)
        {
            // カウントダウン中も「スタート予定時刻からの経過時間」でノーツ動かす
            startBeforeStart += Time.deltaTime;
            timeFromStart = startBeforeStart;
        }
        else
        {
            return; // まだ何も始まっていない（ゲーム待機中）は何もしない
        }


        int firstIndex = nextBeatIndex; // ++されても現在のインデックスを保持するため待避

        // --- 判定ロジックはnextBeatIndexのみ ---
        if (nextBeatIndex < beatList.Count)
        {
            var beat = beatList[nextBeatIndex];

            // 判定ウィンドウに入ったら isActive = true
            if (!beat.judged && !beat.isActive && timeFromStart >= beat.time - windowSize)
                beat.isActive = true;

            // 判定ウィンドウを過ぎたら isActive = false、Miss処理
            if (!beat.judged && beat.isActive && timeFromStart > beat.time + windowSize)
            {
                beat.isActive = false;
                beat.judged = true;
                Debug.Log("Miss!（時間切れ）");
                nextBeatIndex++;  // 次のリズムへ
                missCount++; // ミスカウント
            }
            // 判定済みなら次のビートへ進める（OnConductで判定された場合も対応）
            if (beat.judged && beat.isActive)
            {
                beat.isActive = false;
                nextBeatIndex++;  // 次のリズムへ
            }
        }

        int lastIndex = firstIndex;
        float sumTime = 0f;
        while (lastIndex < beatList.Count && sumTime <= noteTravelTime)
        {
            sumTime = beatList[lastIndex].time - timeFromStart;
            lastIndex++;  // 最後の次を呼び出す危険性がある。
            //Debug.Log("sumtime" + sumTime);
        }

        // --- ノーツのUI表示・移動 ---
        for (int i = 0; i < noteUIList.Count; i++)
        {
            var beat = beatList[i];
            var note = noteUIList[i];
            float timeToJudge = beat.time - timeFromStart; // timeToJudge ... 判定する時間までの残り時間

            // ノーツを表示すべき期間のみアクティブ　（判定されていない、時間オーバーになっていない、表示時間以内である）
            if (beat.judged || timeToJudge <= -windowSize || timeToJudge > noteTravelTime)
            {
                note.SetActive(false);
                continue;
            }
            note.SetActive(true);

            // ノーツの動き（流す処理）
            float t = 1.0f - (timeToJudge / noteTravelTime); // 1:判定時間　0:判定前表示時間
            float x = Mathf.LerpUnclamped(startX_UI, judgeX_UI, t); // １を超えた分も反映
            note.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y_UI);

            // 色や大きさはImageのcolorやRectTransform.sizeDeltaで制御（必要なら）
            var img = note.GetComponent<Image>();
            if (i == nextBeatIndex && beat.isActive)
                img.color = Color.yellow;
            else if (beat.judged)
                img.color = Color.white;
            else
                img.color = Color.gray;

            // ノーツ描画部分で
            img.color = GetNoteColor(beat.volume);
        }

        // --- ゲーム終了チェック ---
        if (isGameStarted && nextBeatIndex >= beatList.Count)
        {
            isGameStarted = false; // 一度だけ通る
            StartCoroutine(GameEndAfterDelay(3.0f));
        }

        // 拍ごとに効果音を鳴らす
        //int nowBeatIndex = Mathf.FloorToInt(timeFromStart / interval);
        //if (nowBeatIndex > lastBeatSoundIndex && nowBeatIndex < beatList.Count)
        //{
        //    metronomeAudioSource.Play();
        //    lastBeatSoundIndex = nowBeatIndex;
        //}
    }

    // 指揮棒を振った時（拍入力）の判定
    public void OnConduct(float conductTime, float resultVolume)
    {

        if (nextBeatIndex < beatList.Count)
        {
            var beat = beatList[nextBeatIndex];
            if (beat.isActive && !beat.judged) // 判定時間であり、まだ判定されていないもの
            {
                float timeFromStart = conductTime - startTime; // timeFromStart ...　この時は　振ったタイミングの
                float diff = Mathf.Abs(beat.time - timeFromStart); //誤差の計算
                beat.judged = true; // 判定済み！
                VolumeLevel volumeLevel = ConvertSwingToVolumeLevel(resultVolume);

                int timingScore = 0;
                string judgeText = "Miss";

                if (diff < perfectDiff) { timingScore = perfactScore; perfectCount++; judgeText = "Perfect"; }
                else if (diff < goodDiff) { timingScore = goodScore; goodCount++; judgeText = "Good"; }
                else { timingScore = niceScore; niceCount++; judgeText = "Nice"; }

                // 音量一致スコア
                int volumeScore = 0;
                int volDiff = Mathf.Abs((int)beat.volume - (int)volumeLevel);
                if (volDiff == 0) volumeScore = sameVolumeScore;
                else if (volDiff == 1) volumeScore = diffVolumeScore;

                totalScore += timingScore + volumeScore;
                UpdateTotalScoreUI(); // 総合スコアに加算

                //Debug.Log($"{judgeText}! (Timing {timingScore} + Volume {volumeScore}) 総合: {totalScore}");

                ShowJudgementText($"{judgeText}! +{timingScore + volumeScore}", judgementTextPrefab.GetComponent<RectTransform>().anchoredPosition);
                return;
            }
        }
        Debug.Log("Miss!");
    }

    // 音量から大中小を返す関数
    public static VolumeLevel ConvertSwingToVolumeLevel(float musicVolume)
    {
        if (musicVolume > 0.6f) return VolumeLevel.Large;
        if (musicVolume > 0.25f) return VolumeLevel.Medium;
        return VolumeLevel.Small;
    }

    // Noteの色返す関数
    Color GetNoteColor(VolumeLevel level)
    {
        switch (level)
        {
            case VolumeLevel.Large: return Color.red;
            case VolumeLevel.Medium: return Color.green;
            case VolumeLevel.Small: return Color.blue;
            default: return Color.white;
        }
    }

    void ShowJudgementText(string result, Vector2 position)
    {
        var obj = Instantiate(judgementTextPrefab, canvasParent);
        obj.GetComponent<TextMeshProUGUI>().text = result;
        obj.GetComponent<RectTransform>().anchoredPosition = position;
        Destroy(obj, 0.4f); // 0.8秒後に自動消滅
    }
    // スコア更新
    void UpdateTotalScoreUI()
    {
        if (totalScoreText != null)
            totalScoreText.text = "Score: " + totalScore;
    }

    void LoadPattern()
    {
        if (useInspectorPattern)
        {
            bpm = inspectorBpm;
            songDuration = inspectorSongDuration;
            // rhythmPatternはInspectorで指定済み
        }
        else
        {
            // 曲・難易度にマッチしたRhythmPatternDataをロード
            var data = allPatternData.Find(x =>
                x.musicType == selectedMusic && x.difficultyType == selectedDifficulty);

            if (data != null)
            {
                bpm = data.bpm;
                songDuration = data.songDuration;
                rhythmPattern = new List<RhythmNote>(data.notes); // ディープコピー（必要なら）
            }
            else
            {
                Debug.LogWarning("RhythmPatternDataが見つかりませんでした！");
            }
        }
    }

    // SelectedPanelで選択後　代入するための関数
    public void SetSelectedMusicAndDifficulty(string musicStr, string difficultyStr)
    {
        musicStr = musicStr.Replace(" ", ""); // 空白を削除
        // 文字列からenumへ変換
        if (System.Enum.TryParse<MusicType>(musicStr, out var musicResult))
            selectedMusic = musicResult;
        else
            Debug.LogWarning("MusicTypeの変換に失敗: " + musicStr);

        if (System.Enum.TryParse<DifficultyType>(difficultyStr, out var diffResult))
            selectedDifficulty = diffResult;
        else
            Debug.LogWarning("DifficultyTypeの変換に失敗: " + difficultyStr);
    }

    public void InitRhythm()
    {

        // === ① 既存ノーツ・レーンの削除 ===
        foreach (var obj in noteUIList)
            if (obj != null) Destroy(obj);
        noteUIList.Clear();

        // === ② インデックス・スコア・フラグのリセット ===
        nextBeatIndex = 0;
        totalScore = 0;
        perfectCount = goodCount = niceCount = missCount = 0;
        isGameStarted = false; isStartStarted = false;

        UpdateTotalScoreUI();
        // 曲、難易度が選択されていることが前提
        // bpmなどをロード
        LoadPattern();

        totalScore = 0;
        UpdateTotalScoreUI();

        float baseInterval = 60f / bpm; // 4分音符=60/BPM秒
        startBeforeStart = -((float)countDown + startToStartTime); // カウントダウン中のノーツの表示用→loop
        countdownText = countdownTextObj.GetComponent<TextMeshProUGUI>();

        // 1. ノーツリスト・UI生成
        beatList.Clear();
        noteUIList.Clear();

        // 背景レーンUI生成 例あの関係最初に作成
        var laneBG = Instantiate(laneBGPrefab, canvasParent);
        var rt = laneBG.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2((startX_UI + judgeX_UI) / 2f, y_UI); // 中心は幅の真ん中
        rt.sizeDelta = new Vector2(Mathf.Abs(startX_UI - judgeX_UI) + 0.05f, 0.05f); // 幅＋余白, 高さ40
        laneBG.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f); // 半透明黒

        // テンポリストの作成
        int noteIndex = 0;
        float curTime = 0f;
        foreach (var rn in rhythmPattern)
        {
            float interval = baseInterval;
            switch (rn.noteValue)
            {
                case NoteValue.Half: interval = baseInterval * 2f; break;
                case NoteValue.Quarter: interval = baseInterval; break;
                case NoteValue.Eighth: interval = baseInterval / 2f; break;
                case NoteValue.Sixteenth: interval = baseInterval / 4f; break;
            }
            beatList.Add(new BeatInfo { time = curTime, volume = rn.volume });

            // ノーツ生成等…
            var note = Instantiate(noteUIPrefab, canvasParent);
            note.SetActive(false);

            // 子オブジェクトのTextMeshProUGUIを探して制御
            // Text (TMP) の取得
            var textTMP = note.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (isShowNoteNomber)
            {
                textTMP.text = (noteIndex).ToString(); // ノート番号
                textTMP.enabled = true;
            }
            else
            {
                textTMP.text = "";
                textTMP.enabled = false;
            }
            noteUIList.Add(note);


            curTime += interval;
            if (curTime > songDuration) break;
            noteIndex++;

        }
    }

    IEnumerator GameEndAfterDelay(float delaySeconds)
    {
        Debug.Log("ゲーム終了！5秒後にリザルト画面へ…");
        yield return new WaitForSeconds(delaySeconds);

        // ここでリザルト画面表示やGameControllerへの通知を行う
        gameController.OnGameFinished(totalScore);
    }
}
