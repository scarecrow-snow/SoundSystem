using UnityEngine;

namespace SCLib_SoundSystem
{
    /// <summary>
    /// ビルダーパターンによるサウンド設定・再生構造体
    /// メソッドチェーンを使用して直感的かつ柔軟なサウンド設定を実現します
    /// 複雑なサウンド設定を段階的に構築し、最終的にPlay()で一括実行されます
    /// </summary>
    /// <remarks>
    /// この構造体は以下の利点を提供します：
    /// - メソッドチェーンによる読みやすいコード
    /// - 段階的な設定による設定ミスの防止
    /// - 柔軟な組み合わせによる多様なサウンド再生
    /// - SoundManagerとの密接な連携による効率的なリソース管理
    /// - ゼロアロケーションによる高パフォーマンス（GC負荷ゼロ）
    /// </remarks>
    /// <example>
    /// 基本的な使用例:
    /// <code>
    /// // 基本的な再生
    /// SoundManager.Instance.CreateSoundBuilder()
    ///     .WithSoundData(soundData)
    ///     .Play();
    ///
    /// // 位置指定付きランダムピッチ再生
    /// SoundManager.Instance.CreateSoundBuilder()
    ///     .WithSoundData(explosionSound)
    ///     .WithPosition(bombPosition)
    ///     .WithRandomPitch()
    ///     .Play();
    /// </code>
    /// </example>
    public struct SoundBuilder
    {
        /// <summary>
        /// サウンド管理を担当するSoundManagerの参照
        /// プールからのエミッター取得や同時再生数チェックに使用されます
        /// </summary>
        readonly SoundManager soundManager;

        /// <summary>
        /// 再生するサウンドの設定データ
        /// WithSoundData()で設定されます
        /// </summary>
        SoundData soundData;

        /// <summary>
        /// サウンドを再生する3D位置
        /// デフォルトは原点、WithPosition()で変更可能です
        /// </summary>
        Vector3 position;

        /// <summary>
        /// ランダムピッチを適用するかどうかのフラグ
        /// WithRandomPitch()で有効化されます
        /// </summary>
        bool randomPitch;

        /// <summary>
        /// ランダムボリュームを適用するかどうかのフラグ
        /// WithRandomVolume()で有効化されます
        /// </summary>
        bool randomVolume;

        /// <summary>
        /// ランダムボリュームのオフセット範囲
        /// x: 最小値、y: 最大値
        /// </summary>
        Vector2 volumeOffset;

        /// <summary>
        /// SoundBuilderのコンストラクタ
        /// SoundManagerの参照を保持し、後続の処理で使用します
        /// </summary>
        /// <param name="soundManager">サウンド管理を担当するSoundManager</param>
        internal SoundBuilder(SoundManager soundManager)
        {
            this.soundManager = soundManager;
            this.soundData = null;
            this.position = Vector3.zero;
            this.randomPitch = false;
            this.randomVolume = false;
            this.volumeOffset = Vector2.zero;
        }

        /// <summary>
        /// 再生するサウンドデータを設定します
        /// このメソッドは必須で、Play()前に必ず呼び出す必要があります
        /// </summary>
        /// <param name="soundData">再生するサウンドの設定データ</param>
        /// <returns>メソッドチェーンのためのSoundBuilderインスタンス</returns>
        /// <example>
        /// <code>
        /// builder.WithSoundData(explosionSoundData)
        /// </code>
        /// </example>
        public SoundBuilder WithSoundData(SoundData soundData)
        {
            var builder = this;
            builder.soundData = soundData;
            return builder;
        }

        /// <summary>
        /// サウンドを再生する3D位置を設定します
        /// 3D音響設定（spatialBlend等）と組み合わせて空間音響効果を実現します
        /// </summary>
        /// <param name="position">再生位置のワールド座標</param>
        /// <returns>メソッドチェーンのためのSoundBuilderインスタンス</returns>
        /// <example>
        /// <code>
        /// builder.WithPosition(player.transform.position)
        /// builder.WithPosition(new Vector3(10, 0, 5))
        /// </code>
        /// </example>
        public SoundBuilder WithPosition(Vector3 position)
        {
            var builder = this;
            builder.position = position;
            return builder;
        }

