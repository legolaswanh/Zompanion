using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在需要保存状态的 GameObject 上。提供唯一 GUID，负责收集/恢复同物体上所有 ISaveable 组件的状态。
/// </summary>
public class SaveableEntity : MonoBehaviour
{
    [SerializeField] private string uniqueId;

    public string UniqueId => uniqueId;

    /// <summary>遍历同 GameObject 上所有 ISaveable，收集各自的状态 JSON。Key = 组件类型名。</summary>
    public Dictionary<string, string> CaptureAll()
    {
        var stateMap = new Dictionary<string, string>();
        foreach (var saveable in GetComponents<ISaveable>())
        {
            string typeName = saveable.GetType().Name;
            stateMap[typeName] = saveable.CaptureState();
        }
        return stateMap;
    }

    /// <summary>按组件类型名匹配，将对应的 JSON 传给 ISaveable.RestoreState。</summary>
    public void RestoreAll(Dictionary<string, string> stateMap)
    {
        foreach (var saveable in GetComponents<ISaveable>())
        {
            string typeName = saveable.GetType().Name;
            if (stateMap.TryGetValue(typeName, out string json))
                saveable.RestoreState(json);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
            Debug.Log($"[SaveableEntity] 自动生成 ID: {uniqueId} ({gameObject.name})");
        }
    }

    [ContextMenu("重新生成 ID")]
    private void RegenerateId()
    {
        uniqueId = System.Guid.NewGuid().ToString();
        Debug.Log($"[SaveableEntity] 已重新生成 ID: {uniqueId} ({gameObject.name})");
    }
#endif
}
