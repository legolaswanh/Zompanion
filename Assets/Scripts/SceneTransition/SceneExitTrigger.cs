using UnityEngine;

/// <summary>
/// 场景出口 Trigger：挂在带 2D Trigger 的物体上，玩家进入后带过渡 UI 加载目标场景。
/// 每个 Trigger 独立配置目标场景名与目标场景中的入口 ID，玩家会在目标场景对应入口点位出生，并保持进入前的朝向。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneExitTrigger : MonoBehaviour
{
    [Tooltip("玩家进入后要加载的场景名")]
    [SerializeField] private string targetSceneName;

    [Tooltip("目标场景中 SceneEntryPoint 的 Entry Point ID，留空则用 \"From\" + 当前场景名")]
    [SerializeField] private string entryPointId;

    [Tooltip("进入新场景时的出生方向（用于偏移与朝向）。设为 (0,0) 则使用玩家 LastDir")]
    [SerializeField] private Vector2 spawnDirectionOverride;

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

        string effectiveEntryId = ResolveEntryPointId();
        Vector2 facing = spawnDirectionOverride.sqrMagnitude > 0.01f
            ? spawnDirectionOverride.normalized
            : (PlayerMovement.Instance != null ? PlayerMovement.Instance.LastDir() : Vector2.down);
        SpawnContext.SetTransitionContext(targetSceneName, effectiveEntryId, facing);

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(targetSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
        }
    }

    private string ResolveEntryPointId()
    {
        if (!string.IsNullOrEmpty(entryPointId)) return entryPointId;
        var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return "From" + (string.IsNullOrEmpty(current.name) ? "Unknown" : current.name);
    }
}
