# Zompanion 背包与 Slot 系统说明

本文档总结游戏中**玩家背包 (Inventory)** 及** Slot 槽位**相关功能的设计、配置方式与注意事项。

---

## 1. 功能概述

背包系统用于存储玩家拾取的物品（如挖掘获得的尸块、剧情道具等）。采用 **ScriptableObject** 存储数据，UI 通过事件驱动自动刷新。支持与**组装台**拖拽交互、**僵尸扩容**等扩展。

### 核心流程

1. 挖掘/拾取 → 调用 `InventorySO.AddItem()` 加入背包
2. 数据变化 → 触发 `OnInventoryUpdated` 事件
3. `InventoryDisplay` 订阅事件 → 调用 `UpdateDisplay()` 刷新所有槽位
4. 拖拽物品 → 在背包格、组装台槽位间移动，数据与 UI 同步更新

---

## 2. 相关脚本与组件

| 脚本/组件 | 职责 |
|-----------|------|
| `InventorySO` | 背包数据（ScriptableObject）：容量、槽位列表、添加/设置物品、扩容 |
| `InventorySlot` | 单个槽位数据结构：存储 `ItemDataSO`，提供 `IsEmpty` / `Clear` |
| `InventoryDisplay` | 背包 UI 容器：根据容量实例化槽位、订阅事件、刷新显示 |
| `InventorySlotUI` | 单个槽位 UI：接收拖拽、调用 `UpdateSlotDisplay` 刷新图标 |
| `ItemUI` | 物品图标：显示 Sprite，持有 `ItemDataSO` 引用 |
| `UIDragHandler` | 拖拽逻辑：处理从背包/组装台拖出、放回、跨区域放置 |
| `ItemDataSO` | 物品定义（ScriptableObject）：名称、图标、类型、世界预制体 |

---

## 3. 配置说明

### 3.1 InventorySO 背包配置

通过菜单 **Create → Inventory → Inventory System** 创建。

| 字段 | 类型 | 说明 |
|------|------|------|
| **Base Capacity** | `int` | 基础容量，默认 10 |
| **Extra Capacity** | `int` | 僵尸提供的额外容量，默认 0 |
| **Clear On Start** | `bool` | 游戏开始时是否清空背包（调试用） |
| **Slots** | `List<InventorySlot>` | 槽位数据，由 `Initialize()` 按容量初始化 |

**MaxCapacity**：只读属性 = `baseCapacity + extraCapacity`

### 3.2 InventoryDisplay 配置

挂载在背包 Canvas 或父物体上。

| 字段 | 类型 | 说明 |
|------|------|------|
| **Inventory Data** | `InventorySO` | 背包数据引用 |
| **Slot Prefab** | `GameObject` | 槽位预制体（需含 `InventorySlotUI`） |
| **Slots Container** | `Transform` | 槽位的父节点（通常为 Grid Layout Group） |

**工作方式**：`Start` 时若 `clearOnStart` 则清空背包，订阅 `OnInventoryUpdated`，调用 `InitializeInventoryUI()` 实例化槽位并首次刷新。

### 3.3 InventorySlotUI 配置

每个槽位预制体需包含此组件。

| 字段 | 类型 | 说明 |
|------|------|------|
| **Slot Index** | `int` | 运行时由 `UpdateSlotDisplay` 设置，对应 `inventoryData.slots[index]` |
| **Inventory Data** | `InventorySO` | 与 `InventoryDisplay` 引用同一份数据 |
| **Item Icon Prefab** | `GameObject` | 物品图标预制体（需含 `ItemUI` + `UIDragHandler` + `CanvasGroup`） |

**必须项**：槽位需实现 `IDropHandler`，且需有 Raycast Target（如 `Image`）以接收拖拽。

### 3.4 ItemUI 与物品图标预制体

物品图标预制体需包含：

- `ItemUI`：`iconImage` 引用 `Image` 组件，用于显示 `ItemDataSO.icon`
- `UIDragHandler`：处理拖拽
- `CanvasGroup`：拖拽时 `alpha = 0.6`、`blocksRaycasts = false`，保证 EndDrag 能检测到目标

### 3.5 引用链汇总

```
InventorySO (PlayerBackpack.asset)
    ↑
    ├── InventoryDisplay.inventoryData
    ├── InventorySlotUI.inventoryData（每个槽位）
    ├── PlayerInteraction.playerInventory
    ├── DiggingTrigger.Interact(playerInventory)
    └── ZombieManager.playerInventory（扩容用）
```

---

## 4. 重要注意事项

### 4.1 拖拽时的数据同步

- **背包 → 背包**：`InventorySlotUI.OnDrop` 用 `SetItemAt` 更新数据；拖拽中的图标会被 `Destroy`，`UpdateDisplay` 会在目标格生成新图标。
- **背包 → 组装台**：`UIAssemblySlot` 负责 `InsertPart`，并将来源格 `SetItemAt(slotIndex, null)`。
- **组装台 → 背包**：`InventorySlotUI.OnDrop` 会调用 `platform.RemovePart`，确保组装台逻辑与来源一致。
- **拖到无效区域**：`UIDragHandler.ReturnToOriginalSlot` 将图标放回原父节点；**背包数据在 OnBeginDrag 时未清除**，因此不需要恢复数据。

