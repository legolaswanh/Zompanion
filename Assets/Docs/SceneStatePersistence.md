# 场景状态持久化方案

> 最后更新：2026-02-24

## 1. 问题描述

当前所有场景切换使用 `LoadSceneMode.Single`，旧场景的全部 GameObject 在切换时被销毁。  
当玩家从 HomeScene 前往 ExplorationScene 再返回 HomeScene 时，挖掘点、组装台、NPC 对话等状态全部重置。

**受影响的系统：**

| 系统 | 丢失的状态 |
|------|-----------|
| DiggingTrigger | `isDug`（是否已挖完）、`assignedItems`（坑里剩余物品）、`gameObject.activeSelf` |
| AssemblyPlatform | `currentTorso` / `currentArm` / `currentLeg`（台上的部件） |
| SceneLootManager | 每次加载都重新随机分配物品，已挖过的坑会被重新填满 |
| DialogueInteractTrigger | `isTalking`（是否已触发过对话） |
| 未来新增的可交互物体 | 任意运行时可变状态 |

**目标：**
- 会话内自动保持：场景来回切换时状态不丢失（内存级）
- 存档到磁盘：退出游戏后再进来也能恢复（结合 SaveSystem）

---

## 2. 架构设计

### 2.1 核心组件

```
ISaveable（接口）
    ↑ 实现
DiggingTrigger / AssemblyPlatform / SceneLootManager / DialogueInteractTrigger / ...
    ↑ 挂在同一 GameObject
SaveableEntity（MonoBehaviour，提供唯一 GUID）
    ↑ 注册到
SceneStateManager（单例，DontDestroyOnLoad，内存字典）
    ↔ 序列化/反序列化
SaveSystem（静态类，JSON 写入磁盘）
```

### 2.2 数据结构

```
SceneStateManager 内存结构:
Dictionary<string sceneName, List<EntitySaveData>>

EntitySaveData:
├── string entityId          (SaveableEntity 的 GUID)
├── string componentType     (ISaveable 实现类的类型名)
└── string stateJson         (该组件状态的 JSON 字符串)
```

### 2.3 流程

#### 场景卸载前（捕获）

```
SceneTransitionManager.LoadSceneWithTransition()
  或 GameManager.LoadScene()
    │
    ▼
SceneStateManager.CaptureCurrentSceneState()
    │
    ├── 找到当前场景所有 SaveableEntity
    ├── 对每个 SaveableEntity，遍历其上所有 ISaveable 组件
    ├── 调用 ISaveable.CaptureState() 获得状态对象
    ├── 用 JsonUtility.ToJson() 序列化为字符串
    └── 存入 Dictionary[currentSceneName]
```

#### 场景加载后（恢复）

```
SceneManager.sceneLoaded 事件
    │
    ▼
SceneStateManager.RestoreSceneState(scene.name)
    │
    ├── 查 Dictionary 中是否有该场景数据
    ├── 找到新场景所有 SaveableEntity
    ├── 按 GUID 匹配已保存的 EntitySaveData
    ├── 用 JsonUtility.FromJsonOverwrite() 反序列化
    └── 调用 ISaveable.RestoreState(object)
```

#### 存档到磁盘

```
SaveSystem.Save()
    │
    ├── 先调用 SceneStateManager.CaptureCurrentSceneState()
    ├── 调用 SceneStateManager.SerializeAllToJson() 得到完整 JSON
    ├── 写入 SaveData.sceneStatesJson
    └── 写入 save.json
```

#### 读档

```
SaveSystem.Load()
    │
    ├── 读取 SaveData
    ├── 调用 SceneStateManager.DeserializeAllFromJson(data.sceneStatesJson)
    └── 后续场景加载时 RestoreSceneState 自动生效
```

---

## 3. 新建文件清单

### 3.1 ISaveable.cs

**路径：** `Assets/Scripts/Save/ISaveable.cs`

```csharp
public interface ISaveable
{
    /// <summary>捕获当前状态，返回一个 [Serializable] 对象。</summary>
    object CaptureState();

    /// <summary>从保存的状态恢复。参数类型与 CaptureState 返回的一致。</summary>
    void RestoreState(object state);
}
```

**要点：**
- 返回的对象必须标记 `[System.Serializable]` 以支持 JsonUtility
- 不要在状态对象中直接引用 UnityEngine.Object（如 ScriptableObject），用字符串 ID 代替

### 3.2 SaveableEntity.cs

**路径：** `Assets/Scripts/Save/SaveableEntity.cs`

