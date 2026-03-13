# Zombie System Plan

## 1. 目标与范围
- 目标：在当前项目中落地可玩的僵尸系统，支撑「组装 -> 生成僵尸 -> 跟随/管理 -> 提供增益」闭环。
- 时间节点：
  - 2026-02-23：第一次测试可玩版本
  - 2026-03-20：最终提交版本
- 本计划优先实现 MVP，并预留简单有趣扩展功能。

---

## 2. MVP 功能定义

### 2.1 僵尸实体与信息
- 每只僵尸具备：
  - `ID`
  - `Name`
  - `Type`（普通/特殊）
  - `Category`（如和尚、官员等图鉴分类）
  - `BodyParts`（身子/手/脚来源）
  - `BuffType`（背包扩容/掉落增益等）

### 2.2 生成与持有
- 玩家在组装台提交部位后，生成僵尸数据与实例。
- MVP 支持 2 种僵尸原型（1 普通 + 1 特殊）。
- 僵尸加入“玩家持有列表”，可被设置为跟随或工作状态。

### 2.3 跟随系统（已确认：贪吃蛇）
- 跟随上限：2 只。
- 规则：严格贪吃蛇队列跟随。
  - 玩家为队头。
  - 第 1 只僵尸跟随玩家历史轨迹点。
  - 第 2 只僵尸跟随第 1 只僵尸历史轨迹点。
- 不做复杂寻路，MVP 采用轨迹点插值方案（开发快、效果稳定）。

### 2.4 管理界面（MVP）
- 面板列表：展示当前僵尸、类型、状态、基础增益。
- 操作：
  - 切换跟随/非跟随
  - 设置工作状态（占位，先做状态切换与UI反馈）
  - 查看基础信息

### 2.5 增益效果（MVP）
- 背包僵尸：增加背包容量（对接 `InventorySO.ExpandCapacity`）
- 挖掘僵尸：增加目标掉落概率（对接 `SceneLootManager` 后续加权接口）

### 2.6 图鉴与故事（MVP）
- 图鉴列表 + 基础详情页（名称、分类、解锁状态、简述）。
- 至少 1 条僵尸故事可通过提交道具或生成僵尸触发解锁。

---

## 3. 架构设计（脚本框架）

## 3.1 建议目录
在 `Assets/Scripts/ZombieSystem/` 下建立：
- `Data/`
- `Runtime/`
- `Follow/`
- `UI/`
- `Story/`

## 3.2 数据层（ScriptableObject + Runtime Data）

### ScriptableObject
- `ZombieDefinitionSO`
  - 基础配置：名称、类型、分类、立绘/Prefab、动画配置
  - 需求配置：所需部位组合
  - 增益配置：BuffType + 数值
  - 图鉴配置：描述、故事ID

- `ZombieBuffConfigSO`
  - 不同 buff 的统一参数配置（便于后续平衡）

### Runtime Data
- `ZombieInstanceData`（可序列化类）
  - `InstanceId`
  - `DefinitionId`
  - `State`（Idle/Following/Working）
  - `UnlockTime`

## 3.3 运行时系统

- `ZombieManager`（核心总管，单例或场景级）
  - 持有所有僵尸实例
  - 对外提供增删改查、状态切换
  - 广播事件给 UI 与其他系统

- `ZombieSpawnService`
  - 接收组装结果，创建 `ZombieInstanceData`
  - 生成或绑定场景中的僵尸 GameObject

- `ZombieBuffService`
  - 统一应用/回收僵尸增益
  - 与 `InventorySO`、探索掉落系统解耦

- `ZombieFollowController`
  - 管理跟随队列（最多2只）
  - 维护轨迹点缓存与插值移动

- `ZombieWorkService`（MVP 占位）
  - 先完成状态切换与事件回调
  - 最终版再接具体岗位产出逻辑

## 3.4 UI 层
- `ZombiePanelController`
  - 列表渲染、筛选、选中项
- `ZombieEntryView`
  - 单个僵尸条目（头像/名称/状态/按钮）
