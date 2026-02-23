using UnityEngine;

/// <summary>
/// 挂在关卡结束区域物体上（带 Collider 且 Is Trigger = true）。
/// 玩家（Tag "Player"）进入后通知当前场景的 LevelSceneManager 进入结束流程。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class LevelEndTrigger : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[LevelEndTrigger] 玩家进入结束区域");
        var manager = FindObjectOfType<GameSceneManager>();
        if (manager != null)
            manager.OnPlayerReachedEnd();
        else
            Debug.LogWarning("[LevelEndTrigger] 场景中未找到 GameSceneManager");
    }
}
