using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using Random = UnityEngine.Random;

namespace SCLib_SoundSystem
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
        /// サウンドの再生完了を非同視で監視し、完了時に自動的にプールへ返却します
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
                // AudioSourceの再生が完了するまで待機（nullチェック付き）
                await UniTask.WaitWhile(() => 
                {
                    // オブジェクトが破棄されていないかチェック
                    if (this == null || audioSource == null) return false;
                    
                    // AudioSourceが破棄されている場合のセーフガード
                    try
                    {
                        return audioSource.isPlaying;
                    }
                    catch (MissingReferenceException)
                    {
                        // AudioSourceが破棄されている場合は再生終了とみなす
                        return false;
                    }
                }, cancellationToken: cancellationToken);

                // 再生完了後、プールに自動返却（オブジェクトの存在確認付き）
                ReturnToPoolSafely();
            }
            catch (OperationCanceledException)
            {
                // 手動停止や重複再生防止でキャンセルされた場合の処理
                // 特別な処理は不要（プール返却は別途行われる）
            }
            catch (MissingReferenceException)
            {
                // オブジェクトが破棄されている場合は静かに終了
                // プール返却は不要（オブジェクトが既に破棄されているため）
            }
        }

        /// <summary>
        /// サウンドの再生を手動で停止し、即座にプールに返却します
        /// ゲームポーズや緊急停止が必要な場合に使用されます
        /// </summary>
        public void Stop()
        {
            StopPlaying();                               // 非同期処理を停止
            
            // AudioSourceの再生停止（安全にアクセス）
            if (audioSource != null)
            {
                try
                {
                    audioSource.Stop();
                }
                catch (MissingReferenceException)
                {
                    // AudioSourceが破棄されている場合は無視
                }
            }
            
            // プールに安全に返却
            ReturnToPoolSafely();
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
        /// ピッチ値は0.1f～3.0fの範囲内に自動調整されます
        /// </remarks>
        public void WithRandomPitch(float min = -0.05f, float max = 0.05f)
        {
            // nullチェック：Dataが未初期化の場合は処理をスキップ
            if (Data == null) return;
            
            // 基準ピッチにランダム値を加算
            float randomizedPitch = Data.pitch + Random.Range(min, max);
            
            // ピッチ値を安全な範囲内に制限（0.1f～3.0f）
            audioSource.pitch = Mathf.Clamp(randomizedPitch, 0.1f, 3.0f);
        }
        

        /// <summary>
        /// 音の音量にランダムな変化を加えます
        /// 同じサウンドでも毎回異なる音量で再生され、音の多様性を実現します
        /// </summary>
        /// <param name="min">音量変化の最小値（負の値で小さくなる）</param>
        /// <param name="max">音量変化の最大値（正の値で大きくなる）</param>
        /// <remarks>
        /// 元のSoundData.volumeを基準値として、指定範囲内でランダムに変化させます
        /// 例：環境音、効果音などの単調さを防ぐのに効果的です
        /// 音量値は0.0f～1.0fの範囲内に自動調整されます
        /// </remarks>
        public void WithRandomVolume(float min = -0.05f, float max = 0.05f)
        {
            // nullチェック：Dataが未初期化の場合は処理をスキップ
            if (Data == null) return;
            
            // 基準音量にランダム値を加算
            float randomizedVolume = Data.volume + Random.Range(min, max);
            
            // 音量値を安全な範囲内に制限（0.0f～1.0f）
            audioSource.volume = Mathf.Clamp01(randomizedVolume);
        }

        /// <summary>
        /// プールに安全に返却するためのメソッド
        /// SoundManagerの存在確認とオブジェクトの状態確認を行います
        /// </summary>
        private void ReturnToPoolSafely()
        {
            // オブジェクトが破棄されていないかチェック
            if (this == null) return;
            
            // SoundManagerが存在し、かつインスタンスが有効であることを確認
            if (SoundManager.Instance != null)
            {
                try
                {
                    SoundManager.Instance.ReturnToPool(this);
                }
                catch (MissingReferenceException)
                {
                    // SoundManagerが破棄されている場合は静かに終了
                }
            }
        }

        /// <summary>
        /// オブジェクトが破棄される際のクリーンアップ処理
        /// 非同期処理を安全に停止し、リソースを解放します
        /// </summary>
        void OnDestroy()
        {
            // 進行中の非同期処理を停止
            StopPlaying();
            
            // AudioSourceを明示的に停止（残存している場合）
            if (audioSource != null)
            {
                try
                {
                    audioSource.Stop();
                }
                catch (MissingReferenceException)
                {
                    // AudioSourceが既に破棄されている場合は無視
                }
            }
        }
    }
}
