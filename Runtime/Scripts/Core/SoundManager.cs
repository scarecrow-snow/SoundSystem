using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace SCLib_SoundSystem
{
    /// <summary>
    /// サウンド管理システムの中核となるマネージャークラス
    /// オブジェクトプーリング、同時再生数制限、効率的なリソース管理を提供します
    /// シングルトンパターンでシーン間で永続化され、すべてのサウンド再生を統括します
    /// </summary>
    /// <remarks>
    /// このクラスは以下の主要機能を提供します：
    /// - SoundEmitterのオブジェクトプーリングによる効率的なメモリ管理
    /// - 同一サウンドの同時再生数制限によるパフォーマンス最適化
    /// - ビルダーパターンによる直感的なサウンド再生API
    /// - アクティブなサウンドエミッターの追跡と管理
    /// </remarks>
    /// <example>
    /// 基本的な使用例:
    /// <code>
    /// // サウンドを再生
    /// SoundManager.CreateSoundBuilder()
    ///     .WithSoundData(soundData)
    ///     .WithPosition(transform.position)
    ///     .WithRandomPitch()
    ///     .Play();
    /// 
    /// // すべてのサウンドを停止
    /// SoundManager.StopAll();
    /// </code>
    /// </example>
    public class SoundManager : IDisposable
    {
        /// <summary>
        /// SoundEmitterオブジェクトのプール
        /// メモリ効率とパフォーマンスを向上させるため、オブジェクトの再利用を管理します
        /// </summary>
        IObjectPool<SoundEmitter> soundEmitterPool;

        /// <summary>
        /// 現在アクティブ（再生中）なSoundEmitterのリスト
        /// StopAll()メソッドや統計情報の取得に使用されます
        /// </summary>
        readonly List<SoundEmitter> activeSoundEmitters = new();

        /// <summary>
        /// 各SoundDataの現在の同時再生数を追跡する辞書
        /// 同時再生数制限の実装に使用され、パフォーマンスの最適化を図ります
        /// </summary>
        public readonly Dictionary<SoundData, int> Counts = new();

        /// <summary>
        /// SoundEmitterプレハブの参照
        /// プールで生成されるSoundEmitterの元となるプレハブです
        /// </summary>
        SoundEmitter soundEmitterPrefab;

        /// <summary>
        /// プールのコレクションチェックを有効にするかどうか
        /// デバッグ時に重複した返却を検出できますが、パフォーマンスに影響します
        /// </summary>
        bool collectionCheck = true;

        /// <summary>
        /// プールの初期容量
        /// 開始時に事前に作成されるSoundEmitterの数です
        /// </summary>
        int defaultCapacity = 10;

        /// <summary>
        /// プールの最大サイズ
        /// プールが保持できるSoundEmitterの最大数です
        /// </summary>
        int maxPoolSize = 100;

        /// <summary>
        /// 同一サウンドの最大同時再生数
        /// パフォーマンスを保護するための制限値です
        /// </summary>
        int maxSoundInstances = 30;

        Transform parent;

        public Transform transform => parent;

        public SoundManager(SoundEmitter soundEmitterPrefab, Transform parent, bool collectionCheck = true, int defaultCapacity = 10, int maxPoolSize = 100, int maxSoundInstances = 30)
        {
            this.soundEmitterPrefab = soundEmitterPrefab;
            this.parent = parent;
            this.collectionCheck = collectionCheck;
            this.defaultCapacity = defaultCapacity;
            this.maxPoolSize = maxPoolSize;
            this.maxSoundInstances = maxSoundInstances;

            InitializePool();
        }

        /// <summary>
        /// サウンド再生用のビルダーを作成します
        /// ビルダーパターンにより、柔軟で直感的なサウンド設定が可能です
        /// </summary>
        /// <returns>設定可能なSoundBuilderインスタンス</returns>
        /// <example>
        /// 使用例:
        /// <code>
        /// SoundManager.Instance.CreateSoundBuilder()
        ///     .WithSoundData(soundData)
        ///     .WithPosition(Vector3.zero)
        ///     .Play();
        /// </code>
        /// </example>
        public SoundBuilder CreateSoundBuilder() => new SoundBuilder(this);

        /// <summary>
        /// 指定されたサウンドデータが再生可能かどうかを判定します
        /// 同時再生数の制限をチェックし、パフォーマンスを保護します
        /// </summary>
        /// <param name="data">チェックするサウンドデータ</param>
        /// <returns>再生可能な場合true、制限に達している場合false</returns>
        public bool CanPlaySound(SoundData data)
        {
            if (data == null) return false;
            return !Counts.TryGetValue(data, out var count) || count < maxSoundInstances;
        }

        /// <summary>
        /// プールからSoundEmitterを取得します
        /// 内部的にSoundBuilderから呼び出されるメソッドです
        /// </summary>
        /// <returns>プールから取得したSoundEmitterインスタンス</returns>
        public SoundEmitter Get()
        {
            return soundEmitterPool.Get();
        }

        /// <summary>
        /// 使用済みのSoundEmitterをプールに返却します
        /// SoundEmitterの再生終了時に自動的に呼び出されます
        /// </summary>
        /// <param name="soundEmitter">返却するSoundEmitterインスタンス</param>
        public void ReturnToPool(SoundEmitter soundEmitter)
        {
            soundEmitterPool.Release(soundEmitter);
        }

        /// <summary>
        /// 現在再生中のすべてのサウンドを停止します
        /// ゲームポーズやシーン切り替え時に使用されます
        /// </summary>
        public void StopAll()
        {
            // アクティブなエミッター全てに対して停止処理を実行
            foreach (var soundEmitter in activeSoundEmitters)
            {
                soundEmitter.Stop();
            }
        }

        /// <summary>
        /// オブジェクトプールを初期化します
        /// SoundEmitterの効率的な管理とメモリ最適化を実現します
        /// </summary>
        void InitializePool()
        {
            soundEmitterPool = new ObjectPool<SoundEmitter>(
                CreateSoundEmitter,      // オブジェクト作成時のコールバック
                OnTakeFromPool,          // プールから取得時のコールバック
                OnReturnedToPool,        // プールに返却時のコールバック
                OnDestroyPoolObject,     // オブジェクト破棄時のコールバック
                collectionCheck,         // デバッグ用のコレクションチェック
                defaultCapacity,         // 初期容量
                maxPoolSize);            // 最大サイズ
        }

        /// <summary>
        /// 新しいSoundEmitterインスタンスを作成します
        /// プレハブからインスタンス化し、初期状態を非アクティブに設定します
        /// </summary>
        /// <returns>作成されたSoundEmitterインスタンス</returns>
        SoundEmitter CreateSoundEmitter()
        {
            var soundEmitter = GameObject.Instantiate(soundEmitterPrefab);
            soundEmitter.gameObject.SetActive(false); // プール内では非アクティブ状態
            return soundEmitter;
        }

        /// <summary>
        /// プールからSoundEmitterが取得された際の処理
        /// オブジェクトをアクティブ化し、アクティブリストに追加します
        /// </summary>
        /// <param name="soundEmitter">取得されたSoundEmitterインスタンス</param>
        void OnTakeFromPool(SoundEmitter soundEmitter)
        {
            soundEmitter.gameObject.SetActive(true);        // アクティブ化
            activeSoundEmitters.Add(soundEmitter);           // アクティブリストに追加
        }

        /// <summary>
        /// SoundEmitterがプールに返却された際の処理
        /// 同時再生数カウントの更新、非アクティブ化、アクティブリストからの削除を行います
        /// </summary>
        /// <param name="soundEmitter">返却されたSoundEmitterインスタンス</param>
        void OnReturnedToPool(SoundEmitter soundEmitter)
        {
            // 同時再生数カウントを減算（安全性チェック付き）
            if (soundEmitter.Data != null && Counts.TryGetValue(soundEmitter.Data, out var count) && count > 0)
            {
                Counts[soundEmitter.Data] = count - 1;
            }
            soundEmitter.gameObject.SetActive(false);        // 非アクティブ化
            activeSoundEmitters.Remove(soundEmitter);         // アクティブリストから削除
        }

        /// <summary>
        /// プールサイズ超過時にSoundEmitterを破棄する際の処理
        /// GameObjectを完全に削除してメモリを解放します
        /// </summary>
        /// <param name="soundEmitter">破棄するSoundEmitterインスタンス</param>
        void OnDestroyPoolObject(SoundEmitter soundEmitter)
        {
            if (soundEmitter != null && soundEmitter.gameObject != null)
            {
                GameObject.Destroy(soundEmitter.gameObject);
            }
        }

        public void Dispose()
        {
            // アクティブなサウンドエミッターを全て停止
            foreach (var soundEmitter in activeSoundEmitters)
            {
                soundEmitter.Stop();
            }
            activeSoundEmitters.Clear();

            // 同時再生数カウントをクリア
            Counts.Clear();
        }
    }
}