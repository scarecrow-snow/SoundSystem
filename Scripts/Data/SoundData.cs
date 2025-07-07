using System;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioSystem
{
    /// <summary>
    /// AudioSourceの全パラメータを管理するデータクラス
    /// Unityの標準AudioSourceが持つ全ての設定項目を包含し、
    /// シリアライズ可能な形でサウンド設定を保存・管理します
    /// </summary>
    /// <remarks>
    /// このクラスは以下の特徴を持ちます：
    /// - AudioSourceの完全な設定の保存と復元
    /// - インスペクターでの直感的な設定編集
    /// - ScriptableObjectとしての活用も可能
    /// - 再利用可能なサウンド設定の管理
    /// </remarks>
    /// <example>
    /// 使用例:
    /// <code>
    /// // コードでの作成
    /// var soundData = new SoundData()
    /// {
    ///     clip = audioClip,
    ///     volume = 0.8f,
    ///     pitch = 1.2f,
    ///     spatialBlend = 1.0f // 3D音響
    /// };
    /// 
    /// // 再生
    /// SoundManager.Instance.CreateSoundBuilder()
    ///     .WithSoundData(soundData)
    ///     .Play();
    /// </code>
    /// </example>
    [Serializable]
    public class SoundData
    {
        // ===== 基本設定 =====
        
        /// <summary>
        /// 再生するオーディオクリップ
        /// 実際の音声データファイルです
        /// </summary>
        [Header("基本設定")]
        [Tooltip("再生するオーディオクリップ")]
        public AudioClip clip;
        
        /// <summary>
        /// 出力先AudioMixerGroup
        /// 音量調整やエフェクト適用に使用されます
        /// </summary>
        [Tooltip("出力先AudioMixerGroup（音量調整・エフェクト用）")]
        public AudioMixerGroup mixerGroup;
        
        /// <summary>
        /// ループ再生するかどうか
        /// trueの場合、クリップの最後まで再生後に先頭から繰り返します
        /// </summary>
        [Tooltip("ループ再生するかどうか")]
        public bool loop;
        
        /// <summary>
        /// 自動再生するかどうか
        /// 通常はfalseにして手動制御を推奨します
        /// </summary>
        [Tooltip("自動再生するかどうか（通常はfalse推奨）")]
        public bool playOnAwake;
        

        // ===== 音響効果設定 =====
        
        /// <summary>
        /// 音声をミュートするかどうか
        /// デバッグ時の一時的な音声オフに便利です
        /// </summary>
        [Header("音響効果")]
        [Tooltip("音声をミュートするかどうか")]
        public bool mute;
        
        /// <summary>
        /// オーディオエフェクトをバイパスするかどうか
        /// パフォーマンス向上や特定の音響効果除外に使用されます
        /// </summary>
        [Tooltip("オーディオエフェクトをバイパスするかどうか")]
        public bool bypassEffects;
        
        /// <summary>
        /// オーディオリスナーエフェクトをバイパスするかどうか
        /// UI音などでリスナー位置の影響を受けたくない場合に使用されます
        /// </summary>
        [Tooltip("オーディオリスナーエフェクトをバイパスするかどうか")]
        public bool bypassListenerEffects;
        
        /// <summary>
        /// リバーブゾーンエフェクトをバイパスするかどうか
        /// 環境音響効果を受けたくない音に使用されます
        /// </summary>
        [Tooltip("リバーブゾーンエフェクトをバイパスするかどうか")]
        public bool bypassReverbZones;

        // ===== 音量・ピッチ設定 =====
        
        /// <summary>
        /// 再生優先度 (0-256、低いほど高優先度)
        /// 同時再生数が限界に達した際の優先順位を決定します
        /// </summary>
        [Header("音量・ピッチ")]
        [Range(0, 256)]
        [Tooltip("再生優先度（0-256、低いほど高優先度）")]
        public int priority = 128;
        
        /// <summary>
        /// 音量 (0.0-1.0)
        /// 基準となる音量レベルです
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("音量（0.0-1.0）")]
        public float volume = 1f;
        
        /// <summary>
        /// ピッチ（再生速度） (-3.0-3.0、1.0が標準)
        /// 音の高さと再生速度を同時に変更します
        /// </summary>
        [Range(-3f, 3f)]
        [Tooltip("ピッチ（再生速度、1.0が標準）")]
        public float pitch = 1f;
        
        /// <summary>
        /// ステレオパン (-1.0-1.0、0が中央)
        /// 左右の音量バランスを調整します
        /// </summary>
        [Range(-1f, 1f)]
        [Tooltip("ステレオパン（-1.0が左、1.0が右、0が中央）")]
        public float panStereo;
        
        /// <summary>
        /// 空間ブレンド (0.0-1.0、0が2D、1が3D)
        /// 2D音響と3D音響の混合比率を設定します
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("空間ブレンド（0.0が2D、1.0が3D）")]
        public float spatialBlend;
        
        /// <summary>
        /// リバーブゾーンミックス (0.0-1.1)
        /// 環境音響効果の強さを調整します
        /// </summary>
        [Range(0f, 1.1f)]
        [Tooltip("リバーブゾーンミックス（環境音響効果の強さ）")]
        public float reverbZoneMix = 1f;
        
        /// <summary>
        /// ドップラーレベル (0.0-5.0)
        /// 移動による音の周波数変化の強さを設定します
        /// </summary>
        [Range(0f, 5f)]
        [Tooltip("ドップラーレベル（移動による音の周波数変化）")]
        public float dopplerLevel = 1f;
        
        /// <summary>
        /// 3D音響の拡散角度 (0-360度)
        /// 指向性のある音源の拡散範囲を設定します
        /// </summary>
        [Range(0f, 360f)]
        [Tooltip("3D音響の拡散角度（指向性音源の拡散範囲）")]
        public float spread;

        // ===== 距離設定 =====
        
        /// <summary>
        /// 最小距離
        /// この距離内では音量が最大となります
        /// </summary>
        [Header("距離設定")]
        [Tooltip("最小距離（この距離内で音量最大）")]
        public float minDistance = 1f;
        
        /// <summary>
        /// 最大距離
        /// この距離で音が完全に聞こえなくなります
        /// </summary>
        [Tooltip("最大距離（この距離で音が聞こえなくなる）")]
        public float maxDistance = 500f;

        // ===== リスナー設定 =====
        
        /// <summary>
        /// リスナー音量設定を無視するかどうか
        /// マスター音量に影響されたくない音に使用されます
        /// </summary>
        [Header("リスナー設定")]
        [Tooltip("リスナー音量設定を無視するかどうか")]
        public bool ignoreListenerVolume;
        
        /// <summary>
        /// リスナーポーズ設定を無視するかどうか
        /// ゲームポーズ中も再生し続けたい音に使用されます
        /// </summary>
        [Tooltip("リスナーポーズ設定を無視するかどうか")]
        public bool ignoreListenerPause;

        // ===== ロールオフ設定 =====
        
        /// <summary>
        /// 距離による音量減衰カーブ
        /// 対数的減衰が最も自然で推奨されます
        /// </summary>
        [Header("距離減衰")]
        [Tooltip("距離による音量減衰カーブ（Logarithmicが推奨）")]
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    }
}