using UnityEngine;

/// <summary>
/// 通用 ISaveable：保存/恢复 GameObject 的 activeSelf 状态。
/// 适用于被 Timeline、脚本等在运行时切换 active/inactive 的物体。
/// 配合 SaveableEntity 使用。
/// </summary>
public class GameObjectStateSaveable : MonoBehaviour, ISaveable
{
    [System.Serializable]
    class State
    {
        public bool isActive;
    }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new State { isActive = gameObject.activeSelf });
    }

    public void RestoreState(string stateJson)
    {
        var state = JsonUtility.FromJson<State>(stateJson);
        gameObject.SetActive(state.isActive);
    }
}
