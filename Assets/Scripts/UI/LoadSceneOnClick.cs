using UnityEngine;
using UnityEngine.SceneManagement;
using Code.Scripts;

/// <summary>
/// 挂到按钮上：点击后加载指定场景。
/// 用于 OpeningStory 等场景里的「Start」按钮，由玩家点击后再进入游戏场景。
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Button))]
public class LoadSceneOnClick : MonoBehaviour
{
    [Tooltip("要加载的场景名，如 HomeScene")]
    [SerializeField] private string sceneName = "HomeScene";

    /// <summary>供 Button 的 OnClick 列表调用。若有 SceneTransitionManager 则走带进度条的过渡，否则直接加载。</summary>
    public void OnClick()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[LoadSceneOnClick] 未设置 sceneName");
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(sceneName);
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
