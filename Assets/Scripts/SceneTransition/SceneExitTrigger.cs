using UnityEngine;

/// <summary>
/// 场景出口 Trigger：挂在带 2D Trigger 的物体上，玩家进入后带过渡 UI 加载目标场景。
/// 每个 Trigger 独立配置目标场景名，不依赖本场景的 GameSceneManager。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneExitTrigger : MonoBehaviour
{
    [Tooltip("玩家进入后要加载的场景名")]
    [SerializeField] private string targetSceneName;

    [Tooltip("需要检测的物体 Tag")]
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[SceneExitTrigger] 未设置 targetSceneName");
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(targetSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
        }
    }
}
