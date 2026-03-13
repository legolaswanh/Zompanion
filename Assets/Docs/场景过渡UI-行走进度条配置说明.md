# 场景过渡 UI：角色行走进度条配置说明

实现「角色向右行走动画 = 加载进度条」的过渡界面效果。

---

## 1. 整体思路

`SceneTransitionManager` 在加载场景时会寻找一个名为 `Fill` 的 `Image`（或 `Image.Type.Filled`），并按其 `progressAnimationDuration` **按时间均匀**更新 `fillAmount`（0→1），不跟随实际加载速度。  
通过添加一个**隐藏的 Fill** 作为进度源，再用 `WalkingProgressIndicator` 脚本驱动角色沿轨道移动，即可实现行走进度效果。

---

## 2. 预制体层级结构

在 `Assets/Prefabs/UI/` 下创建 `SceneTransitionLoadingPanel` 预制体，建议结构如下：

```
SceneTransitionLoadingPanel (根物体，挂 CanvasGroup)
├── Background (Image，全屏深色底)
├── ProgressTrack (空物体，RectTransform，作为轨道的父物体)
│   ├── Fill (Image，隐藏，供 SceneTransitionManager 更新进度)
│   └── WalkingCharacter (角色，挂 WalkingProgressIndicator + Animator)
└── LoadingText (Text，可选，显示 "Loading..." 等)
```

---

## 3. 详细配置步骤

### 3.1 根物体与 Canvas

1. 新建空物体 `SceneTransitionLoadingPanel`
2. 添加 `Canvas` 组件（如尚未有 Canvas）：
   - Render Mode: **Screen Space - Overlay**
   - Sort Order: **10000**（保证在最上层）
3. 添加 `CanvasScaler`：
   - UI Scale Mode: **Scale With Screen Size**
   - Reference Resolution: **1920 x 1080**
4. 添加 `CanvasGroup`（用于整体淡入淡出）

### 3.2 Background

- 子物体 `Background`，挂 `Image`
- RectTransform：锚点拉伸全屏
- Color：深色，如 `(0.05, 0.05, 0.05, 1)`

### 3.3 ProgressTrack（轨道）

1. 在根下新建空物体 `ProgressTrack`
2. RectTransform 设置：
   - Anchors：水平拉伸，垂直居中，例如 Min(0.25, 0.45) Max(0.75, 0.55)
   - 左右留出边距，形成一条横向轨道

### 3.4 Fill（隐藏进度源）

1. 在 `ProgressTrack` 下新建 `Fill`
2. 添加 `Image` 组件：
   - Type: **Filled**
   - Fill Method: **Horizontal**
   - Fill Origin: **Left**
   - Fill Amount: **0**
   - Color: 设 Alpha 为 **0**（完全透明，仅作为进度数据源）
3. RectTransform：锚点拉伸填满 ProgressTrack

> 名称必须包含 `Fill`，这样 `SceneTransitionManager` 才能识别并更新它。

### 3.5 WalkingCharacter（行走角色）

1. 在 `ProgressTrack` 下新建 `WalkingCharacter`
2. 添加角色精灵：
   - 使用 `Image` 显示角色贴图，或使用带 `Animator` 的 `Image` 播放行走动画
3. RectTransform：
   - Anchor：左中，如 Min(0, 0.5) Max(0, 0.5)
   - Pivot：(0.5, 0.5)
   - 宽度、高度按角色尺寸调整
4. 添加 `WalkingProgressIndicator` 组件：
   - **Progress Fill Image**：拖入上方的 `Fill` 物体上的 Image
   - **Track Rect**：拖入 `ProgressTrack`（留空则用父物体）
   - **Start X**：进度 0 时的 X 位置（轨道左端），如 `-400`
   - **End X**：进度 1 时的 X 位置（轨道右端），如 `400`

> 若轨道宽度与 1920 参考分辨率相关，可先试 `-400` ~ `400`，再按实际视觉效果微调。

5. 行走动画（可选）：
   - 为角色添加 `Animator`，新建 Animator Controller
   - 创建 Idle → Walk 的过渡
   - 过渡界面显示期间 Animator 会持续播放，形成行走效果

---

## 4. 在 Persistent 中挂载预制体

1. 打开 `Persistent` 场景
2. 找到 `SceneTransitionManager`（或在 GameManager 下自动创建的实例）
3. 将 `SceneTransitionLoadingPanel` 预制体拖到 **Loading Panel Prefab** 字段
4. 设置 **Progress Animation Duration**：进度条 0→1 的时长（如 1.5 秒），角色会在此时间内均匀走完轨道；建议 `Minimum Display Time` ≥ 此值

---

## 5. 可选：轨道背景图

若希望轨道有路面、路径等背景：

- 在 `ProgressTrack` 下、`Fill` 之前添加一个 `TrackBackground` (Image)
- 使用道路/路径 Sprite，锚点拉伸填满 ProgressTrack
- 确保其渲染在 Fill 和角色之下（Hierarchy 顺序或 Sort Order）

---

## 6. 快速检查清单

| 项目 | 说明 |
|------|------|
| Fill 名称 | 必须包含 "Fill" |
| Fill Image | Type = Filled，Horizontal，Alpha 可设为 0 |
| WalkingProgressIndicator | 挂在角色上，引用 Fill 的 Image |
| Start X / End X | 与轨道实际宽度匹配 |
| 预制体 | 已赋给 SceneTransitionManager.Loading Panel Prefab |

配置完成后，所有通过 `SceneTransitionManager.LoadSceneWithTransition()` 触发的场景切换都会使用该行走进度界面。
