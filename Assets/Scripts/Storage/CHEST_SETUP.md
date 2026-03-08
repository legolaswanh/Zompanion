# 宝箱功能配置说明

## 预制体搭建步骤

### 1. 宝箱 UI 面板（ChestPanel）

在 Canvas 下创建子物体，或新建一个 Canvas：

1. **根节点**：添加 `ChestPanelHelper`、`Canvas`（如需要）、`GraphicRaycaster`
2. **StorageDisplay 容器**：添加 `StorageDisplay` 组件
   - **预生成格子模式**：勾选 Use Existing Slots，Slots Container 指向已有格子父物体（如 ChestImage/GameObject），Slot Prefab 留空。每个格子（Inventory_slot）需挂载 `InventorySlotUI` 并配置 `itemIconPrefab`（Item_Prefab），`iconSizeRatio` 可设为 0.85 使物品图标为格子大小的 85%。
   - **动态生成模式**：不勾选 Use Existing Slots，Slot Prefab 拖入格子预制体，Slots Container 为格子容器。
   - Panel Root：拖入本面板根节点
   - Use Panel Coordinator：勾选（支持 ESC 关闭）
3. 默认设为 **SetActive(false)**，运行时由宝箱触发打开

### 2. 宝箱触发器（Chest Prefab）

1. 创建空物体，Tag 设为 **Chest**
2. 添加 **BoxCollider2D** 或 **CircleCollider2D**，勾选 Is Trigger
3. 添加 **ChestInteractTrigger** 组件
   - Capacity：宝箱格子数，需与预生成格子数量一致（如 10 个格子则填 10）
   - Chest Panel：拖入上述 UI 面板的 Canvas（可选，用于自动查找 StorageDisplay）
   - Storage Display：拖入 StorageDisplay 所在物体
4. 添加 **InteractButtonCanvasSpawner** 组件
   - Button Canvas Prefab：拖入 `InteractButtonCanvas.prefab`
5. 添加 **SaveableEntity** 组件（用于存档宝箱内容）
6. 添加宝箱视觉（Sprite 等）

### 3. 场景放置

将宝箱预制体放入场景，确保 Player 的 Tag 为 "Player"，进入 Trigger 范围后按 E 即可打开。
