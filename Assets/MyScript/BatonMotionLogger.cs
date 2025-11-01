using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatonMotionLogger : MonoBehaviour
{
    public BatonDynamicVolume batonDynamicVolume;
    [Header("Batonの音源")]
    public AudioSource batonSource;
    public bool isBatonSource = false;
    [Header("曲の音源")]
    public AudioSource MusicSource;
    public RhythmManager rhythmManager; // Inspectorで割り当ててください
    private Vector3 lastPosition;
    private Vector3 lastVelocity;
    private Vector3 velocity;
    private float velocityMagnitude;
    private int timesOfReflectVolume = 0; // 拍タイミング後の反映する回数

    [Header("Debug")]
    public bool isDebugBatonPoint = false;
    public bool isDebugVelocityAfterBatonPoint = false;
    private int frameCounterForVelocity = 0;

    private float coolTime = 0f;
    private float resultVolume = 0f;
    private float timeToOnConduct = 0f;

    // Start is called before the first frame update
    void Start()
    {
        lastPosition = transform.position;
        lastVelocity = Vector3.zero;
        coolTime = 0f;

        if (batonSource == null)
            batonSource = GetComponent<AudioSource>();

    }

    // Update is called once per frame
    void Update()
    {
        // 現在の位置
        Vector3 currentPosition = transform.position;
        coolTime -= Time.deltaTime;

        // 速度計算
        velocity = (currentPosition - lastPosition) / Time.deltaTime;
        velocityMagnitude = velocity.magnitude;

        if (timesOfReflectVolume > 0)
        {
            if (timesOfReflectVolume == 1)
            {
                resultVolume = batonDynamicVolume.SetAndReturnTargetVolumeBySwing(velocity.magnitude);
                batonDynamicVolume.SetMusicVolume();
                rhythmManager.OnConduct(timeToOnConduct, resultVolume);
            }
            else
            {
                batonDynamicVolume.SetTargetVolumeBySwing(velocity.magnitude);
                batonDynamicVolume.SetMusicVolume();
            }
            timesOfReflectVolume--;
        }

        if (isDebugVelocityAfterBatonPoint)
        {
            Debug.Log(frameCounterForVelocity + "フレーム目の速度：" + velocity.magnitude);
            frameCounterForVelocity++;
        }

        // y方向の速度
        float yVelocity = velocity.y;

        float prevYVelocity = lastVelocity.y; // 前フレームの速度

        // 条件：前が負、今が正 → －→＋ の時だけ (速度が0.2m/s以上の時)
        if (coolTime <= 0f && prevYVelocity < 0f && yVelocity > 0f && Mathf.Abs(velocity.magnitude) > 0.15f && Mathf.Abs(lastVelocity.magnitude) > 0.05f)
        {
            if (isBatonSource) // batonを振ったとき音を鳴らすのか？
            {
                batonSource.Play();
            }
            // RhythmManagerに「今拍を振った！」と通知
            if (rhythmManager != null)
            {
                timeToOnConduct = Time.time; // OnConduct　へ送る時間の設定
            }
            if (isDebugBatonPoint)
            {
                Debug.Log("拍タイミング！（下から上への切り返し）" + Time.frameCount);
            }
            if (isDebugVelocityAfterBatonPoint)
            {
                Debug.Log("拍タイミング！（下から上への切り返し）" + Time.frameCount);
                frameCounterForVelocity = 0;
            }
            // ここにスコア加点や判定処理を書く
            coolTime = 0.25f; // 0.2秒間クールタイム
            timesOfReflectVolume = 3; //　拍を打ってから3回だけ音量を変えるている
        }

        // 次フレーム用に保存
        lastPosition = currentPosition;
        lastVelocity = velocity;
    }
}
