using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using Random = UnityEngine.Random;

namespace AudioSystem
{
    /// <summary>
    /// 個別のサウンド再生を担当するエミッタークラス
    /// SoundManagerによってプール管理され、効率的なサウンド再生を実現します
    /// UniTaskによる非同期処理で自動的なプール返却を行い、メモリリークを防止します
    /// </summary>
    /// <remarks>
    /// このクラスは以下の主要機能を提供します：
    /// - SoundDataに基づくAudioSourceの完全な設定
    /// - UniTaskを使用した非同期の再生完了監視
    /// - 自動的なプール返却によるリソース管理
    /// - ランダムピッチ機能による音の多様性
    /// - 手動停止機能とリソースクリーンアップ
    /// </remarks>
    /// <example>
    /// 通常はSoundBuilder経由で使用されます：
    /// <code>
    /// SoundManager.Instance.CreateSoundBuilder()
    ///     .WithSoundData(soundData)
    ///     .WithRandomPitch()
    ///     .Play(); // 内部でSoundEmitterが自動管理される
    /// </code>
    /// </example>
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        /// <summary>
        /// このエミッターに設定されているサウンドデータ
        /// 初期化後に参照可能になります
        /// </summary>
        public SoundData Data { get; private set; }
        
        /// <summary>
        /// 実際の音声再生を担当するAudioSourceコンポーネント
        /// SoundDataの設定が全て適用されます
        /// </summary>
        AudioSource audioSource;
        
        /// <summary>
        /// 非同期処理のキャンセル制御用トークンソース
        /// 手動停止時や重複再生防止に使用されます
        /// </summary>
        CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// MonoBehaviourのAwakeメソッド
        /// AudioSourceコンポーネントを取得または追加します
        /// </summary>
        void Awake()
        {
            audioSource = gameObject.GetOrAdd<AudioSource>();
        }

        /// <summary>
        /// SoundDataを使用してAudioSourceを初期化します
        /// SoundDataの全パラメータをAudioSourceに適用し、再生準備を整えます
        /// </summary>
        /// <param name="data">適用するサウンドデータ</param>
        /// <remarks>
        /// この初期化により、AudioSourceはSoundDataで定義された
        /// 全ての音響設定（音量、ピッチ、3D設定など）を取得します
        /// </remarks>
        public void Initialize(SoundData data)
        {
            if (data == null) return;
            
            Data = data;
            
            // 基本設定
            audioSource.clip = data.clip;
            audioSource.outputAudioMixerGroup = data.mixerGroup;
            audioSource.loop = data.loop;
            audioSource.playOnAwake = data.playOnAwake;
            
            // 音響効果設定
            audioSource.mute = data.mute;
            audioSource.bypassEffects = data.bypassEffects;
            audioSource.bypassListenerEffects = data.bypassListenerEffects;
            audioSource.bypassReverbZones = data.bypassReverbZones;
            
            // 音量・ピッチ・優先度設定
            audioSource.priority = data.priority;
            audioSource.volume = data.volume;
            audioSource.pitch = data.pitch;
            audioSource.panStereo = data.panStereo;
            
            // 3D音響設定
            audioSource.spatialBlend = data.spatialBlend;
            audioSource.reverbZoneMix = data.reverbZoneMix;
            audioSource.dopplerLevel = data.dopplerLevel;
            audioSource.spread = data.spread;
            audioSource.minDistance = data.minDistance;
            audioSource.maxDistance = data.maxDistance;
            audioSource.rolloffMode = data.rolloffMode;
            
            // リスナー設定
            audioSource.ignoreListenerVolume = data.ignoreListenerVolume;
            audioSource.ignoreListenerPause = data.ignoreListenerPause;
        }

        /// <summary>
        /// サウンドの再生を開始します
        /// 既存の再生を停止してから新しい再生を開始し、
        /// 非同期で再生完了を監視してプールに自動返却します
        /// </summary>
        public void Play()
        {
            StopPlaying();                                              // 既存の再生処理を停止
            audioSource.Play();                                         // AudioSourceで再生開始
            cancellationTokenSource = new CancellationTokenSource();   // キャンセル制御用トークン作成
            WaitForSoundToEnd(cancellationTokenSource.Token).Forget(); // 非同期監視開始
        }

        /// <summary>
        /// サウンドの再生完了を非同期で監視し、完了時に自動的にプールへ返却します
        /// UniTaskを使用することで、メインスレッドをブロックせずに効率的な監視を実現します
        /// </summary>
        /// <param name="cancellationToken">処理のキャンセル制御用トークン</param>
        /// <remarks>
        /// この非同期処理により、開発者は手動でプール返却を行う必要がなく、
        /// メモリリークを防止できます
        /// </remarks>
        private async UniTaskVoid WaitForSoundToEnd(CancellationToken cancellationToken)
        {
            try
            {
                // AudioSourceの再生が完了するまで待機
                await UniTask.WaitWhile(() => audioSource.isPlaying, cancellationToken: cancellationToken);
                
                // 再生完了後、プールに自動返却
                SoundManager.Instance.ReturnToPool(this);
            }
            catch (OperationCanceledException)
            {
                // 手動停止や重複再生防止でキャンセルされた場合の処理
                // 特別な処理は不要（プール返却は別途行われる）
            }
        }

        /// <summary>
        /// サウンドの再生を手動で停止し、即座にプールに返却します
        /// ゲームポーズや緊急停止が必要な場合に使用されます
        /// </summary>
        public void Stop()
        {
            StopPlaying();                               // 非同期処理を停止
            audioSource.Stop();                          // AudioSourceの再生停止
            SoundManager.Instance.ReturnToPool(this);   // プールに返却
        }

        /// <summary>
        /// 進行中の非同期再生監視処理を停止します
        /// キャンセルトークンを使用して安全にリソースを解放します
        /// </summary>
        private void StopPlaying()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();   // 非同期処理をキャンセル
                cancellationTokenSource.Dispose();  // リソース解放
                cancellationTokenSource = null;     // 参照をクリア
            }
        }

        /// <summary>
        /// 音のピッチにランダムな変化を加えます
        /// 同じサウンドでも毎回異なる印象を与え、音の多様性を実現します
        /// </summary>
        /// <param name="min">ピッチ変化の最小値（負の値で低くなる）</param>
        /// <param name="max">ピッチ変化の最大値（正の値で高くなる）</param>
        /// <remarks>
        /// 元のSoundData.pitchを基準値として、指定範囲内でランダムに変化させます
        /// 例：足音、武器音、環境音などの単調さを防ぐのに効果的です
        /// </remarks>
        public void WithRandomPitch(float min = -0.05f, float max = 0.05f)
        {
            audioSource.pitch = Data.pitch + Random.Range(min, max);
        }
    }
}
