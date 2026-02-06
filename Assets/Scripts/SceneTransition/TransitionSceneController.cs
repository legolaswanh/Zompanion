using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Code.Scripts
{
    /// <summary>
    /// 过渡场景控制器：根据上一关卡传入的 TextKey 显示对应文字，等待后加载目标场景。
    /// 挂在过渡场景的根物体或 Canvas 上。
    /// </summary>
    public class TransitionSceneController : MonoBehaviour
    {
        // [Serializable]
        // public class TextMapping
        // {
        //     [Tooltip("Key，与 LevelSceneManager 的 Transition Text Key 对应")]
        //     public string key;
        //     [Tooltip("要显示的 GameObject（如 Text、含文字的父物体）")]
        //     public GameObject textObject;
        // }

        // [Header("文字映射")]
        // [Tooltip("Key -> 对应要显示的 UI 物体，根据上一关卡自动选择")]
        // [SerializeField] private TextMapping[] textMappings = Array.Empty<TextMapping>();

        [Header("过渡设置")]
        [Tooltip("显示过渡画面的时长（秒），结束后加载目标场景")]
        [SerializeField] [Min(0.1f)] private float displayDuration = 2f;
        [Tooltip("无数据时的默认目标场景")]
        [SerializeField] private string defaultNextScene = "MainMenu";

        [Header("淡入淡出")]
        [Tooltip("淡出黑屏显示过渡文字的时长")]
        [SerializeField] [Min(0f)] private float fadeInDuration = 0.8f;
        [Tooltip("淡入黑屏后加载目标场景的时长")]
        [SerializeField] [Min(0f)] private float fadeOutDuration = 0.8f;

        [Header("可选 Timeline")]
        [Tooltip("若有 Timeline，播放完后再加载；为空则按 displayDuration 计时")]
        [SerializeField] private UnityEngine.Playables.PlayableDirector timeline;

        private void Start()
        {
            string nextScene = TransitionSceneData.NextSceneName ?? defaultNextScene;
            // string textKey = TransitionSceneData.TextKey ?? nextScene;

            // // 初始隐藏所有文字
            // foreach (var m in textMappings)
            // {
            //     if (m.textObject != null)
            //         m.textObject.SetActive(false);
            // }

            // // 根据 Key 显示对应文字
            // foreach (var m in textMappings)
            // {
            //     if (m.textObject != null && string.Equals(m.key, textKey, StringComparison.OrdinalIgnoreCase))
            //     {
            //         m.textObject.SetActive(true);
            //         break;
            //     }
            // }

            TransitionSceneData.Clear();

            // Debug.Log($"[TransitionSceneController] 显示文字 Key={textKey}，加载目标 {nextScene}");

            StartCoroutine(TransitionSequence(nextScene));
        }

        private IEnumerator TransitionSequence(string sceneName)
        {
            // 1. 确保黑屏，淡出显示过渡文字
            if (TransitionFadeManager.Instance != null && fadeInDuration > 0f)
            {
                TransitionFadeManager.Instance.SetBlack();
                yield return TransitionFadeManager.Instance.FadeOut(fadeInDuration);
            }

            // 2. 等待显示时长或 Timeline
            if (timeline != null)
            {
                timeline.Play();
                while (timeline.state == PlayState.Playing)
                    yield return null;
            }
            else
            {
                yield return new WaitForSecondsRealtime(displayDuration);
            }

            // 3. 淡入黑屏
            if (TransitionFadeManager.Instance != null && fadeOutDuration > 0f)
            {
                yield return TransitionFadeManager.Instance.FadeIn(fadeOutDuration);
                TransitionFadeManager.Instance.FadeOutOnNextSceneLoad();
            }
            else if (TransitionFadeManager.Instance != null)
            {
                TransitionFadeManager.Instance.SetBlack();
                TransitionFadeManager.Instance.FadeOutOnNextSceneLoad();
            }

            // 4. 加载目标场景
            LoadNextScene(sceneName);
        }

        private void LoadNextScene(string sceneName)
        {
            Debug.Log($"[TransitionSceneController] 加载场景: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
}