### 4.2 背包数据在拖拽时不被提前清除

`UIDragHandler.OnBeginDrag` 只对**组装台槽位**调用 `RemovePart`，对**背包槽位**不做数据修改。这样设计是为了：

- 拖到非法区域时，图标可直接回到原位，数据无需恢复。
- 拖到合法槽位时，由目标 `OnDrop` 负责 `SetItemAt` 更新，并 `Destroy` 拖拽图标。

### 4.3 一格一物，无堆叠

当前 `InventorySlot` 仅存储 `ItemDataSO`，无 `quantity`。每个槽位对应一个物品，同类物品占多个格。

### 4.4 Initialize 与容量变化

- `Initialize()` 在 `slots.Count != MaxCapacity` 时清空并重建槽位列表。
- `ExpandCapacity(amount)` 增加 `extraCapacity` 并追加空槽位，会触发 `OnInventoryUpdated`。
- 扩容后 `InventoryDisplay` 不会自动增加新槽位 UI，当前实现是在 `Start` 时按 `MaxCapacity` 一次性生成。若运行时扩容，需额外逻辑刷新槽位数量。

### 4.5 事件订阅与内存泄漏

- `InventoryDisplay` 在 `OnDestroy` 中取消订阅 `OnInventoryUpdated`。
- 若 `InventorySO` 为持久资源（如 DontDestroyOnLoad），需确保 `InventoryDisplay` 销毁时正确取消订阅。

### 4.6 Clear On Start

`clearOnStart = true` 时，每次进入场景都会清空背包，便于测试。发布版本建议设为 `false`，并配合存档系统。

### 4.7 背包满时的行为

- `AddItem` 返回 `false` 表示背包已满。
- `DiggingTrigger` 在背包满时不会移除挖掘点中的物品，玩家清理背包后可继续挖掘。

---

## 5. 数据流与工作流程

```
玩家挖掘 → DiggingTrigger.Interact(playerInventory)
                ↓
        playerInventory.AddItem(item)
                ↓
        slots 中第一个空格子写入 itemData
                ↓
        OnInventoryUpdated?.Invoke()
                ↓
        InventoryDisplay.UpdateDisplay()
                ↓
        各 InventorySlotUI.UpdateSlotDisplay(slot, index)
                ↓
        空格子：销毁子物体 ItemUI
        有物品：实例化 itemIconPrefab，SetItem(slot.itemData)
```

### 拖拽流程简图

```
OnBeginDrag
  - 组装台来源：platform.RemovePart
  - 背包来源：不改数据
  - 图标移到 Canvas 根，alpha=0.6, blocksRaycasts=false

OnDrop（落在 InventorySlotUI）
  - 格子为空：SetItemAt + Destroy 拖拽图标 + 若来自组装台则 RemovePart
  - 格子有物品：当前实现不允许（仅接受空格子）

OnDrop（落在 UIAssemblySlot）
  - 见《Assembling系统说明.md》

OnEndDrag（未落到合法目标）
  - ReturnToOriginalSlot：回父节点，组装台来源则 InsertPart 恢复
```

---

## 6. 与其他系统的对接

| 系统 | 对接方式 |
|------|----------|
| **挖掘** | `DiggingTrigger.Interact(InventorySO)` 调用 `AddItem` |
| **组装台** | 拖拽时通过 `SetItemAt` / `InsertPart` / `RemovePart` 同步 |
| **僵尸扩容** | `ZombieManager` 调用 `playerInventory.ExpandCapacity(expand)` |
| **场景掉落** | `SceneLootManager` 分配物品到挖掘点，由挖掘逻辑间接加入背包 |

---

## 7. ItemDataSO 与 ItemType

物品通过 **Create → Inventory → Item Data** 创建，包含：

- `itemName`、`description`、`icon`、`itemType`
- `worldPrefab`：丢弃到场景时生成的物体（当前未实现丢弃逻辑）

`ItemType` 枚举：`General`、`Head`、`Torso`、`Arm`、`Leg`、`StoryProp`。组装台仅接受 `Torso`、`Arm`、`Leg`。

---

## 8. 预制体结构参考

```
SlotUICanvas
└── InventoryDisplay
    └── SlotsContainer (Grid Layout Group)
        └── Slot_Prefab (× MaxCapacity)
            ├── Image（槽位背景）
            └── InventorySlotUI
                └── [运行时] ItemIconPrefab 实例
                    ├── ItemUI
                    ├── UIDragHandler
                    ├── CanvasGroup
                    └── Image（物品图标）
```

---

## 9. 扩展建议

- 实现堆叠：在 `InventorySlot` 增加 `quantity`，`AddItem` 时尝试合并同类物品。
- 运行时扩容 UI：`ExpandCapacity` 后动态实例化新槽位并加入 `slotUIList`。
- 丢弃功能：调用 `ItemDataSO.worldPrefab` 在场景中生成物品，并 `SetItemAt(index, null)`。
- 背包满提示：`AddItem` 返回 `false` 时弹出提示 UI。
- 存档：序列化 `InventorySO.slots` 或关键字段，读档时恢复并调用 `OnInventoryUpdated`。
