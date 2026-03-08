using UnityEngine;

/// <summary>
/// 挂在宝箱 UI 面板根节点上。面板被关闭（含 ESC）时恢复玩家移动。
/// </summary>
public class ChestPanelHelper : MonoBehaviour
{
    private void OnDisable()
    {
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.EnableMove();
    }
}
