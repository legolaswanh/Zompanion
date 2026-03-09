using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Scripts
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Config")]
        [SerializeField] private GameConfigSO gameConfig;

        private GameObject _currentPlayer;
        private AudioListener _persistentAudioListener;
        private InventorySO _runtimePlayerInventory;

        public GameObject CurrentPlayer => _currentPlayer;
        public InventorySO PlayerInventory => _runtimePlayerInventory;
        public ZombieCatalogSO ZombieCatalog => GetZombieCatalog();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeRuntimeInventory();
            EnsureAudioManagerExists();
            EnsureTransitionFadeManagerExists();
            EnsureSceneTransitionManagerExists();
            EnsureSceneStateManagerExists();
            EnsureTimelineManagerExists();
            EnsureZombieSystemExists();
            EnsurePersistentAudioListener();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            ResolveSingleAudioListener();
            PlaceOrSpawnPlayerAtSpawnPoint();

            string startupScene = GetStartSceneName();
            if (string.IsNullOrEmpty(startupScene))
            {
                Debug.LogWarning("[GameManager] startSceneName is empty.");
                return;
            }

            SceneManager.LoadScene(startupScene, LoadSceneMode.Additive);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResolveSingleAudioListener();
            DisableDuplicateMainCameras();
            EnsurePersistentCameraRenders();
            PlaceOrSpawnPlayerAtSpawnPoint();

            string startupScene = GetStartSceneName();
            if (!string.IsNullOrEmpty(startupScene) && scene.name == startupScene && mode == LoadSceneMode.Additive)
            {
                if (scene.isLoaded)
                    SceneManager.SetActiveScene(scene);
            }
        }

        private void InitializeRuntimeInventory()
        {
            InventorySO template = GetInventoryTemplate();
            if (template == null)
            {
                _runtimePlayerInventory = null;
                Debug.LogWarning("[GameManager] Inventory template is missing. Runtime inventory is null.");
                return;
            }

            _runtimePlayerInventory = Instantiate(template);
            _runtimePlayerInventory.name = $"{template.name}_Runtime";
            _runtimePlayerInventory.Initialize();

            if (_runtimePlayerInventory.clearOnStart)
                _runtimePlayerInventory.ClearAll();
        }

        private void EnsureZombieSystemExists()
        {
            if (!GetInitializeZombieSystemOnStartup())
                return;

            ZombieManager existingManager = FindAnyZombieManager();
            if (existingManager != null)
            {
                if (ZombieCatalog != null)
                    existingManager.SetZombieCatalog(ZombieCatalog);

                if (_runtimePlayerInventory != null)
                    existingManager.SetPlayerInventory(_runtimePlayerInventory);
                return;
            }

            GameObject prefab = GetZombieSystemPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("[GameManager] ZombieSystem prefab is not assigned; initialization skipped.");
                return;
            }

            GameObject zombieSystem = Instantiate(prefab);
            zombieSystem.name = prefab.name;

            ZombieManager spawnedManager = zombieSystem.GetComponent<ZombieManager>();
            if (spawnedManager == null)
                spawnedManager = zombieSystem.GetComponentInChildren<ZombieManager>(true);

            if (spawnedManager != null)
            {
                if (ZombieCatalog != null)
                    spawnedManager.SetZombieCatalog(ZombieCatalog);

                if (_runtimePlayerInventory != null)
                    spawnedManager.SetPlayerInventory(_runtimePlayerInventory);
            }
        }

        private ZombieManager FindAnyZombieManager()
        {
            if (ZombieManager.Instance != null)
                return ZombieManager.Instance;

#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<ZombieManager>(FindObjectsInactive.Include);
#else
            return FindObjectOfType<ZombieManager>(true);
#endif
        }

        private void EnsurePersistentAudioListener()
        {
            GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCam != null)
                _persistentAudioListener = mainCam.GetComponent<AudioListener>();

            if (_persistentAudioListener == null)
            {
                AudioListener existing = GetComponentInChildren<AudioListener>(true);
                if (existing != null)
                {
                    _persistentAudioListener = existing;
                }
                else
                {
                    GameObject go = new GameObject("PersistentAudioListener");
                    go.transform.SetParent(transform);
                    _persistentAudioListener = go.AddComponent<AudioListener>();
                }
            }

            ResolveSingleAudioListener();
        }

        private void ResolveSingleAudioListener()
        {
            if (_persistentAudioListener == null)
                return;

#if UNITY_2023_1_OR_NEWER
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            AudioListener[] listeners = FindObjectsOfType<AudioListener>();
#endif

            foreach (AudioListener listener in listeners)
            {
                if (listener == _persistentAudioListener)
                    continue;

                Destroy(listener);
            }

            _persistentAudioListener.enabled = true;
        }

        private void EnsureTransitionFadeManagerExists()
        {
            if (TransitionFadeManager.Instance != null)
                return;

            GameObject go = new GameObject("TransitionFadeManager");
            go.transform.SetParent(transform);
            go.AddComponent<TransitionFadeManager>();
        }

        private void EnsureSceneTransitionManagerExists()
        {
            if (FindObjectOfType<SceneTransitionManager>(true) != null)
                return;

            if (SceneTransitionManager.Instance != null)
                return;

            GameObject go = new GameObject("SceneTransitionManager");
            go.transform.SetParent(transform);
            go.AddComponent<SceneTransitionManager>();
        }

        private void EnsureSceneStateManagerExists()
        {
            if (SceneStateManager.Instance != null)
                return;

            GameObject go = new GameObject("SceneStateManager");
            go.transform.SetParent(transform);
            go.AddComponent<SceneStateManager>();
        }

        private void EnsureTimelineManagerExists()
        {
            if (TimelineManager.Instance != null)
                return;

            GameObject go = new GameObject("TimelineManager");
            go.transform.SetParent(transform);
            go.AddComponent<TimelineManager>();
        }

        private void EnsureAudioManagerExists()
        {
            if (AudioManager.Instance != null)
                return;

            GameObject go = new GameObject("AudioManager");
            go.transform.SetParent(transform);
            go.AddComponent<AudioManager>();
        }

        private void EnsurePersistentCameraRenders()
        {
            if (_persistentAudioListener == null)
                return;

            Camera cam = _persistentAudioListener.GetComponent<Camera>();
            if (cam == null)
                return;

            if (!cam.enabled)
                cam.enabled = true;

            if (cam.clearFlags == CameraClearFlags.Depth || cam.clearFlags == CameraClearFlags.Nothing)
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        }

        private void DisableDuplicateMainCameras()
        {
            if (_persistentAudioListener == null)
                return;

            GameObject persistentCam = _persistentAudioListener.gameObject;
            GameObject[] mainCams = GameObject.FindGameObjectsWithTag("MainCamera");
            foreach (GameObject go in mainCams)
            {
                if (go != null && go != persistentCam)
                    go.SetActive(false);
            }
        }

        public void PlaceOrSpawnPlayerAtSpawnPoint()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            Vector3 pos;
            Quaternion rot;
            Vector2? facingOverride = null;

            if (SpawnContext.HasTransitionContext && SpawnContext.TargetSceneName == currentScene)
            {
                var entryPoint = FindSceneEntryPoint(SpawnContext.EntryPointId);
                if (entryPoint != null)
                {
                    pos = entryPoint.GetSpawnPosition(SpawnContext.FacingDirection);
                    rot = Quaternion.identity;
                    facingOverride = SpawnContext.FacingDirection;
                    SpawnContext.Clear();
                }
                else
                {
                    Debug.LogWarning($"[GameManager] 未找到 EntryPointId={SpawnContext.EntryPointId}，退回默认 SpawnPoint");
                    SpawnContext.Clear();
                    if (!TryGetDefaultSpawn(out pos, out rot))
                        return;
                }
            }
            else
            {
                if (!TryGetDefaultSpawn(out pos, out rot))
                    return;
            }

            if (_currentPlayer == null)
            {
                GameObject playerPrefabToUse = GetPlayerPrefab();
                if (playerPrefabToUse == null)
                    return;

                _currentPlayer = Instantiate(playerPrefabToUse, pos, rot);
                _currentPlayer.tag = "Player";
            }
            else
            {
                _currentPlayer.transform.SetPositionAndRotation(pos, rot);
            }

            var rb = _currentPlayer.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.position = new Vector2(pos.x, pos.y);
            }

            var pm = _currentPlayer.GetComponent<PlayerMovement>();
            if (pm != null && facingOverride.HasValue)
                pm.SetFacingDirection(facingOverride.Value);
        }

        private static SceneEntryPoint FindSceneEntryPoint(string entryPointId)
        {
#if UNITY_2023_1_OR_NEWER
            var all = UnityEngine.Object.FindObjectsByType<SceneEntryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var all = UnityEngine.Object.FindObjectsOfType<SceneEntryPoint>(true);
#endif
            foreach (var ep in all)
            {
                if (ep != null && ep.EntryPointId == entryPointId)
                    return ep;
            }
            return null;
        }

        private static bool TryGetDefaultSpawn(out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;
            GameObject spawnPoint = GameObject.FindWithTag("SpawnPoint");
            if (spawnPoint == null) return false;
            pos = spawnPoint.transform.position;
            rot = spawnPoint.transform.rotation;
            return true;
        }

        public void SetPlayerControlEnabled(bool enabled)
        {
            if (_currentPlayer == null)
                return;

            foreach (MonoBehaviour mb in _currentPlayer.GetComponents<MonoBehaviour>())
            {
                if (mb != null)
                    mb.enabled = enabled;
            }

            if (!enabled)
                ResetPlayerAnimatorToIdle(_currentPlayer);
        }

        private static void ResetPlayerAnimatorToIdle(GameObject player)
        {
            Animator anim = player.GetComponentInChildren<Animator>();
            if (anim == null)
                return;

            anim.SetFloat("Hor", 0f);
            anim.SetFloat("Vert", 0f);
            anim.SetFloat("State", 0f);
            anim.SetBool("IsJump", false);
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[GameManager] LoadScene: sceneName is empty.");
                return;
            }

            SceneStateManager.Instance?.CaptureCurrentSceneState();
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        public void LoadNewGame()
        {
            string transitionScene = GetFirstTimeTransitionSceneName();
            string firstScene = GetFirstGameSceneName();

            if (!string.IsNullOrEmpty(transitionScene))
            {
                TransitionSceneData.Set(firstScene);
                LoadScene(transitionScene);
                return;
            }

            LoadScene(firstScene);
        }

        public void LoadSavedGame()
        {
            SaveSystem.SaveData data = SaveSystem.Load();
            if (data == null || string.IsNullOrEmpty(data.sceneName))
            {
                Debug.LogWarning("[GameManager] No valid save data found.");
                return;
            }

            LoadScene(data.sceneName);
        }

        public void LoadMainMenu()
        {
            LoadScene("MainMenu");
        }

        public void PlayerTeleport(Transform targetTransform)
        {
            if (_currentPlayer == null || targetTransform == null)
                return;

            CharacterController cc = _currentPlayer.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                _currentPlayer.transform.position = targetTransform.position;
                cc.enabled = true;
                return;
            }

            _currentPlayer.transform.position = targetTransform.position;
        }

        private GameObject GetPlayerPrefab()
        {
            return gameConfig != null ? gameConfig.playerPrefab : null;
        }

        private string GetStartSceneName()
        {
            return gameConfig != null ? gameConfig.startSceneName : null;
        }

        private string GetFirstGameSceneName()
        {
            return gameConfig != null ? gameConfig.firstGameSceneName : null;
        }

        private string GetFirstTimeTransitionSceneName()
        {
            return gameConfig != null ? gameConfig.firstTimeTransitionSceneName : null;
        }

        private InventorySO GetInventoryTemplate()
        {
            return gameConfig != null ? gameConfig.inventoryTemplate : null;
        }

        private GameObject GetZombieSystemPrefab()
        {
            return gameConfig != null ? gameConfig.zombieSystemPrefab : null;
        }

        private ZombieCatalogSO GetZombieCatalog()
        {
            return gameConfig != null ? gameConfig.zombieCatalog : null;
        }

        private bool GetInitializeZombieSystemOnStartup()
        {
            return gameConfig != null && gameConfig.initializeZombieSystemOnStartup;
        }
    }
}
