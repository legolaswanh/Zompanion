using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("UI Prefabs")]
    [SerializeField] private GameObject mainHudPrefab;
    [SerializeField] private List<GameObject> runtimeHudPrefabs = new List<GameObject>();

    [Header("Game Scenes")]
    [SerializeField] private string[] gameSceneNames = { "HomeScene", "TestScene", "ExplorationScene" };

    private GameObject _mainHudInstance;
    private readonly Dictionary<GameObject, GameObject> _runtimeHudInstances = new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (FindObjectsByType<UIManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isGameScene = IsGameScene(scene.name);
        if (isGameScene)
            EnsureHudExists();

        SetHudActive(isGameScene);
    }

    private bool IsGameScene(string sceneName)
    {
        if (gameSceneNames == null)
            return false;

        for (int i = 0; i < gameSceneNames.Length; i++)
        {
            if (gameSceneNames[i] == sceneName)
                return true;
        }

        return false;
    }

    private void EnsureHudExists()
    {
        EnsureHudInstance(mainHudPrefab, ref _mainHudInstance);

        if (runtimeHudPrefabs == null)
            return;

        for (int i = 0; i < runtimeHudPrefabs.Count; i++)
        {
            GameObject prefab = runtimeHudPrefabs[i];
            if (prefab == null || prefab == mainHudPrefab)
                continue;

            if (_runtimeHudInstances.TryGetValue(prefab, out GameObject existing) && existing != null)
                continue;

            GameObject instance = Instantiate(prefab);
            DontDestroyOnLoad(instance);
            _runtimeHudInstances[prefab] = instance;
        }
    }

    private static void EnsureHudInstance(GameObject prefab, ref GameObject instance)
    {
        if (instance != null || prefab == null)
            return;

        instance = Instantiate(prefab);
        DontDestroyOnLoad(instance);
    }

    private void SetHudActive(bool active)
    {
        if (_mainHudInstance != null)
            _mainHudInstance.SetActive(active);

        foreach (var pair in _runtimeHudInstances)
        {
            if (pair.Value != null)
                pair.Value.SetActive(active);
        }
    }
}
