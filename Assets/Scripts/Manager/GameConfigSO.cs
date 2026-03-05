using UnityEngine;

namespace Code.Scripts
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Config/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Player")]
        public GameObject playerPrefab;
        public string startSceneName;

        [Header("New Game")]
        public string firstGameSceneName = "HomeScene";
        public string firstTimeTransitionSceneName;

        [Header("Inventory")]
        public InventorySO inventoryTemplate;

        [Header("Zombie System")]
        public ZombieCatalogSO zombieCatalog;
        public GameObject zombieSystemPrefab;
        public bool initializeZombieSystemOnStartup = true;
    }
}
