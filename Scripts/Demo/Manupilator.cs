using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SCLib_SoundSystem;
using UnityEngine.Audio;

/// <summary>
/// SoundSystemの使用方法を示すデモンストレーションクラス
/// 基本的なサウンド再生の実装例と操作方法を提供します
/// 学習目的やテスト用途での活用を想定しています
/// </summary>
/// <remarks>
/// このクラスは以下の学習内容を提供します：
/// - SoundDataの基本的な作成方法
/// - SoundManagerのビルダーパターンの使用例
/// - マウス入力による手動サウンド再生
/// - ランダムピッチ効果の適用方法
/// - 3D位置指定による空間音響の実現
/// </remarks>
/// <example>
/// このスクリプトの使用方法：
/// 1. GameObjectにアタッチ
/// 2. インスペクターでAudioClipとMixerGroupを設定
/// 3. 実行時にマウス左クリックでサウンド再生
/// </example>
public class Manupilator : MonoBehaviour
{
    /// <summary>
    /// 再生するオーディオクリップ
    /// インスペクターで設定してください
    /// </summary>
    [Header("サウンド設定")]
    [SerializeField, Tooltip("再生するオーディオクリップ")]
    AudioClip clip;
    
    /// <summary>
    /// 音声の出力先AudioMixerGroup
    /// 音量調整やエフェクト適用に使用されます
    /// </summary>
    [SerializeField, Tooltip("音声の出力先AudioMixerGroup")]
    AudioMixerGroup mixerGroup;
    
    /// <summary>
    /// ループ再生するかどうか
    /// テスト用にBGMなどを繰り返し再生する場合に使用
    /// </summary>
    [SerializeField, Tooltip("ループ再生するかどうか")]
    bool loop;
    
    /// <summary>
    /// 自動再生するかどうか
    /// 通常のデモでは手動制御のためfalseを推奨
    /// </summary>
    [SerializeField, Tooltip("自動再生するかどうか（通常はfalse）")]
    bool playOnAwake;
    
    /// <summary>
    /// 作成されたサウンドデータ
    /// Start()で初期化され、Update()で使用されます
    /// </summary>
    SoundData soundData;

    /// <summary>
    /// 初期化処理
    /// インスペクターで設定された値を使用してSoundDataを作成します
    /// </summary>
    void Start()
    {
        // SoundDataを作成し、インスペクターの設定値を適用
        var data = new SoundData()
        {
            clip = clip,                    // 再生するクリップ
            mixerGroup = mixerGroup,        // 出力先ミキサーグループ
            loop = loop,                    // ループ設定
            playOnAwake = playOnAwake,      // 自動再生設定
        };

        soundData = data;
    }

    /// <summary>
    /// 入力処理とサウンド再生
    /// マウス左クリック時にSoundSystemを使用してサウンドを再生します
    /// </summary>
    /// <remarks>
    /// この実装は以下の機能を示しています：
    /// - SoundManagerのビルダーパターンの使用
    /// - ランダムピッチによる音の多様性
    /// - 3D位置指定による空間音響
    /// - メソッドチェーンによる直感的な設定
    /// </remarks>
    void Update()
    {
        // マウス左クリック時の処理
        if (Input.GetMouseButtonDown(0))
        {
            // SoundManagerのビルダーパターンを使用してサウンドを再生
            SoundManager.Instance.CreateSoundBuilder()
                 .WithSoundData(soundData)           // 作成したサウンドデータを指定
                 .WithRandomPitch()                  // ランダムピッチ効果を追加
                 .WithPosition(transform.position)   // このオブジェクトの位置で再生
                 .Play();                            // 実際の再生を実行

        }
    }

}
