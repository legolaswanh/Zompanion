using UnityEngine;

namespace Code.Scripts
{
    /// <summary>
    /// 场景音乐配置：放在每个场景中，配置该场景的 BGM。
    /// 场景加载时，AudioManager 会自动查找并播放配置的音乐。
    /// 可挂在 LevelSceneManager 同一物体或单独空物体上。
    /// </summary>
    public class SceneMusicConfig : MonoBehaviour
    {
        [Header("BGM 配置")]
        [Tooltip("本场景的背景音乐")]
        [SerializeField] private AudioClip musicClip;

        [Tooltip("是否循环")]
        [SerializeField] private bool loop = true;

        [Tooltip("音量倍率 (0~1)")]
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;

        [Tooltip("淡入时长（秒）")]
        [SerializeField] [Min(0f)] private float fadeInTime = 1f;

        [Header("可选")]
        [Tooltip("本场景无 BGM 时是否停止当前音乐")]
        [SerializeField] private bool stopMusicOnLoad = false;

        public AudioClip MusicClip => musicClip;
        public bool Loop => loop;
        public float Volume => volume;
        public float FadeInTime => fadeInTime;
        public bool StopMusicOnLoad => stopMusicOnLoad;
    }
}