```csharp
public class SaveableEntity : MonoBehaviour
{
    [SerializeField] private string uniqueId;

    public string UniqueId => uniqueId;

    // Editor 下自动生成 GUID（OnValidate 或 Inspector 按钮）
    // 运行时：CaptureAll() / RestoreAll() 遍历同 GameObject 上所有 ISaveable
}
```

**要点：**
- `uniqueId` 在场景内必须唯一，建议用 GUID
- 提供 `OnValidate()` 自动生成，避免手动输入出错
- 一个 GameObject 可以有多个 ISaveable 组件，用组件类型名区分

### 3.3 SceneStateManager.cs

**路径：** `Assets/Scripts/Save/SceneStateManager.cs`

- 单例，DontDestroyOnLoad
- 由 GameManager.Awake() 中 `EnsureSceneStateManagerExists()` 保证存在
- 核心 API：
  - `CaptureCurrentSceneState()` — 在场景卸载前调用
  - `RestoreSceneState(string sceneName)` — 在 sceneLoaded 中调用
  - `SerializeAllToJson()` → `string` — 供 SaveSystem 存档
  - `DeserializeAllFromJson(string json)` — 供 SaveSystem 读档
  - `ClearAllStates()` — 新游戏时清空

### 3.4 ItemLookup.cs

**路径：** `Assets/Scripts/Save/ItemLookup.cs`

- 静态工具类，用于将 `itemName` 字符串还原为 `ItemDataSO` 引用
- 启动时用 `Resources.LoadAll<ItemDataSO>("Items")` 构建查找表
- **前提：** 需要将 `Assets/ScriptableObjects/Inventory/` 下的 ItemDataSO 移到 `Assets/Resources/Items/`

---

## 4. 修改文件清单

### 4.1 DiggingTrigger.cs

**路径：** `Assets/Scripts/AreaExploration/DiggingTrigger.cs`  
**改动：** 实现 `ISaveable` 接口

**保存的状态：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `isDug` | bool | 是否已被挖完 |
| `assignedItemNames` | List\<string\> | 坑里剩余物品的 `itemName` 列表 |
| `isActive` | bool | `gameObject.activeSelf`（挖完后会 SetActive(false)） |
| `colliderEnabled` | bool | 碰撞体是否启用 |

**状态数据类示例：**
```csharp
[System.Serializable]
public class DiggingTriggerState
{
    public bool isDug;
    public List<string> assignedItemNames;
    public bool isActive;
    public bool colliderEnabled;
}
```

**RestoreState 注意事项：**
- 恢复时需要用 `ItemLookup` 将 `assignedItemNames` 还原为 `List<ItemDataSO>`
- 如果 `isDug == true`，需要切换精灵为 `dugSprite` 并禁用碰撞体
- 如果 `isActive == false`，需要 `gameObject.SetActive(false)`

### 4.2 AssemblyPlatform.cs

**路径：** `Assets/Scripts/Assembling/AssemblyPlatform.cs`  
**改动：** 实现 `ISaveable` 接口

**保存的状态：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `currentTorsoName` | string | 躯干部件的 `itemName`，null 表示空 |
| `currentArmName` | string | 手臂部件的 `itemName` |
| `currentLegName` | string | 腿部部件的 `itemName` |

**状态数据类示例：**
```csharp
[System.Serializable]
public class AssemblyPlatformState
{
    public string currentTorsoName;
    public string currentArmName;
    public string currentLegName;
}
```

**RestoreState 注意事项：**
- 恢复后需要通知 UI 刷新（触发 `OnAssemblyCleared` 或类似事件）

### 4.3 SceneLootManager.cs

**路径：** `Assets/Scripts/Manager/SceneLootManager.cs`  
**改动：** 实现 `ISaveable` 接口

**保存的状态：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `hasDistributed` | bool | 是否已经分配过物品 |

**状态数据类示例：**
```csharp
[System.Serializable]
public class SceneLootManagerState
{
    public bool hasDistributed;
}
```

**关键逻辑变更：**
- `Start()` 中在调用 `DistributeLoot()` 之前，检查 `SceneStateManager` 是否有该场景的已保存状态
- 如果有 → 跳过分配（各 DiggingTrigger 会各自从自己的 `RestoreState` 恢复物品列表）
- 如果没有 → 正常执行 `DistributeLoot()`

### 4.4 DialogueInteractTrigger.cs

**路径：** `Assets/Scripts/Interaction/DialogueInteractTrigger.cs`  
**改动：** 实现 `ISaveable` 接口

**保存的状态：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `isTalking` | bool | 是否已触发过对话 |

**状态数据类示例：**
```csharp
[System.Serializable]
public class DialogueInteractTriggerState
{
    public bool isTalking;
}
```

