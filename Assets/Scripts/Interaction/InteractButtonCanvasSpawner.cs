using UnityEngine;

/// <summary>
/// 挂载在 Trigger 物体上，玩家进入时在 Trigger 上方生成 buttonCanvas 预制体。
/// 由 DiggingTrigger、AssemblyPlatform、DialogueInteractTrigger 等在 OnTriggerEnter/Exit 时调用。
/// </summary>
public class InteractButtonCanvasSpawner : MonoBehaviour
{
    [Header("预制体")]
    [Tooltip("交互按钮 Canvas 预制体，需挂载 InteractButtonCanvas 组件")]
    [SerializeField] private GameObject buttonCanvasPrefab;

    [Header("位置")]
    [Tooltip("Canvas 相对于 Trigger 物体上方的偏移（世界单位）")]
    [SerializeField] private float offsetY = 0.5f;

    private GameObject _instance;

    /// <summary> 在 Trigger 上方生成 buttonCanvas，若已有实例则先销毁 </summary>
    public void Show()
    {
        Hide();
        if (buttonCanvasPrefab == null)
            return;

        _instance = Instantiate(buttonCanvasPrefab, transform);
        _instance.transform.localPosition = new Vector3(0f, offsetY, 0f);
        _instance.transform.localRotation = Quaternion.identity;
        _instance.transform.localScale = Vector3.one;
        _instance.SetActive(true);
    }

    /// <summary> 销毁已生成的 buttonCanvas </summary>
    public void Hide()
    {
        if (_instance != null)
        {
            Destroy(_instance);
            _instance = null;
        }
    }
}
