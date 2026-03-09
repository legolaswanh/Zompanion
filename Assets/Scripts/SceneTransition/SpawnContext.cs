using UnityEngine;

/// <summary>
/// 跨场景临时存储：场景切换时记录目标出生点与玩家朝向，供 GameManager 在新场景加载后放置玩家。
/// 由 SceneExitTrigger 在切场景前设置，PlaceOrSpawnPlayerAtSpawnPoint 使用后清除。
/// </summary>
public static class SpawnContext
{
    /// <summary>是否为首进入（新游戏/读档），使用默认 SpawnPoint，不应用过渡出生点。</summary>
    public static bool IsFirstEntry { get; private set; }

    /// <summary>目标场景名（当前加载 scene 应与之匹配才使用 context）</summary>
    public static string TargetSceneName { get; private set; }

    /// <summary>目标场景中 SceneEntryPoint 的 entryPointId</summary>
    public static string EntryPointId { get; private set; }

    /// <summary>玩家进入传送前的朝向（2D 四方向，供 Animator MoveX/MoveY）</summary>
    public static Vector2 FacingDirection { get; private set; }

    /// <summary>是否有有效的 transition 出生上下文</summary>
    public static bool HasTransitionContext => !string.IsNullOrEmpty(TargetSceneName) && !string.IsNullOrEmpty(EntryPointId);

    /// <summary>
    /// 设置场景切换出生上下文。由 SceneExitTrigger 在 LoadSceneWithTransition 前调用。
    /// </summary>
    /// <param name="targetSceneName">目标场景名</param>
    /// <param name="entryPointId">目标场景内 SceneEntryPoint 的 ID</param>
    /// <param name="facingDirection">玩家进入传送前的朝向</param>
    public static void SetTransitionContext(string targetSceneName, string entryPointId, Vector2 facingDirection)
    {
        IsFirstEntry = false;
        TargetSceneName = targetSceneName;
        EntryPointId = entryPointId;
        FacingDirection = facingDirection.sqrMagnitude > 0.01f ? facingDirection.normalized : Vector2.down;
    }

    /// <summary>标记为首进入（新游戏/读档后首次进入某场景），使用默认 SpawnPoint。</summary>
    public static void SetFirstEntry()
    {
        IsFirstEntry = true;
        Clear();
    }

    /// <summary>清除出生上下文（使用完毕后调用）</summary>
    public static void Clear()
    {
        TargetSceneName = null;
        EntryPointId = null;
        FacingDirection = Vector2.down;
    }
}