- `ZombieDetailPanel`
  - 详情 + 跟随/工作切换
- `ZombieCodexPanel`
  - 图鉴解锁与故事展示

---

## 4. 关键流程设计

### 4.1 组装生成流程
1. 组装台提交部位组合。
2. 查询 `ZombieDefinitionSO` 匹配规则。
3. `ZombieSpawnService` 创建实例数据并入库。
4. `ZombieBuffService` 按配置应用初始增益（如背包+格）。
5. UI 刷新并触发“新图鉴解锁”提示。

### 4.2 跟随切换流程
1. 玩家在管理面板点击“设为跟随”。
2. `ZombieManager` 校验跟随上限（2只）。
3. `ZombieFollowController` 入队并分配跟随索引。
4. 运行时按贪吃蛇规则跟随移动。

### 4.3 进入工作状态流程（MVP）
1. 玩家在面板中切换“工作”。
2. `ZombieManager` 更新状态并广播事件。
3. UI 更新状态标记；产出逻辑在最终版接入。

---

## 5. 开发排期（可执行）

### 阶段 A：到 2026-02-23（首测）
1. 完成数据结构：`ZombieDefinitionSO`、`ZombieInstanceData`
2. 完成 `ZombieManager` 基础增删查与状态切换
3. 完成跟随系统（贪吃蛇，2只上限）
4. 完成基础管理面板（列表 + 跟随切换）
5. 完成 2 个僵尸原型接入
6. 完成 1 条图鉴/故事解锁链路

验收：
- 能生成僵尸、能跟随、能切换状态、能看到图鉴基础信息

### 阶段 B：2026-03-10 ~ 2026-03-20（终版）
1. 接入 `ZombieBuffService` 到背包与探索掉率
2. 完善工作状态（至少1个简单岗位效果）
3. 优化跟随表现（抖动、穿插、转向）
4. 增加 1~2 个有差异化增益僵尸
5. 完成 UI 反馈（解锁弹窗、状态提示）
6. 稳定性测试与数值微调

---

## 6. 简单且有趣的扩展（优先级从高到低）
- 扩展1：僵尸“个性短语气泡”
  - 跟随时随机说一句短台词（仅文本气泡，无复杂系统）
- 扩展2：工作中“进度条领奖”
  - 工作状态每X秒产出一次简单材料，可手动领取
- 扩展3：图鉴“首次解锁小动画”
  - UI放大+音效，增强反馈成本低
- 扩展4：僵尸偏好垃圾标签
  - 特定僵尸提升某类垃圾转化率（仅做数值加权）

---

## 7. 与现有系统对接点
- 背包：`Assets/Scripts/Inventory/ScriptableObjects/InventorySO.cs`
  - 使用 `ExpandCapacity(int amount)` 接入背包僵尸增益
- 探索掉落：`Assets/Scripts/Manager/SceneLootManager.cs`
  - 为掉落分配增加“僵尸加权参数”入口
- 玩家与场景管理：
  - `Assets/Scripts/Manager/GameManager.cs`
  - `Assets/Scripts/Manager/GameSceneManager.cs`
  - 进入场景后确保跟随僵尸重绑定玩家轨迹

---

## 8. 风险与约束
- 风险1：跟随实现若直接寻路，开发成本高且不稳定
  - 对策：首版使用轨迹点贪吃蛇方案
- 风险2：UI与系统联动复杂导致延期
  - 对策：首版先做列表+按钮，不做复杂拖拽
- 风险3：数值迭代不足影响体验
  - 对策：将 buff 与掉率做成配置化参数

---

## 9. 交付标准（Definition of Done）
- 首测（03-09）必须满足：
  - 僵尸可生成、可查看、可跟随（贪吃蛇）
  - 至少1个增益生效
  - 至少1条图鉴故事可解锁
- 终版（03-20）必须满足：
  - 僵尸系统全链路稳定（生成/管理/跟随/增益/图鉴）
  - 至少1个扩展玩法上线
  - 关键交互有可感知反馈（UI/音效/提示）