        /// <summary>
        /// ランダムピッチ効果を有効にします
        /// 同じサウンドでも毎回異なるピッチで再生され、音の多様性を実現します
        /// </summary>
        /// <returns>メソッドチェーンのためのSoundBuilderインスタンス</returns>
        /// <remarks>
        /// ピッチ変化の範囲はSoundEmitter.WithRandomPitch()で制御されます
        /// デフォルトは±0.05の範囲です
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.WithRandomPitch() // 足音や環境音の単調さを防ぐ
        /// </code>
        /// </example>
        public SoundBuilder WithRandomPitch()
        {
            var builder = this;
            builder.randomPitch = true;
            return builder;
        }

        /// <summary>
        /// ランダム音量効果を有効にします
        /// 同じサウンドでも毎回異なる音量で再生され、音の多様性を実現します
        /// </summary>
        /// <param name="min">音量変化の最小値（負の値で小さくなる）</param>
        /// <param name="max">音量変化の最大値（正の値で大きくなる）</param>
        /// <returns>メソッドチェーンのためのSoundBuilderインスタンス</returns>
        /// <remarks>
        /// 音量変化の範囲はSoundEmitter.WithRandomVolume()で制御されます
        /// 基準値からの相対的な変化として適用されます
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.WithRandomVolume(-0.1f, 0.1f) // 環境音の音量バリエーション
        /// </code>
        /// </example>
        public SoundBuilder WithRandomVolume(float min = -0.05f, float max = 0.05f)
        {
            var builder = this;
            builder.randomVolume = true;
            builder.volumeOffset = new Vector2(min, max);
            return builder;
        }

        /// <summary>
        /// 設定されたパラメータでサウンドを再生します
        /// これまでのメソッドチェーンで設定された全ての設定を適用し、実際の再生を実行します
        /// </summary>
        /// <remarks>
        /// 実行される処理の流れ：
        /// 1. サウンドデータと再生可能性のバリデーション
        /// 2. プールからSoundEmitterを取得
        /// 3. SoundEmitterの初期化と位置設定
        /// 4. オプション設定（ランダムピッチ等）の適用
        /// 5. 同時再生数カウントの更新
        /// 6. 実際の再生開始
        /// </remarks>
        /// <example>
        /// <code>
        /// // 完全な使用例
        /// SoundManager.Instance.CreateSoundBuilder()
        ///     .WithSoundData(soundData)      // 必須
        ///     .WithPosition(transform.position) // オプション
        ///     .WithRandomPitch()             // オプション
        ///     .Play();                       // 実行
        /// </code>
        /// </example>
        public void Play()
        {
            // バリデーション：サウンドデータの存在と再生可能性をチェック
            if (soundData == null || !soundManager.CanPlaySound(soundData)) return;

            // プールからSoundEmitterを取得
            SoundEmitter soundEmitter = soundManager.Get();
            if (soundEmitter == null) return;

            // SoundEmitterの初期化と配置設定
            soundEmitter.Initialize(soundData, soundManager);                    // サウンドデータを適用
            soundEmitter.transform.position = position;           // 3D位置を設定
            soundEmitter.transform.parent = soundManager.transform; // 階層構造を整理

            // オプション設定の適用
            if (randomPitch)
            {
                soundEmitter.WithRandomPitch(); // ランダムピッチ効果を適用
            }

            if (randomVolume)
            {
                soundEmitter.WithRandomVolume(volumeOffset.x, volumeOffset.y);
            }

            // 同時再生数の追跡更新
            IncrementSoundCount();

            // サウンド再生開始（非同期監視も自動開始）
            soundEmitter.Play();
        }

        /// <summary>
        /// 指定されたサウンドデータの同時再生数カウントを増加させます
        /// パフォーマンス保護のための同時再生数制限システムの一部です
        /// </summary>
        /// <remarks>
        /// このカウントは、SoundEmitterがプールに返却される際に
        /// SoundManager.OnReturnedToPool()で自動的に減算されます
        /// </remarks>
        void IncrementSoundCount()
        {
            if (soundManager.Counts.ContainsKey(soundData))
            {
                soundManager.Counts[soundData]++; // 既存カウントを増加
            }
            else
            {
                soundManager.Counts[soundData] = 1; // 新規カウント開始
            }
        }
    }
}