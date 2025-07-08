using UnityEngine;

namespace SCLib_SoundSystem
{
    /// <summary>
    /// オーディオ関連の拡張メソッドを提供するクラス
    /// 音量の対数変換やフェード効果に関する機能を提供します
    /// </summary>
    public static class AudioExtensions
    {
        /// <summary>
        /// 音量スライダーの位置を表すfloat値を対数音量に変換します
        /// これにより、音量スライダーを動かした際により滑らかで自然な音響進行を得ることができます
        /// 
        /// 数学的処理の内容:
        /// - スライダー値が0.0001以上であることを保証し、対数関数に0が渡されることを防ぎます
        /// - スライダー値の常用対数（底10）を取得します
        /// - 結果に20を掛けます。音響工学では、dBスケールでの1単位の変化は、
        ///   人間の耳が知覚する音量の倍増または半減にほぼ相当するため、20倍します
        /// 
        /// このメソッドは、UnityのAudio Mixerで使用されるUI音量スライダーの正規化に便利です
        /// </summary>
        /// <param name="sliderValue">音量スライダーの値（0.0～1.0の範囲）</param>
        /// <returns>対数スケールに変換された音量値（dB単位）</returns>
        /// <example>
        /// 使用例:
        /// <code>
        /// float sliderValue = 0.5f; // 50%の位置
        /// float dbValue = sliderValue.ToLogarithmicVolume();
        /// audioMixer.SetFloat("MasterVolume", dbValue);
        /// </code>
        /// </example>
        public static float ToLogarithmicVolume(this float sliderValue)
        {
            return Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20;
        }

        /// <summary>
        /// [0, 1]の範囲の数値を、人間の音量知覚を模倣した対数スケール（同じく[0, 1]の範囲）に変換します
        /// 人間の音量知覚は対数的であるため、この変換により自然なフェード効果を実現できます
        /// 
        /// 数学的処理の内容:
        /// - Log10関数内で、元の数値の9倍に1を加えてから対数を取ります
        ///   これにより、数値が対数曲線に滑らかにスケールされ、[0, 1]の範囲に収まります
        /// - 補間された数値の常用対数（底10）を取得します
        /// - 結果をLog10(10)で割って正規化し、[0, 1]の範囲に収まることを保証します
        ///   補間後のLog10関数への入力値は1から10の間で変化するためです
        /// 
        /// このメソッドは、オーディオクリップ間の改良されたフェード効果に便利です
        /// </summary>
        /// <param name="fraction">変換する数値（0.0～1.0の範囲）</param>
        /// <returns>対数スケールに変換された数値（0.0～1.0の範囲）</returns>
        /// <example>
        /// 使用例:
        /// <code>
        /// float fadeProgress = 0.5f; // 50%の進行状況
        /// float logProgress = fadeProgress.ToLogarithmicFraction();
        /// currentTrack.volume = 1.0f - logProgress;
        /// nextTrack.volume = logProgress;
        /// </code>
        /// </example>
        public static float ToLogarithmicFraction(this float fraction)
        {
            return Mathf.Log10(1 + 9 * fraction) / Mathf.Log10(10);
        }
    }
}