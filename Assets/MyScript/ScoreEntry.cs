using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class ScoreEntry
{
    public string userName;     // ユーザー名
    public string musicName;    // 曲名
    public string difficulty;   // 難易度
    public int score;           // スコア
    public string date;         // 日付（例："2024-08-04"）
}

[Serializable]
public class ScoreDataList
{
    public List<ScoreEntry> scores = new List<ScoreEntry>();
}