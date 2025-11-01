using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OpenCover.Framework.Model;

public class GameController : MonoBehaviour
{
    [Header("各canvas")]
    public GameObject userNamePanel; // ユーザー名　入力画面
    public GameObject selectPanel; // 曲 + 難易度　選択画面
    public GameObject gamePanel; // 実際のゲーム画面 (RhythmManager) 
    public GameObject resultPanel; // 結果・ランキング表示画面
    public TextMeshProUGUI rankingText;
    public Button selectDicisionButton; // Inspectorでセット

    public TMP_InputField userNameInput; // userNamePanel の　インプット欄
    public TMP_Text greetingText;
    public TMP_Dropdown musicDropdown; // 曲のドロップダウン
    private string currentUserName = "";

    [Header("連携")]
    public RhythmManager rhythmManager; // rhythmManagerに選択した値を渡すため。
    public ScoreManager scoreManager; //　ランキング表示のため

    [Header("曲リスト　難易度リスト")]
    public List<string> musicList = new List<string> { "Jingle Bells", "Beethoven" };
    public List<string> difficultyList = new List<string> { "Easy", "Normal", "Hard" };

    private string selectedMusic = "";
    private string selectedDifficulty = "";



    // Start is called before the first frame update
    void Start()
    {
        // ドロップダウンに曲リストをセット（動的追加例）
        musicDropdown.ClearOptions();
        musicDropdown.AddOptions(musicList);

        // 初期選択
        selectedMusic = musicList[musicDropdown.value];
        musicDropdown.onValueChanged.AddListener(OnMusicDropdownChanged);

        // ユーザー名　入力画面　だけを表示
        ShowUserNamePanel();
    }

    // ドロップダウン選択時の処理
    void OnMusicDropdownChanged(int index)
    {
        selectedMusic = musicList[index];
        Debug.Log("選択された曲: " + selectedMusic);

        // 必要に応じてボタンの有効化など
        UpdateStartButtonInteractable();
        ShowRanking();
    }

    public void OnUserNameEntered()
    {
        currentUserName = userNameInput.text;
        PlayerPrefs.SetString("UserName", currentUserName); // 永続保存
        greetingText.text = "Welcome, " + currentUserName + "!";
        ShowSelectPanel();
    }

    public void OnMusicSelected(string music)
    {
        selectedMusic = music;
        UpdateStartButtonInteractable();
        // 曲のボタンにこの関数を割り当て
    }

    public void OnDifficultySelected(string diff)
    {
        selectedDifficulty = diff;
        UpdateStartButtonInteractable();
        ShowRanking();
    }

    void UpdateStartButtonInteractable()
    {
        // どちらも未選択でなければtrue
        selectDicisionButton.interactable = (!string.IsNullOrEmpty(selectedMusic) && !string.IsNullOrEmpty(selectedDifficulty));
    }

    public void OnStartButtonPressed()
    {
        if (!string.IsNullOrEmpty(selectedMusic) && !string.IsNullOrEmpty(selectedDifficulty))
        {
            // ここで実際の処理
            Debug.Log("Show Start Panel: " + selectedMusic + ", " + selectedDifficulty);
            StartGame(); // ゲーム画面へ移行
        }
    }

    void StartGame()
    {
        // rhythmManagerへ変数を代入
        rhythmManager.SetSelectedMusicAndDifficulty(selectedMusic, selectedDifficulty);
        // 曲データと難易度をRhythmManagerにセットして開始
        // RhythmManager.SetMusic(selectedMusic, selectedDifficulty);
        ShowGamePanel();
    }

    public void OnGameFinished(int score)
    {
        // スコア保存・ランキング処理
        SaveScore(currentUserName, score);
        ShowResultPanel();
    }

    void SaveScore(string user, int score)
    {
        FindObjectOfType<ScoreManager>().AddScore(user, selectedMusic, selectedDifficulty, score);
        // 日付とユーザーで保存など（PlayerPrefsやJSON/DBに拡張可能）
        // 例：PlayerPrefs.SetInt("score_" + user + "_" + DateTime.Today, score);
    }

    void ShowRanking()
    {
        var ranking = scoreManager.GetRanking(selectedMusic, selectedDifficulty);
        string text = "";
        int rank = 1;
        // ★デバッグ出力
        Debug.Log($"ランキング {selectedMusic}-{selectedDifficulty}: {ranking.Count}件");
        foreach (var entry in ranking)
        {
            text += $"{rank}. {entry.userName} : {entry.score} ({entry.date})\n";
            rank++;
        }
        rankingText.text = text;
    }


    // 画面切り替え関数
    void ShowUserNamePanel()
    {
        userNamePanel.SetActive(true);
        selectPanel.SetActive(false);
        gamePanel.SetActive(false);
        resultPanel.SetActive(false);
    }
    public void ShowSelectPanel()
    {
        Debug.Log("SelectPanel");
        userNamePanel.SetActive(false);
        selectPanel.SetActive(true);
        gamePanel.SetActive(false);
        resultPanel.SetActive(true);
    }
    void ShowGamePanel()
    {
        userNamePanel.SetActive(false);
        selectPanel.SetActive(false);
        gamePanel.SetActive(true);
        resultPanel.SetActive(false);
    }
    void ShowResultPanel()
    {
        ShowRanking();
        userNamePanel.SetActive(false);
        selectPanel.SetActive(false);
        gamePanel.SetActive(false);
        resultPanel.SetActive(true);
    }
}