### 4.4b DialogueTriggerSaveable.cs（新建伴随组件）

**路径：** `Assets/Scripts/Save/DialogueTriggerSaveable.cs`  
**用途：** 保存同 GameObject 上所有 Pixel Crushers `DialogueSystemTrigger` 的 `enabled` 状态。  
适用于一次性对话触发器：触发后被禁用，切场景再回来时保持已禁用。

**保存的状态：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `triggerEnabled` | List\<bool\> | 每个 DialogueSystemTrigger 的 enabled 状态（按 GetComponents 顺序） |
| `gameObjectActive` | bool | 物体本身是否激活 |

**使用方式：** 在有 `DialogueSystemTrigger` 的 GameObject 上添加 `DialogueTriggerSaveable` + `SaveableEntity`。  
无需修改 Pixel Crushers 插件代码。

### 4.5 SaveSystem.cs

**路径：** `Assets/Scripts/Save/SaveSystem.cs`  
**改动：** 扩展 `SaveData` 类

**新增字段：**
```csharp
[Serializable]
public class SaveData
{
    public int version;
    public string sceneName;
    public long saveTimeTicks;
    public string sceneStatesJson;  // ← 新增：SceneStateManager 序列化的所有场景状态
}
```

**Save() 改动：**
```csharp
// 在构建 SaveData 时
if (SceneStateManager.Instance != null)
{
    SceneStateManager.Instance.CaptureCurrentSceneState();
    data.sceneStatesJson = SceneStateManager.Instance.SerializeAllToJson();
}
```

**Load() 改动：**
```csharp
// 读取后注入内存
if (SceneStateManager.Instance != null && !string.IsNullOrEmpty(data.sceneStatesJson))
{
    SceneStateManager.Instance.DeserializeAllFromJson(data.sceneStatesJson);
}
```

### 4.6 GameManager.cs

**路径：** `Assets/Scripts/Manager/GameManager.cs`  
**改动：**

1. `Awake()` 中新增 `EnsureSceneStateManagerExists()`
2. `LoadScene()` 中在 `SceneManager.LoadScene()` 之前调用：
   ```csharp
   SceneStateManager.Instance?.CaptureCurrentSceneState();
   ```

### 4.7 SceneTransitionManager.cs

**路径：** `Assets/Scripts/SceneTransition/SceneTransitionManager.cs`  
**改动：**

在 `LoadSceneWithTransitionCoroutine()` 中，`LoadSceneAsync()` 之前调用：
```csharp
if (SceneStateManager.Instance != null)
    SceneStateManager.Instance.CaptureCurrentSceneState();
```

---

## 5. ItemDataSO 资产迁移

当前 ItemDataSO 资产位于 `Assets/ScriptableObjects/Inventory/`，不在 Resources 目录下。

**需要迁移的文件：**
```
Assets/ScriptableObjects/Inventory/ZombieArm_normal.asset
Assets/ScriptableObjects/Inventory/Mysterious Book.asset
Assets/ScriptableObjects/Inventory/ZombieArm_story_01.asset
Assets/ScriptableObjects/Inventory/Trash_01.asset
Assets/ScriptableObjects/Inventory/ZombieTroso_story_01.asset
Assets/ScriptableObjects/Inventory/ZombieLeg_story_01.asset
Assets/ScriptableObjects/Inventory/StoryProp_01.asset
Assets/ScriptableObjects/Inventory/ZombieLeg_normal.asset
Assets/ScriptableObjects/Inventory/ZombieTroso_normal.asset
```

**目标路径：** `Assets/Resources/Items/`

> **注意：** `PlayerBackpack.asset` 是 InventorySO 类型，不是 ItemDataSO，不需要迁移。

迁移后 `ItemLookup.cs` 使用 `Resources.LoadAll<ItemDataSO>("Items")` 即可获取全部物品。

---

## 6. Inspector 配置步骤

对每个需要保存状态的场景 GameObject：

1. 在 Inspector 中点击 "Add Component"，添加 `SaveableEntity`
2. 如果 `uniqueId` 为空，点击 "Generate ID" 按钮（或保存场景时 OnValidate 自动生成）
3. 确认该 GameObject 上的脚本已实现 `ISaveable` 接口

**当前需要添加 SaveableEntity 的物体：**

| 场景 | GameObject | 上面的 ISaveable |
|------|-----------|-----------------|
| HomeScene / ExplorationScene | 每个 DiggingTrigger 挖掘点 | DiggingTrigger |
| HomeScene | AssemblyPlatform 组装台 | AssemblyPlatform |
| HomeScene | 每个 NPC（带 DialogueInteractTrigger） | DialogueInteractTrigger |
| 所有场景 | 每个带 DialogueSystemTrigger 的物体（一次性对话等） | DialogueTriggerSaveable |

