using UnityEngine;

namespace SCLib_SoundSystem
{
    /// <summary>
    /// GameObjectに対する拡張メソッドを提供するクラス
    /// UnityのGameObjectの機能を拡張し、より便利なAPI を提供します
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// 指定された型のコンポーネントを取得し、存在しない場合は自動的に追加します
        /// このメソッドは、コンポーネントの存在確認と追加を一度に行うことで、
        /// コードを簡潔にし、null参照エラーを防ぎます
        /// </summary>
        /// <typeparam name="T">取得・追加するコンポーネントの型。Componentを継承している必要があります</typeparam>
        /// <param name="gameObject">対象となるGameObject</param>
        /// <returns>取得または新しく追加されたコンポーネント</returns>
        /// <example>
        /// 使用例:
        /// <code>
        /// // AudioSourceコンポーネントを取得、なければ追加
        /// AudioSource audioSource = gameObject.GetOrAdd&lt;AudioSource&gt;();
        /// 
        /// // Rigidbodyコンポーネントを取得、なければ追加
        /// Rigidbody rb = gameObject.GetOrAdd&lt;Rigidbody&gt;();
        /// </code>
        /// </example>
        public static T GetOrAdd<T>(this GameObject gameObject) where T : Component
        {
            // 既存のコンポーネントを取得を試行
            T component = gameObject.GetComponent<T>();

            // コンポーネントが存在しない場合は追加
            if (!component)
                component = gameObject.AddComponent<T>();

            return component;
        }
    }
}