using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Scripts
{
    /// <summary>
    /// 存档系统：负责保存/读取游戏进度。
    /// 预留扩展点：过渡场景、剧情进度、背包、僵尸列表等。
    /// </summary>
    public static class SaveSystem
    {
        const string SaveFileName = "save.json";
        const int CurrentVersion = 1;

        static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Serializable]
        public class SaveData
        {
            public int version;
            public string sceneName;
            public long saveTimeTicks;

            // 预留：过渡场景、剧情进度等
            // public string transitionSceneName;
            // public string storyProgressKey;
            // public List<string> inventoryItemGuids;
        }

        /// <summary>是否存在存档</summary>
        public static bool HasSaveData()
        {
            return File.Exists(SavePath);
        }

        /// <summary>保存当前游戏状态</summary>
        public static bool Save()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                var data = new SaveData
                {
                    version = CurrentVersion,
                    sceneName = scene.name,
                    saveTimeTicks = DateTime.UtcNow.Ticks
                };

                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveSystem] 存档成功: {data.sceneName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] 存档失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>读取存档数据（不自动加载场景，由调用方决定）</summary>
        public static SaveData Load()
        {
            if (!HasSaveData())
            {
                Debug.LogWarning("[SaveSystem] 无存档");
                return null;
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || string.IsNullOrEmpty(data.sceneName))
                {
                    Debug.LogWarning("[SaveSystem] 存档数据无效");
                    return null;
                }
                Debug.Log($"[SaveSystem] 读取存档: {data.sceneName}");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] 读档失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>删除存档</summary>
        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[SaveSystem] 已删除存档");
            }
        }
    }
}
