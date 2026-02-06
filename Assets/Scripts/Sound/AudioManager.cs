using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Scripts
{
    /// <summary>
    /// 音乐音效系统：单例、跨场景存在，支持每场景配置不同 BGM，带淡入淡出。
    /// 场景加载时自动查找 SceneMusicConfig 并播放对应音乐。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("音量")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private Coroutine _musicFadeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            Debug.Log("[AudioManager] 已初始化");
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
            var config = FindObjectOfType<SceneMusicConfig>();

            // 过渡场景：使用上一关卡平移到 TransitionSceneData 的音乐，保证连续播放
            if (config == null && TransitionSceneData.CarriedOverMusicClip != null)
            {
                var clip = TransitionSceneData.CarriedOverMusicClip;
                PlayMusic(clip, TransitionSceneData.CarriedOverMusicFadeInTime,
                    TransitionSceneData.CarriedOverMusicLoop, TransitionSceneData.CarriedOverMusicVolume);
                Debug.Log($"[AudioManager] 过渡场景 {scene.name}，延续 BGM: {clip.name}");
                return;
            }

            if (config == null) return;

            if (config.MusicClip != null)
            {
                PlayMusic(config.MusicClip, config.FadeInTime, config.Loop, config.Volume);
                if (_musicSource.clip == config.MusicClip && _musicSource.isPlaying)
                    Debug.Log($"[AudioManager] 场景 {scene.name}，相同 BGM 持续播放: {config.MusicClip.name}");
                else
                    Debug.Log($"[AudioManager] 场景 {scene.name} 加载，播放 BGM: {config.MusicClip.name}");
            }
            else if (config.StopMusicOnLoad)
            {
                StopMusic();
                Debug.Log($"[AudioManager] 场景 {scene.name} 无 BGM 配置，停止音乐");
            }
        }

        /// <summary>播放背景音乐（带淡入，可选淡出当前曲目）。若已是同一曲目在播，则保持连续播放。</summary>
        public void PlayMusic(AudioClip clip, float fadeInTime = 1f, bool loop = true, float volumeScale = 1f)
        {
            if (clip == null) return;

            // 相同曲目已在播放：保持连续，不打断
            if (_musicSource.clip == clip && _musicSource.isPlaying)
            {
                if (_musicFadeCoroutine != null)
                    StopCoroutine(_musicFadeCoroutine);
                _musicFadeCoroutine = null;
                _musicSource.volume = musicVolume * masterVolume * volumeScale;
                return;
            }

            if (_musicFadeCoroutine != null)
                StopCoroutine(_musicFadeCoroutine);

            _musicFadeCoroutine = StartCoroutine(FadeToNewMusic(clip, fadeInTime, loop, volumeScale));
        }

        private IEnumerator FadeToNewMusic(AudioClip clip, float fadeInTime, bool loop, float volumeScale)
        {
            float targetVol = musicVolume * masterVolume * volumeScale;

            if (_musicSource.isPlaying)
            {
                float fadeOutTime = Mathf.Min(0.5f, fadeInTime * 0.5f);
                yield return FadeMusicVolume(0f, fadeOutTime);
            }

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = 0f;
            _musicSource.Play();

            yield return FadeMusicVolume(targetVol, fadeInTime);

            _musicFadeCoroutine = null;
        }

        private IEnumerator FadeMusicVolume(float targetVolume, float duration)
        {
            float start = _musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(start, targetVolume, elapsed / duration);
                yield return null;
            }

            _musicSource.volume = targetVolume;
        }

        /// <summary>停止音乐（可选淡出）</summary>
        public void StopMusic(float fadeOutTime = 0.5f)
        {
            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
                _musicFadeCoroutine = null;
            }

            if (fadeOutTime <= 0f || !_musicSource.isPlaying)
            {
                _musicSource.Stop();
                return;
            }

            StartCoroutine(FadeOutAndStop(fadeOutTime));
        }

        private IEnumerator FadeOutAndStop(float duration)
        {
            float start = _musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(start, 0f, elapsed / duration);
                yield return null;
            }

            _musicSource.Stop();
            _musicSource.volume = musicVolume * masterVolume;
        }

        /// <summary>播放音效（不打断音乐）</summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            float vol = sfxVolume * masterVolume * volumeScale;
            _sfxSource.PlayOneShot(clip, vol);
        }

        /// <summary>在指定世界坐标播放音效（3D 音效）</summary>
        public void PlaySFXAt(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume * volumeScale);
        }

        public void SetMasterVolume(float v) => masterVolume = Mathf.Clamp01(v);
        public void SetMusicVolume(float v) => musicVolume = Mathf.Clamp01(v);
        public void SetSFXVolume(float v) => sfxVolume = Mathf.Clamp01(v);

        public bool IsMusicPlaying => _musicSource != null && _musicSource.isPlaying;

        /// <summary>将当前正在播放的音乐保存到 TransitionSceneData，供过渡场景延续使用</summary>
        public void CarryOverCurrentMusic()
        {
            if (_musicSource == null || _musicSource.clip == null || !_musicSource.isPlaying) return;
            float volScale = (musicVolume * masterVolume) > 0.01f
                ? Mathf.Clamp01(_musicSource.volume / (musicVolume * masterVolume))
                : 1f;
            TransitionSceneData.SetCarriedOverMusic(_musicSource.clip, _musicSource.loop, volScale, 0.5f);
            Debug.Log($"[AudioManager] 已保存延续音乐: {_musicSource.clip.name}");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
