using UnityEngine;

namespace Code.Scripts
{
    /// <summary>
    /// 静态数据传递：在加载过渡场景前设置，过渡场景读取后用于显示对应文字、延续音乐。
    /// </summary>
    public static class TransitionSceneData
    {
        /// <summary>加载完过渡后要去的目标场景名</summary>
        public static string NextSceneName { get; set; }

        /// <summary>文字映射的 Key，用于在过渡场景中显示对应 UI</summary>
        public static string TextKey { get; set; }

        /// <summary>延续到过渡场景的音乐（由上一关卡传入）</summary>
        public static AudioClip CarriedOverMusicClip { get; set; }
        public static bool CarriedOverMusicLoop { get; set; }
        public static float CarriedOverMusicVolume { get; set; }
        public static float CarriedOverMusicFadeInTime { get; set; }

        public static void Set(string nextScene, string textKey = null)
        {
            NextSceneName = nextScene;
            TextKey = string.IsNullOrEmpty(textKey) ? nextScene : textKey;
        }

        /// <summary>设置延续到过渡场景的音乐（在加载过渡场景前调用）</summary>
        public static void SetCarriedOverMusic(AudioClip clip, bool loop = true, float volume = 1f, float fadeInTime = 0.5f)
        {
            CarriedOverMusicClip = clip;
            CarriedOverMusicLoop = loop;
            CarriedOverMusicVolume = volume;
            CarriedOverMusicFadeInTime = fadeInTime;
        }

        public static void Clear()
        {
            NextSceneName = null;
            TextKey = null;
            CarriedOverMusicClip = null;
        }
    }
}
