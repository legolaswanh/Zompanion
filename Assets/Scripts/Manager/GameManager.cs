using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Scripts
{
    /// <summary>
    /// 跨场景单例：负责在每次场景加载后把玩家放到当前场景的 SpawnPoint。
    /// 适用于关卡、开始界面、结束界面等复用 Template 的场景。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Player")] [Tooltip("玩家预制体；不赋值则不会生成玩家（例如纯 UI 的开始/结束界面）")] 
        [SerializeField] GameObject playerPrefab;
        [Tooltip("游戏启动时加载的首个场景（如 MainMenu）")]
        [SerializeField] string startSceneName;

        [Header("主界面/新游戏")]
        [Tooltip("新游戏要去的首个玩法场景（如 HomeScene、ExplorationScene）")]
        [SerializeField] string firstGameSceneName = "HomeScene";
        [Tooltip("初次进入游戏时的过渡/剧情场景；留空则直接进入 firstGameSceneName")]
        [SerializeField] string firstTimeTransitionSceneName;

        GameObject _currentPlayer;

        AudioListener _persistentAudioListener;

        /// <summary>当前跨场景存在的玩家实例；可能为 null（例如尚未进入过需要玩家的场景）。</summary>
        public GameObject CurrentPlayer => _currentPlayer;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.Log("[GameManager] 已存在单例，销毁重复实例");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioManagerExists();
            EnsureTransitionFadeManagerExists();
            EnsurePersistentAudioListener();
            Debug.Log("[GameManager] 单例已创建，DontDestroyOnLoad");
        }

        void EnsurePersistentAudioListener()
        {
            // Main Camera 已经 DontDestroyOnLoad（由 CameraControl 处理），上面自带 AudioListener。
            // 只需找到它作为持久监听器即可，不再额外创建。
            var mainCam = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCam != null)
            {
                _persistentAudioListener = mainCam.GetComponent<AudioListener>();
            }

            // 若 Main Camera 上没有（兜底），再检查自身子物体或创建
            if (_persistentAudioListener == null)
            {
                var existing = GetComponentInChildren<AudioListener>(true);
                if (existing != null)
                {
                    _persistentAudioListener = existing;
                }
                else
                {
                    var go = new GameObject("PersistentAudioListener");
                    go.transform.SetParent(transform);
                    _persistentAudioListener = go.AddComponent<AudioListener>();
                    Debug.Log("[GameManager] 已创建跨场景 AudioListener（兜底）");
                }
            }

            ResolveSingleAudioListener();
        }

        /// <summary>保证场景中只有唯一一个启用的 AudioListener。销毁场景里多余的，只保留持久的那个。</summary>
        void ResolveSingleAudioListener()
        {
            if (_persistentAudioListener == null) return;

            #if UNITY_2023_1_OR_NEWER
                var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            #else
                var listeners = FindObjectsOfType<AudioListener>();
            #endif

            foreach (var al in listeners)
            {
                if (al == _persistentAudioListener) continue;
                // 场景里多出来的 AudioListener 直接销毁，避免重复
                Debug.Log($"[GameManager] 销毁多余 AudioListener: {al.gameObject.name}");
                Destroy(al);
            }

            _persistentAudioListener.enabled = true;
        }

        void EnsureTransitionFadeManagerExists()
        {
            if (TransitionFadeManager.Instance == null)
            {
                var go = new GameObject("TransitionFadeManager");
                go.transform.SetParent(transform);
                go.AddComponent<TransitionFadeManager>();
                Debug.Log("[GameManager] 已自动创建 TransitionFadeManager");
            }
        }

        void EnsureAudioManagerExists()
        {
            if (AudioManager.Instance == null)
            {
                var go = new GameObject("AudioManager");
                go.transform.SetParent(transform);
                go.AddComponent<AudioManager>();
                Debug.Log("[GameManager] 已自动创建 AudioManager");
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Start()
        {
            ResolveSingleAudioListener();
            Debug.Log("[GameManager] Start（首次场景放置）");
            PlaceOrSpawnPlayerAtSpawnPoint();

            SceneManager.LoadScene(startSceneName, LoadSceneMode.Additive);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameManager] 场景已加载: {scene.name}");
            ResolveSingleAudioListener();
            PlaceOrSpawnPlayerAtSpawnPoint();
        }

        /// <summary>
        /// 在当前场景中查找 Tag 为 "SpawnPoint" 的物体，将玩家放置或生成到该位置。
        /// 若无 SpawnPoint 或未设置 playerPrefab，则不做任何事。
        /// </summary>
        public void PlaceOrSpawnPlayerAtSpawnPoint()
        {
            var spawnPoint = GameObject.FindWithTag("SpawnPoint");
            if (spawnPoint == null)
            {
                Debug.Log("[GameManager] 未找到 Tag=SpawnPoint，跳过放置玩家");
                return;
            }

            var spawnTransform = spawnPoint.transform;
            Vector3 pos = spawnTransform.position;
            Quaternion rot = spawnTransform.rotation;

            if (_currentPlayer == null)
            {
                if (playerPrefab == null)
                {
                    Debug.Log("[GameManager] 未设置 Player Prefab，跳过生成");
                    return;
                }

                _currentPlayer = Instantiate(playerPrefab, pos, rot);
                _currentPlayer.tag = "Player"; // 保证 EndTrigger 能识别
                Debug.Log($"[GameManager] 已生成玩家于 SpawnPoint {spawnPoint.name} @ {pos}");
            }
            else
            {
                _currentPlayer.transform.SetPositionAndRotation(pos, rot);
                Debug.Log($"[GameManager] 已移动已有玩家到 SpawnPoint {spawnPoint.name} @ {pos}");
            }
        }

        /// <summary>
        /// 设置玩家是否可被放置/生成。若为 false，下次场景加载时不会移动/生成玩家（可用于纯过场场景）。
        /// 默认行为始终尝试放置，可通过外部在需要时关闭。
        /// </summary>
        public void SetPlayerControlEnabled(bool enabled)
        {
            if (_currentPlayer == null)
            {
                Debug.Log("[GameManager] SetPlayerControlEnabled: 无当前玩家，忽略");
                return;
            }

            foreach (var mb in _currentPlayer.GetComponents<MonoBehaviour>())
            {
                if (mb != null) mb.enabled = enabled;
            }

            // 禁用时把玩家动作重置为静止，避免保持上一帧的移动/跑步姿态
            if (!enabled)
                ResetPlayerAnimatorToIdle(_currentPlayer);

            Debug.Log($"[GameManager] 玩家控制已{(enabled ? "开启" : "关闭")}");
        }

        /// <summary>
        /// 将玩家 Animator 重置为静止（Hor/Vert/State=0, IsJump=false），与 ithappy Character_Movement 参数一致。
        /// </summary>
        static void ResetPlayerAnimatorToIdle(GameObject player)
        {
            var anim = player.GetComponentInChildren<Animator>();
            if (anim == null) return;
            anim.SetFloat("Hor", 0f);
            anim.SetFloat("Vert", 0f);
            anim.SetFloat("State", 0f);
            anim.SetBool("IsJump", false);
        }
        
        /// <summary>
        /// 加载场景（Single 模式，替换当前场景）。供主界面、存档等调用。
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[GameManager] LoadScene: 场景名为空");
                return;
            }
            Debug.Log($"[GameManager] 加载场景: {sceneName}");
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        /// <summary>
        /// 新游戏：根据配置决定是否经过渡/剧情场景，再进入首个玩法场景。
        /// </summary>
        public void LoadNewGame()
        {
            if (!string.IsNullOrEmpty(firstTimeTransitionSceneName))
            {
                TransitionSceneData.Set(firstGameSceneName);
                LoadScene(firstTimeTransitionSceneName);
            }
            else
            {
                LoadScene(firstGameSceneName);
            }
        }

        /// <summary>
        /// 读取存档并加载对应场景。
        /// </summary>
        public void LoadSavedGame()
        {
            var data = SaveSystem.Load();
            if (data == null || string.IsNullOrEmpty(data.sceneName))
            {
                Debug.LogWarning("[GameManager] 无有效存档");
                return;
            }
            LoadScene(data.sceneName);
        }

        /// <summary>
        /// 返回主菜单。
        /// </summary>
        public void LoadMainMenu()
        {
            LoadScene("MainMenu");
        }

        /// <summary>
        /// 传送Player到指定位置
        /// </summary>
        /// <param name="transform">指定的位置</param>
        public void PlayerTeleport(Transform targetTransform)
        {
            // 1. 获取 CharacterController 组件
            CharacterController cc = _currentPlayer.GetComponent<CharacterController>();

            if (targetTransform == null)
            {
                return;
            }
            if (cc != null)
            {
                // 关键：必须先禁用组件
                cc.enabled = false; 
        
                // 2. 移动位置
                _currentPlayer.transform.position = targetTransform.position;
        
                // 3. 重新启用组件
                cc.enabled = true;
            }
            else
            {
                // 如果没有 CC 组件，才直接移动
                _currentPlayer.transform.position = targetTransform.position;
            }
        }
    }
}