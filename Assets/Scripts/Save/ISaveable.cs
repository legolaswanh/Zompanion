/// <summary>
/// 任何需要在场景卸载/加载时保持状态的 MonoBehaviour 实现此接口。
/// 配合 SaveableEntity 组件使用。
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// 捕获当前状态，返回 JSON 字符串。
    /// 建议：定义一个 [System.Serializable] 的状态类，用 JsonUtility.ToJson 序列化。
    /// </summary>
    string CaptureState();

    /// <summary>
    /// 从 JSON 字符串恢复状态。
    /// 用 JsonUtility.FromJson 反序列化为对应的状态类，然后赋值到字段并刷新视觉表现。
    /// </summary>
    void RestoreState(string stateJson);
}
