using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 静态工具类：通过 itemName 查找 ItemDataSO 引用。
/// 前提：ItemDataSO 资产需要放在 Assets/Resources/Items/ 目录下。
/// </summary>
public static class ItemLookup
{
    static Dictionary<string, ItemDataSO> _cache;

    static void EnsureCache()
    {
        if (_cache != null) return;
        _cache = new Dictionary<string, ItemDataSO>();
        var all = Resources.LoadAll<ItemDataSO>("Items");
        foreach (var item in all)
        {
            if (string.IsNullOrEmpty(item.itemName)) continue;
            if (_cache.ContainsKey(item.itemName))
            {
                Debug.LogWarning($"[ItemLookup] 重复 itemName: {item.itemName}，后者覆盖前者");
            }
            _cache[item.itemName] = item;
        }
        if (_cache.Count == 0)
            Debug.LogWarning("[ItemLookup] 未找到任何 ItemDataSO。请确保资产位于 Assets/Resources/Items/ 目录下。");
    }

    /// <summary>通过 itemName 获取 ItemDataSO，找不到返回 null。</summary>
    public static ItemDataSO Get(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        EnsureCache();
        _cache.TryGetValue(itemName, out var item);
        return item;
    }

    /// <summary>强制重建缓存（例如热更资源后调用）。</summary>
    public static void Rebuild()
    {
        _cache = null;
        EnsureCache();
    }
}
