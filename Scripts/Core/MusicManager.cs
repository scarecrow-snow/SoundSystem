using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SCLib_SoundSystem
{
    /// <summary>
    /// 音楽管理とクロスフェード機能を提供するマネージャークラス
    /// プレイリスト機能、自動的な音楽の連続再生、滑らかなクロスフェード効果を実現します
    /// シングルトンパターンでシーン間で永続化され、BGM管理を統括します
    /// </summary>
    /// <remarks>
    /// このクラスは以下の主要機能を提供します：
    /// - キューベースのプレイリスト管理
    /// - 対数フェードによる自然なクロスフェード効果
    /// - 自動的な次曲再生とループ機能
    /// - AudioMixerとの統合サポート
    /// </remarks>
    /// <example>
    /// 基本的な使用例:
    /// <code>
    /// // プレイリストに音楽を追加
    /// MusicManager.Instance.AddToPlaylist(bgmClip);
    /// 
    /// // 特定の曲を即座に再生
    /// MusicManager.Instance.Play(menuMusicClip);
    /// 
    /// // プレイリストをクリア
    /// MusicManager.Instance.Clear();
    /// </code>
    /// </example>
    public class MusicManager : PersistentSingleton<MusicManager>
    {
        /// <summary>
        /// クロスフェードにかかる時間（秒）
        /// 曲の切り替えにかかる時間を制御します
        /// </summary>
        const float crossFadeTime = 1.0f;

        /// <summary>
        /// 現在のフェード進行時間
        /// 0以下の場合はフェード処理は行われません
        /// </summary>
        float fading;

        /// <summary>
        /// 現在再生中のAudioSource
        /// フェードイン対象として使用されます
        /// </summary>
        AudioSource current;

        /// <summary>
        /// 前回再生していたAudioSource
        /// フェードアウト対象として使用されます
        /// </summary>
        AudioSource previous;

        /// <summary>
        /// 再生待ちの音楽クリップのキュー
        /// FIFO（先入先出）で管理されます
        /// </summary>
        readonly Queue<AudioClip> playlist = new();

        /// <summary>
        /// 開始時に自動でプレイリストに追加される音楽リスト
        /// インスペクターで設定可能です
        /// </summary>
        [SerializeField, Tooltip("開始時に自動でプレイリストに追加される音楽リスト")]
        List<AudioClip> initialPlaylist;

        /// <summary>
        /// 音楽用のAudioMixerGroup
        /// 音楽の音量調整やエフェクト適用に使用されます
        /// </summary>
        [SerializeField, Tooltip("音楽用のAudioMixerGroup（音量調整・エフェクト用）")]
        AudioMixerGroup musicMixerGroup;

        /// <summary>
        /// MonoBehaviourのStartメソッド
        /// 初期プレイリストに設定された音楽を自動的にキューに追加します
        /// </summary>
        void Start()
        {
            // 初期プレイリストの全クリップをキューに追加
            foreach (var clip in initialPlaylist)
            {
                AddToPlaylist(clip);
            }
        }

        /// <summary>
        /// 指定された音楽クリップをプレイリストに追加します
        /// 何も再生されていない場合は自動的に再生を開始します
        /// </summary>
        /// <param name="clip">追加する音楽クリップ</param>
        public void AddToPlaylist(AudioClip clip)
        {
            if (clip == null) return;

            playlist.Enqueue(clip);

            // 何も再生されていない場合は即座に再生開始
            if (current == null && previous == null)
            {
                PlayNextTrack();
            }
        }

        /// <summary>
        /// プレイリストをクリアします
        /// 現在再生中の音楽は停止されません
        /// </summary>
        public void Clear() => playlist.Clear();

        /// <summary>
        /// プレイリストから次の曲を再生します
        /// キューが空の場合は何も実行されません
        /// </summary>
        public void PlayNextTrack()
        {
            if (playlist.TryDequeue(out AudioClip nextTrack))
            {
                Play(nextTrack);
            }
        }

        /// <summary>
        /// 指定された音楽クリップを即座に再生します
        /// クロスフェード効果により、既存の音楽から滑らかに切り替わります
        /// </summary>
        /// <param name="clip">再生する音楽クリップ</param>
        public void Play(AudioClip clip)
        {
            if (clip == null) return;

            // 同じクリップが既に再生中の場合は何もしない
            if (current && current.clip == clip) return;

            // 以前のAudioSourceがあれば破棄
            if (previous)
            {
                Destroy(previous);
                previous = null;
            }

            // currentをpreviousに移動（フェードアウト対象）
            previous = current;

            // 新しいAudioSourceを作成・設定（フェードイン対象）
            current = gameObject.GetOrAdd<AudioSource>();
            current.clip = clip;
            current.outputAudioMixerGroup = musicMixerGroup;
            current.loop = false;                    // プレイリスト機能のため単発再生
            current.volume = 0;                      // フェードインのため0から開始
            current.bypassListenerEffects = true;   // リスナーエフェクトをバイパス
            current.Play();

            // クロスフェード開始（わずかに0より大きい値で開始）
            fading = 0.001f;
        }

        /// <summary>
        /// MonoBehaviourのUpdateメソッド
        /// クロスフェード処理と自動的な次曲再生を管理します
        /// </summary>
        void Update()
        {
            HandleCrossFade();

            // 現在の曲が終了し、プレイリストに曲がある場合は次の曲を再生
            if (current && !current.isPlaying && playlist.Count > 0)
            {
                PlayNextTrack();
            }
        }

        /// <summary>
        /// クロスフェード処理を実行します
        /// 対数フェードにより人間の聴覚に自然な音量変化を実現します
        /// </summary>
        void HandleCrossFade()
        {
            // フェード中でない場合は処理をスキップ
            if (fading <= 0f) return;

            // フェード時間を経過時間で更新
            fading += Time.deltaTime;

            // フェード進行率を0-1の範囲で計算
            float fraction = Mathf.Clamp01(fading / crossFadeTime);

            // 対数フェードで自然な音量変化を実現
            float logFraction = fraction.ToLogarithmicFraction();

            // 前の曲をフェードアウト、現在の曲をフェードイン
            if (previous) previous.volume = 1.0f - logFraction;
            if (current) current.volume = logFraction;

            // フェード完了時の処理
            if (fraction >= 1)
            {
                fading = 0.0f;  // フェード処理終了

                // 前のAudioSourceを破棄してリソース解放
                if (previous)
                {
                    Destroy(previous);
                    previous = null;
                }
            }
        }
    }
}