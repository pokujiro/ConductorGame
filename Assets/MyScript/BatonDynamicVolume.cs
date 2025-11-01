using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BatonDynamicVolume : MonoBehaviour
{
    /// <summary>
    /// 指揮棒の「ふり幅」や「速度」から曲の音量をリアルタイム制御するスクリプト
    /// （BatonMotionLogger から SetVolumeBySwing() を呼び出して使う）
    /// BatonMotionLogger　から　拍タイミング後の3フレームの速度を曲のボリュームに反映している
    /// </summary>

    [Header("制御対象の曲音源")]
    public AudioSource musicSource;  // Inspectorでセット
    [Header("Volume可視化UI")]
    public Image volumeBar; // Inspectorでセット
    [Header("ふり幅→音量変換の最大値")]
    public float maxSwingVelocity = 8.0f;    // 実験値で調整 大きく振って６（速度）であった
    [Header("音量変化の滑らかさ")]
    [Range(0.01f, 1.0f)]
    public float smoothing = 0.5f;   // Lerp係数（0.01〜0.5くらいで実験）

    private float targetVolume = 0.5f;   // 現在目指す音量値

    /// <summary>
    /// 指揮棒のふり幅（速度や移動量など）を受け取って、音量に反映する
    /// </summary>
    /// <param name="swing">直近のふり幅・速度値など（0〜maxSwing想定）</param>
    public void SetTargetVolumeBySwing(float swingVelocity)
    {
        // スムーズな音量変化（ふり幅に応じて 0〜1に正規化）具体的なターゲット音量の設定
        targetVolume = Mathf.Clamp01(swingVelocity / maxSwingVelocity);
    }

    public float SetAndReturnTargetVolumeBySwing(float swingVelocity)
    {
        // スムーズな音量変化（ふり幅に応じて 0〜1に正規化）具体的なターゲット音量の設定
        targetVolume = Mathf.Clamp01(swingVelocity / maxSwingVelocity);
        return targetVolume;
    }

    public void SetMusicVolume()
    {
        if (musicSource == null) return;

        // 実際のAudioSource.volumeをLerpでなめらかに追従
        // Mathf.Lerp(a, b, t)：「a→bへt%ぶん移動した値」を返す
        musicSource.volume = Mathf.Lerp(musicSource.volume, targetVolume, smoothing);
        if (volumeBar != null)
        {
            // 例：ImageのWidth（横方向）を volume(0～1) に応じて変化
            var rt = volumeBar.rectTransform;
            float maxHight = 1f; // 棒グラフの最大横幅
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, musicSource.volume * maxHight);
        }
    }
}

