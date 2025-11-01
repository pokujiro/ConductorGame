# 🎼 VR Conductor - Rhythm Training in Virtual Reality

**VR Conductor** は、  
指揮棒の動きで音楽のテンポ・音量をコントロールしながら演奏体験ができる  
**没入型リズムトレーニングゲーム** です。  
Meta Quest 3 + Unity + OpenXR を使用して開発しています。

---

##  概要

本プロジェクトでは、プレイヤーがVR空間で指揮者となり、  
**拍のタイミングと指揮棒のふり幅（音量）**をもとにスコアを算出します。  

指揮精度・音量再現度に応じてリアルタイムにスコアリングされ、  
ゲーム終了後にランキングやリザルトが表示されます。

---

##  開発環境

| 項目 | 内容 |
|------|------|
| Unity | 2022.3.x (LTS) |
| Platform | Meta Quest 3 (Android Build) |
| XR SDK | OpenXR + Meta XR Plugin |
| Language | C# |
| Audio | Oculus/Meta Spatializer |
| バージョン管理 | Git / GitHub |
| データ保存 | JSON (Playerごとのスコア履歴) |

---

## 🎮 ゲームの流れ

1. **ユーザー名入力**  
   VR内で名前を入力。過去スコアもユーザーごとに保存されます。  

2. **曲と難易度の選択**  
   - 曲（例：Jingle Bells / Beethoven）  
   - 難易度（Easy / Normal / Hard）  
   - Dropdownまたはボタンで選択  

3. **カウントダウン → ゲーム開始**  
   - 拍に合わせて指揮棒を振る  
   - 振り速度で音量がリアルタイムに変化（`BatonDynamicVolume.cs`）  

4. **判定 & スコア表示**  
   - Perfect / Good / Nice / Miss  
   - 音量一致でもスコアが加点  

5. **リザルト画面へ**  
   - 合計スコア表示  
   - JSONファイルに保存  
   - ランキング表示（曲・難易度ごと）

---

##  主要スクリプト構成

```

Assets/
├── Script/
│   ├── GameController.cs       # 各パネル制御（ユーザー名、選曲、ゲーム、結果）
│   ├── RhythmManager.cs        # リズムロジック・スコア判定
│   ├── BatonMotionLogger.cs    # 指揮棒の動きを解析
│   ├── BatonDynamicVolume.cs   # ふり幅→音量変換処理
│   ├── ScoreManager.cs         # JSON形式でスコア保存・読み込み
│   └── RhythmPatternData.cs    # ScriptableObjectで曲データを管理
│
├── Prefabs/
│   ├── Note.prefab             # ノーツUI
│   ├── JudgeLine.prefab        # 判定ライン
│   ├── VolumeBar.prefab        # 音量可視化バー
│   └── Panels/                 # 各Canvas画面
│
└── Resources/
└── RhythmPatternData/      # 曲・難易度別データ

````

---

##  データ構造例

###  スコアデータ (`scoredata.json`)

```json
{
  "scores": [
    {
      "userName": "Taro",
      "musicName": "Jingle Bells",
      "difficulty": "Easy",
      "score": 4070,
      "date": "2025-08-04"
    }
  ]
}
````

###  RhythmPatternData (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "RhythmGame/RhythmPatternData")]
public class RhythmPatternData : ScriptableObject
{
    public MusicType musicType;
    public DifficultyType difficultyType;
    public float bpm;
    public float songDuration;
    public List<RhythmNote> notes;
}
```

---

##  技術ポイント

* **指揮棒の動作解析：**

  * `BatonMotionLogger` が速度・加速度を算出
  * 拍タイミング後の平均速度で音量を決定
  * 指揮棒のY方向の切り返しで拍判定

* **リアルタイム音量制御：**

  * `AudioSource.volume` を `Mathf.Lerp()` で滑らかに補間
  * ふり幅が大きいほど音量アップ

* **データ管理：**

  * 曲・難易度ごとのリズムパターンをScriptableObjectで管理
  * ユーザーごとのスコアはJSONでローカル保存

* **UI遷移制御：**

  * `GameController`が各Canvasを管理
  * VR UIでも操作できるように設計

---

## 📊 今後の拡張予定

*  ランキング表示機能（曲・難易度別TOP10）
*  VR内キーボード入力（Meta XR Interaction SDK対応）
* 🎵 新しい曲データの追加（ScriptableObjectで簡単登録）
* 🌍 オンラインスコア共有対応
* 🪄 オーケストラステージでの演出強化（照明・音響連動）

---

##  スクリーンショット（例）

> 🎯 判定ラインとノーツ表示
> 🎵 指揮中の音量バーの変化
> 🏁 リザルトパネルのスコア表示

*(画像を後で追加する場合はここに)*

---

## 👤 制作者

**開発者：pokujiro**

* 山口大学大学院 創成科学研究科 電気電子情報系
* 興味分野：VR × 教育 / 音楽 × テクノロジー / 自己成長の仕組み化
* NASAハッカソン入賞・卒論優秀賞受賞

---

## ⚙️ ライセンス

This project is released under the **MIT License**.

---

## 💬 参考にした主な技術

* Meta XR SDK for Unity
* Unity Input System
* TextMeshPro UI
* JSON Utility
* Universal Render Pipeline (URP)
* Oculus Interaction SDK
* p5.js / Boid Algorithm (関連研究から)

---

## 💡 README更新ヒント

| 更新内容   | ファイル                  |
| ------ | --------------------- |
| 曲を追加した | `RhythmPatternData/`  |
| 難易度を追加 | `DifficultyType` Enum |
| 機能追加   | `GameController.cs`   |
| デザイン変更 | 各 `Panel.prefab`      |

---

📘 *「VR Conductor」— 音楽 × テクノロジーで、あなたも指揮者になろう。*

