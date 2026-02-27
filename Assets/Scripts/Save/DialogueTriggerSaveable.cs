using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 保存同 GameObject 上所有 DialogueSystemTrigger 的 enabled 状态。
/// 用于一次性对话触发器：触发后被禁用，切场景再回来时保持已禁用。
/// 挂在有 DialogueSystemTrigger 的 GameObject 上，配合 SaveableEntity 使用。
/// </summary>
public class DialogueTriggerSaveable : MonoBehaviour, ISaveable
{
    [System.Serializable]
    public class State
    {
        public List<bool> triggerEnabled = new();
        public bool gameObjectActive;
    }

    public string CaptureState()
    {
        var triggers = GetComponents<DialogueSystemTrigger>();
        var state = new State
        {
            gameObjectActive = gameObject.activeSelf,
            triggerEnabled = new List<bool>(triggers.Length)
        };
        foreach (var t in triggers)
            state.triggerEnabled.Add(t.enabled);
        return JsonUtility.ToJson(state);
    }

    public void RestoreState(string stateJson)
    {
        var state = JsonUtility.FromJson<State>(stateJson);
        var triggers = GetComponents<DialogueSystemTrigger>();

        for (int i = 0; i < triggers.Length && i < state.triggerEnabled.Count; i++)
            triggers[i].enabled = state.triggerEnabled[i];

        gameObject.SetActive(state.gameObjectActive);
    }
}
