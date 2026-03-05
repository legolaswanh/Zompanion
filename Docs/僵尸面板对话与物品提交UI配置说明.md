# 僵尸对话与物品提交配置说明

对话与物品提交在**场景内僵尸 Trigger 范围按 E** 触发。背包中有该僵尸需要的剧情物品时，对话中会显示「提交 XXX」选项，点击后消耗物品并解锁剧情。**无需额外面板**。

## 一、已实现的功能

1. **场景内对话**：玩家靠近僵尸进入 Trigger 范围，按 E 触发对话
2. **对话内提交**：若背包有该僵尸需要的剧情物品且剧情未解锁，对话中显示「提交 [物品名]」选项
3. **点击即提交**：选择选项后物品消失，剧情解锁（由 Sequencer 命令完成）
4. **僵尸面板**：仅负责管理跟随、派遣与剧情展示

## 二、场景内僵尸 Trigger 配置

1. 为可对话的僵尸 GameObject 设置 Tag 为 **InteractiveZombie**
2. 添加 **DialogueInteractTrigger** 组件：
   - **Button Canvas**：可选，按 E 提示
   - **作为僵尸对话**：勾选 ✓
   - **Zombie Definition Id**：可留空，有 ZombieAgent 时自动获取
3. 添加 **DialogueSystemTrigger** 组件（配置 Conversation）
4. 添加 **Collider2D**（Is Trigger = true）

## 三、ZombieDialogueLuaBridge（必须）

在**持久化场景**（如 Persistent）中创建空 GameObject，添加 **ZombieDialogueLuaBridge** 组件。  
该组件向 Lua 注册 `HasItemForZombie`，供对话选项 Condition 使用，需常驻场景。

## 四、Dialogue System 对话配置

### 4.1 对话结构示例

- 父节点：僵尸问「你想给我什么？」等
- 子节点：每个可提交物品对应一个选项，例如「提交 [书本]」

### 4.2 每个「提交 XXX」选项的配置

**Condition（Lua）：**
```lua
return HasItemForZombie(Variable["CurrentZombieDefinitionId"], "物品itemName")
```
- `物品itemName` 需与 `ItemDataSO.itemName` 一致（如 `StoryProp_Monk_01`）

**Sequence：**
```
ZombieGiveItem(StoryProp_Monk_01)
```
- 单参数：itemName，definitionId 从 `Variable["CurrentZombieDefinitionId"]` 读取
- 或双参数：`ZombieGiveItem(zombie_monk, StoryProp_Monk_01)`

### 4.3 相关脚本

- `DialogueInteractTrigger`：按 E 触发对话；勾选「作为僵尸对话」时设置 `Variable["CurrentZombieDefinitionId"]`
- `ZombieDialogueLuaBridge`：注册 Lua 函数 `HasItemForZombie`
- `SequencerCommandZombieGiveItem`：Sequencer 命令，消耗物品并解锁剧情
