# Zompanion 组装系统 (Assembling) 说明

本文档总结游戏中 **AssemblyPlatform** 及相关组装功能的设计、配置方式与注意事项。

---

## 1. 功能概述

组装系统允许玩家在**组装台 (AssemblyPlatform)** 上，通过拖拽**躯干、手臂、腿部**三种尸块部件，按照预定义配方合成不同类型的僵尸。合成成功后，僵尸将在指定位置生成。

### 核心流程

1. 玩家靠近组装台 → 显示交互按钮
2. 按 E 键或点击按钮 → 打开组装 UI
3. 从背包拖拽部位到对应槽位
4. 三个部位齐全后点击「组装」→ 匹配配方 → 生成僵尸并消耗材料

---

## 2. 相关脚本与组件

| 脚本/组件 | 职责 |
|-----------|------|
| `AssemblyPlatform` | 组装台逻辑：配方匹配、部件管理、生成僵尸 |
| `ZombieRecipeSO` | 配方数据（ScriptableObject），定义躯干+手臂+腿的组合与对应僵尸 Prefab |
| `UIAssemblySlot` | 组装槽位 UI，接收拖拽并调用 `AssemblyPlatform.InsertPart` |
| `UIDragHandler` | 拖拽逻辑，负责从背包/组装台拖拽物品 |
| `PlayerInteraction` | 玩家交互，按 E 时打开组装台 UI |

---

## 3. 配置说明

### 3.1 AssemblyPlatform 配置

在 Inspector 中需配置以下字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| **All Recipes** | `List<ZombieRecipeSO>` | 拖入所有可能用到的配方，按顺序遍历匹配 |
| **Spawn Point** | `Transform` | 僵尸生成的位置（世界坐标） |
| **Platform Canvas** | `Canvas` | 组装主界面（含槽位、组装按钮等） |
| **Button Canvas** | `Canvas` | 靠近时显示的「打开组装台」提示按钮 |

**必须项：**

- 组装台 GameObject 需添加 `BoxCollider2D`（用于 2D 触发检测）
- 组装台 GameObject 需设置 Tag 为 **`AssemblyPlatform`**（供 `PlayerInteraction` 识别）
- 玩家 GameObject 需设置 Tag 为 **`Player`**（否则 `OnTriggerEnter2D` 不会响应）

### 3.2 ZombieRecipeSO 配方配置

通过菜单 **Create → Assembling → Zombie Recipe** 创建新配方。

| 字段 | 类型 | 说明 |
|------|------|------|
| **Torso Item** | `ItemDataSO` | 所需躯干（`ItemType.Torso`） |
| **Arm Item** | `ItemDataSO` | 所需手臂（`ItemType.Arm`） |
| **Leg Item** | `ItemDataSO` | 所需腿部（`ItemType.Leg`） |
| **Zombie Prefab** | `GameObject` | 匹配成功时生成的僵尸预制体 |

**匹配规则：** 三者完全一致才视为匹配；顺序按 `allRecipes` 列表遍历，**先匹配到的配方优先**。

### 3.3 UIAssemblySlot 配置

每个槽位需配置：

| 字段 | 类型 | 说明 |
|------|------|------|
| **Acceptable Type** | `ItemType` | 该槽位只接受 `Torso` / `Arm` / `Leg` 之一 |
| **Platform** | `AssemblyPlatform` | 所属的组装台引用 |

- 每个部位类型应对应一个槽位（Torso / Arm / Leg 各一）
- 槽位需添加 `Image` 组件（作为背景，清空时会重新启用）
- 需实现 `IDropHandler`，并确保有 Raycast 接收（如 Canvas 的 Graphic Raycaster）

### 3.4 物品数据 (ItemDataSO)

用于组装的物品必须是 `ItemType` 为 `Torso`、`Arm` 或 `Leg` 的 `ItemDataSO`。  
其他类型（如 `General`、`StoryProp`）会被 `InsertPart` 拒绝。

---

## 4. 重要注意事项

### 4.1  singleton 依赖

- `AssemblyPlatform` 使用 `PlayerInteraction.Instance`、`PlayerMovement.Instance`
- 确保场景中存在 `PlayerInteraction` 与 `PlayerMovement`，且为单例

### 4.2 配方匹配顺序

- `allRecipes` 中的配方按顺序遍历，**第一个匹配的配方会被使用**
- 若存在多个配方使用相同部位组合，只有第一个会生效
- 未匹配到配方时，会输出 `"组装失败：没有匹配的配方"`，材料**不会被消耗**

### 4.3 部件消耗

- 组装成功后会调用 `ClearPlatform()`，清空所有部位
- 物品从背包拖到组装槽后，背包中对应格子会被清空
- 若未组装成功，材料仍在组装台上，可从槽位拖回背包

### 4.4 拖拽与逻辑同步

- `UIDragHandler.OnBeginDrag`：从组装槽拖出时，会调用 `platform.RemovePart` 清除逻辑数据
- 拖到无效区域时 `ReturnToOriginalSlot` 会恢复 `InsertPart`，保持逻辑与 UI 一致
- `UIAssemblySlot` 通过 `OnAssemblyCleared` 事件清空槽位显示，避免重复订阅导致的问题

### 4.5 内存与事件

- `UIAssemblySlot` 在 `OnDisable` 中取消订阅 `platform.OnAssemblyCleared`，防止内存泄漏

### 4.6 Head 部位

- 当前实现中 Head 已被注释，仅使用 Torso / Arm / Leg
- 若将来启用 Head，需同步修改 `ZombieRecipeSO`、`AssemblyPlatform` 和 `UIAssemblySlot` 相关逻辑

### 4.7 生成位置

- 僵尸在 `spawnPoint.position` 生成，使用 `Quaternion.identity`
- 确保 `spawnPoint` 在场景中的位置合理，避免生成在墙内或场景外

---

## 5. 工作流程图

```
玩家进入 Trigger (Tag: Player)
        ↓
显示 ButtonCanvas，设置 currentActiveTrigger
        ↓
玩家按 E → OpenAssemblyPlatform → PlatformCanvas 显示，禁用移动
        ↓
从背包拖拽物品到 UIAssemblySlot
        ↓
platform.InsertPart() + 更新背包数据
        ↓
三个槽位都有部件 → 点击组装
        ↓
Assemble() 遍历 allRecipes 匹配
        ↓
匹配成功 → Instantiate(zombiePrefab) → ClearPlatform() → OnAssemblyCleared
匹配失败 → 仅输出日志，不消耗材料
```

---

## 6. 现有配方示例

- `NormalZombieRecipe`：普通僵尸配方
- `StoryZombieRecipe_01`：剧情僵尸配方

配方存放在 `Assets/ScriptableObjects/Assembling/` 目录下。

---

## 7. 扩展建议

- 可考虑为「未匹配配方」增加默认失败品僵尸或材料退回逻辑
- 配方较多时，可改用 Dictionary 或按组合哈希加速查找
- 可增加配方解锁条件、稀有度等扩展字段
