using UnityEngine;

/// <summary>
/// 场景入口出生点：放在目标场景中，与 SceneExitTrigger 的 entryPointId 对应。
/// 玩家通过传送进入本场景时，会在该点位 + 朝向方向上的偏移处出生，避免站在传送点上。
/// </summary>
public class SceneEntryPoint : MonoBehaviour
{
    [Tooltip("入口 ID，需与来源场景中 SceneExitTrigger 的 Entry Point ID 一致")]
    [SerializeField] private string entryPointId;

    [Tooltip("左右出口时沿朝向的偏移距离")]
    [SerializeField] [Min(0f)] private float offsetX = 0.6f;
    [Tooltip("上下出口时沿朝向的偏移距离")]
    [SerializeField] [Min(0f)] private float offsetY = 0.6f;

    public string EntryPointId => entryPointId;

    /// <summary>
    /// 根据进入朝向计算实际出生位置（点位 + 朝向方向上的偏移，左右用 offsetX，上下用 offsetY）。
    /// 斜向会取主轴方向，保证只在一个轴上偏移。
    /// </summary>
    public Vector3 GetSpawnPosition(Vector2 facingDirection)
    {
        Vector3 pos = transform.position;
        Vector2 dir = ToCardinal(facingDirection);

        if (Mathf.Abs(dir.x) > 0.01f)
            pos.x += Mathf.Sign(dir.x) * offsetX;
        else if (Mathf.Abs(dir.y) > 0.01f)
            pos.y += Mathf.Sign(dir.y) * offsetY;
        return pos;
    }

    private static Vector2 ToCardinal(Vector2 v)
    {
        if (v.sqrMagnitude < 0.01f) return Vector2.down;
        float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y);
        if (ax >= ay) return new Vector2(Mathf.Sign(v.x), 0f);
        return new Vector2(0f, Mathf.Sign(v.y));
    }
}
