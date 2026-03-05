using UnityEngine;

/// <summary>
/// 僵尸图鉴存档桥接：将 ZombieManager 的图鉴状态（解锁僵尸、解锁剧情）接入 SaveSystem。
/// 需挂在 ZombieManager 所在 GameObject 上。SaveSystem 在存档/读档时通过本组件获取并恢复图鉴数据。
/// </summary>
public class ZombieCodexSaveable : MonoBehaviour
{
    public static ZombieCodexSaveable Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>供 SaveSystem 调用：捕获当前图鉴状态。</summary>
    public string CaptureState()
    {
        return ZombieManager.Instance != null ? ZombieManager.Instance.CaptureCodexState() : null;
    }

    /// <summary>供 SaveSystem 调用：恢复图鉴状态。</summary>
    public void RestoreState(string stateJson)
    {
        ZombieManager.Instance?.RestoreCodexState(stateJson);
    }
}
