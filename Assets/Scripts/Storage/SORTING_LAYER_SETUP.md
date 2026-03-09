# 2D 层级遮挡配置说明

## 问题原因

- **Tilemap Individual 模式** 在 Unity Game View 中有已知排序 bug，与 Custom Axis 配合不稳定
- **Custom Axis + 同 Order** 依赖 Unity 内部实现，实际表现不可靠

## 方案：Chunk + 显式 Y 排序

Player 和前置层 Tilemap 都使用**基于 Y 的 sortingOrder**，与背景用固定 Order 区分。

---

## 一、层级与 Order 约定

| 用途 | Sorting Layer | Order | 说明 |
|------|---------------|-------|------|
| 背景层 | Ground 或 GroundObjects | **0 或负值**（如 -10000） | 固定，始终最底 |
| Player | GroundObjects | **YSortRenderer 动态** | baseOrder 需 > 背景 |
| 前置层 Tilemap | GroundObjects | **TilemapYSort 动态** | 与 Player 共用公式 |

---

## 二、配置步骤

### 1. 背景层 Tilemap

- Mode: **Chunk**
- Sorting Layer: Ground 或 GroundObjects
- Order in Layer: **0**（若与 Player 同 Layer，确保 Player 的 baseOrder 更大，见下）

### 2. 前置层 Tilemap（桌子、柜子、栏杆等）+ Pivot Mode

**Pivot Mode**（新增）：
- Center：Bounds 中心（默认）
- **MinY**：最下边缘 Y，L 形柜台、栏杆等用，角色在旁能正确穿插
- MaxY：最上边缘 Y

**桌子上的装饰**：单独 Tilemap + TilemapYSort，**orderOffset = 50~100**（比桌子大）

- Mode: **Chunk**
- Sorting Layer: GroundObjects
- 添加组件：**TilemapYSort**
- 参数与 YSortRenderer 保持一致：
  - `lowerYInFront = true`
  - `baseOrder = 0`（若背景为 0，改用 `baseOrder = 10000`，使前景恒在背景之上）
  - `precision = 1000`
- 可选：在 Tilemap 下加子物体作为 Sort Pivot，放到物体“交互点”（如柜子底部）

### 4. Player（MainCharacter）

- YSortRenderer，`useExplicitOrder = true`，`pivotMode = Center`（用 SortPivot 脚底）

### 5. Sprite 物体（石头、灌木等，会遮挡腿部）

- 加 **YSortRenderer**，**pivotMode = MinY**（用 Sprite 最下边缘 Y，使脚下石头/植物能遮挡腿部）
- `baseOrder`、`precision` 与 Player 一致

---

## 三、baseOrder 与背景

若背景 Order = 0，而 `order = baseOrder + round(y × precision × -1)` 在 Y 较大时可能为负，会跑到背景后面。

建议：

- 背景 Order：**-10000**
- Player / 前置：`baseOrder = 0`，`precision = 1000`  
  或
- 背景 Order：**0**
- Player / 前置：`baseOrder = 10000`，保证 order 恒 > 0

---

## 四、效果

- 背景始终最底
- Player 与前置 Tilemap 按 Y 穿插：Y 小（靠近镜头）在上，Y 大（远离镜头）在下
- 整块 Tilemap 作为单一体参与排序，避免 Individual 模式引起的错乱