---

## 7. 扩展指南：新增可保存系统

当未来有新的可交互物体需要保存状态时，按以下步骤操作：

### 步骤 1：定义状态数据类

在你的脚本文件中（或单独文件）定义一个 `[System.Serializable]` 的状态类：

```csharp
[System.Serializable]
public class MyObjectState
{
    public bool someFlag;
    public int someCounter;
    public string someItemName; // 用 string 代替 ScriptableObject 引用
}
```

**序列化规则：**
- 基本类型（bool, int, float, string）直接存
- `List<T>`（T 为基本类型或 Serializable 类）直接存
- `ScriptableObject` 引用 → 存其 `itemName` 或唯一标识字符串，恢复时用 `ItemLookup` 查找
- `GameObject` / `Transform` 引用 → 不保存，恢复时重新 Find 或通过其他方式定位
- `Vector3` / `Quaternion` → 可以直接存（JsonUtility 支持）

### 步骤 2：实现 ISaveable 接口

```csharp
public class MyInteractableObject : MonoBehaviour, ISaveable
{
    private bool someFlag;
    private int someCounter;

    public object CaptureState()
    {
        return new MyObjectState
        {
            someFlag = this.someFlag,
            someCounter = this.someCounter
        };
    }

    public void RestoreState(object state)
    {
        var s = (MyObjectState)state;
        this.someFlag = s.someFlag;
        this.someCounter = s.someCounter;
        // 根据恢复的数据刷新视觉表现（精灵、UI、碰撞体等）
    }
}
```

### 步骤 3：在 Inspector 中添加 SaveableEntity

在该 GameObject 上添加 `SaveableEntity` 组件，确保生成了唯一 ID。

### 步骤 4：（可选）如果涉及 ScriptableObject 引用

如果你的状态中需要引用新的 ScriptableObject 类型：
1. 将该类型的 .asset 文件放入 `Assets/Resources/` 下的某个子目录
2. 在 `ItemLookup.cs` 中添加对应的查找方法（或创建新的查找工具类）

---

## 8. 关键设计决策

| 决策 | 选择 | 原因 |
|------|------|------|
| 状态标识 | SaveableEntity + GUID | 比 GameObject 名称更稳定，支持场景中同名物体 |
| 序列化格式 | JsonUtility + JSON string | 与现有 SaveSystem 一致，Unity 原生支持，无需额外依赖 |
| 捕获时机 | 场景切换前显式调用 | 比 OnDestroy 更可控，避免销毁顺序问题 |
| ItemDataSO 引用 | 存 itemName + Resources 查找 | ScriptableObject 不能直接 JSON 序列化，用名字做 key 简单可靠 |
| SceneLootManager | 恢复时跳过 DistributeLoot | 各 DiggingTrigger 独立恢复自己的物品，无需重新分配 |
| 内存 + 磁盘双层 | 内存字典用于会话内，存档时一并写入 JSON | 会话内零开销，存档时才涉及 IO |

---

## 9. 注意事项

1. **GUID 唯一性**：同一场景中不能有两个 SaveableEntity 使用相同的 `uniqueId`。如果复制 GameObject，必须重新生成 ID。

2. **场景名作为 Key**：内存字典以场景名为 key。如果重命名场景，旧的保存数据将无法匹配。

3. **新游戏清空**：开始新游戏时，必须调用 `SceneStateManager.ClearAllStates()` 清除上一次游戏的状态缓存。

4. **Additive 场景**：当前 Persistent 场景以 Additive 方式加载首场景。CaptureCurrentSceneState 应只捕获活动场景（`SceneManager.GetActiveScene()`）中的 SaveableEntity，不要误捕 Persistent / DontDestroyOnLoad 中的对象。

5. **RestoreState 顺序**：场景加载后，SceneStateManager 的恢复应在 `SceneLootManager.Start()` 中的 `DistributeLoot()` 之前执行。可以通过 Script Execution Order 或在 SceneLootManager 中延迟一帧检查来解决。

6. **Pixel Crushers Dialogue System**：当前项目使用了 Dialogue System 插件，其 Lua 变量（如 `hasBook`、`hasFirstArm`）也是运行时状态。后续如需保存对话系统状态，可使用 Dialogue System 自带的 `SaveSystem` / `DialogueSystemSaver` 组件，或将 Lua 变量纳入自定义保存逻辑。
