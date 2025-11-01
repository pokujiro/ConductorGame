using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private string path;  // 保存ファイルのパス
    public ScoreDataList data = new ScoreDataList();

    void Awake()
    {
        // 保存先のパスを決める（どの環境でもOKなUnity標準フォルダ）
        path = Application.persistentDataPath + "/scoredata.json";
        Load();  // 最初に読込
    }

    // スコア追加
    public void AddScore(string user, string music, string difficulty, int score)
    {
        // ...（既存コード）
        Debug.Log($"[ScoreManager] AddScore: {user}, {music}, {difficulty}, {score}");
        ScoreEntry entry = new ScoreEntry
        {
            userName = user,
            musicName = music,
            difficulty = difficulty,
            score = score,
            date = System.DateTime.Now.ToString("yyyy-MM-dd")
        };
        data.scores.Add(entry);
        Save();
    }

    // 保存
    public void Save()
    {
        string json = JsonUtility.ToJson(data, true);
        Debug.Log("=== Score JSON Saved ===\n" + json); // ★ここを追加！
        File.WriteAllText(path, json);
    }

    // 読込
    public void Load()
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            Debug.Log("=== Score JSON Loaded ===\n" + json); // ★ここを追加！
            data = JsonUtility.FromJson<ScoreDataList>(json);
        }
        else
        {
            data = new ScoreDataList();
            Debug.Log("No scoredata.json found. Initialized new data.");
        }
    }

    // ランキングを取得（例：曲・難易度で絞り込み、上位10件）
    public List<ScoreEntry> GetRanking(string music, string difficulty, int topN = 10)
    {
        var filtered = data.scores.FindAll(
            x => x.musicName == music && x.difficulty == difficulty
        );
        filtered.Sort((a, b) => b.score.CompareTo(a.score)); // 降順
        return filtered.GetRange(0, Mathf.Min(topN, filtered.Count));
    }
}
