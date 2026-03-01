using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;
using Code.Scripts;

/// <summary>
/// 跨场景单例：统一管理 Timeline（PlayableDirector）的播放。
/// 支持播放前/后自动处理玩家控制、VCam 切换、轨道绑定。
/// </summary>
public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance { get; private set; }

    /// <summary>当前正在播放的 PlayableDirector，没有播放时为 null。</summary>
    public PlayableDirector CurrentDirector { get; private set; }

    /// <summary>是否有 Timeline 正在播放。</summary>
    public bool IsPlaying => CurrentDirector != null && CurrentDirector.state == PlayState.Playing;

    [Header("Player Binding")]
    [Tooltip("Timeline 里角色轨的名称；留空则不自动绑定角色轨")]
    [SerializeField] string playerTrackName = "Player";
    [Tooltip("Player 下要绑到角色轨的子路径（如 Capsule）；留空则绑 Player 根")]
    [SerializeField] string playerBindingPath = "";

    [Header("默认行为")]
    [Tooltip("播放 Timeline 时是否默认禁用玩家操控")]
    [SerializeField] bool disablePlayerControlByDefault = true;
    [Tooltip("播放 Timeline 时是否默认关闭场景 VCam。Cinemachine Track 会自动覆盖 Brain，通常不需要开启")]
    [SerializeField] bool disableVCamsByDefault = false;

    /// <summary>Timeline 开始播放时触发。参数：正在播放的 PlayableDirector。</summary>
    public event Action<PlayableDirector> OnTimelineStarted;
    /// <summary>Timeline 播放结束时触发。参数：已结束的 PlayableDirector。</summary>
    public event Action<PlayableDirector> OnTimelineFinished;

    Coroutine _playCoroutine;
    bool _playerControlWasDisabled;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 公共 API ──

    /// <summary>
    /// 播放指定的 PlayableDirector。
    /// </summary>
    /// <param name="director">要播放的 PlayableDirector（场景内物体上的组件）</param>
    /// <param name="bindPlayer">是否将当前 Player 绑定到 Timeline 的角色轨</param>
    /// <param name="disablePlayerControl">播放期间是否禁用玩家操控（null = 使用默认设置）</param>
    /// <param name="manageVCams">播放期间是否关闭场景 VCam（null = 使用默认设置）</param>
    /// <param name="onFinished">播放结束后的回调</param>
    public void Play(
        PlayableDirector director,
        bool bindPlayer = false,
        bool? disablePlayerControl = null,
        bool? manageVCams = null,
        Action onFinished = null)
    {
        if (director == null)
        {
            Debug.LogWarning("[TimelineManager] Play: director 为 null");
            onFinished?.Invoke();
            return;
        }

        if (IsPlaying)
            Stop();

        if (_playCoroutine != null)
            StopCoroutine(_playCoroutine);

        _playCoroutine = StartCoroutine(PlayCoroutine(
            director,
            bindPlayer,
            disablePlayerControl ?? disablePlayerControlByDefault,
            manageVCams ?? disableVCamsByDefault,
            onFinished));
    }

    /// <summary>
    /// 立即停止当前正在播放的 Timeline。
    /// </summary>
    public void Stop()
    {
        if (CurrentDirector != null && CurrentDirector.state == PlayState.Playing)
            CurrentDirector.Stop();
    }

    /// <summary>
    /// 暂停当前 Timeline。
    /// </summary>
    public void Pause()
    {
        if (CurrentDirector != null && CurrentDirector.state == PlayState.Playing)
            CurrentDirector.Pause();
    }

    /// <summary>
    /// 恢复暂停的 Timeline。
    /// </summary>
    public void Resume()
    {
        if (CurrentDirector != null && CurrentDirector.state == PlayState.Paused)
            CurrentDirector.Resume();
    }

    /// <summary>
    /// 将 PlayableDirector 的指定角色轨绑定到当前运行时 Player 的 Animator。
    /// 可单独调用，不一定要配合 Play 使用。
    /// </summary>
    public void BindPlayerToDirector(PlayableDirector director)
    {
        if (director == null || director.playableAsset == null) return;
        if (string.IsNullOrEmpty(playerTrackName)) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayer == null) return;

        GameObject player = GameManager.Instance.CurrentPlayer;
        Transform bindTarget = string.IsNullOrEmpty(playerBindingPath)
            ? player.transform
            : player.transform.Find(playerBindingPath) ?? player.transform;

        Animator playerAnimator = bindTarget.GetComponent<Animator>()
                                  ?? bindTarget.GetComponentInChildren<Animator>();

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) return;

        foreach (var binding in timeline.outputs)
        {
            var track = binding.sourceObject as TrackAsset;
            if (track != null && track.name == playerTrackName && playerAnimator != null)
                director.SetGenericBinding(track, playerAnimator);
        }
    }

    /// <summary>
    /// 将 Persistent 的 CinemachineBrain 绑定到 PlayableDirector 的 Cinemachine Track。
    /// 解决跨场景无法在编辑器拖引用的问题，每次播放前自动调用。
    /// </summary>
    public void BindCinemachineBrain(PlayableDirector director)
    {
        if (director == null || director.playableAsset == null) return;

        CinemachineBrain brain = CameraControl.Instance != null
            ? CameraControl.Instance.Brain
            : (Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null);

        if (brain == null)
        {
            Debug.LogWarning("[TimelineManager] BindCinemachineBrain: 未找到 CinemachineBrain");
            return;
        }

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) return;

        bool bound = false;
        foreach (var binding in timeline.outputs)
        {
            Debug.Log($"[TimelineManager] Track: {binding.streamName}, outputType: {binding.outputTargetType}");
            if (binding.outputTargetType != null && typeof(CinemachineBrain).IsAssignableFrom(binding.outputTargetType))
            {
                director.SetGenericBinding(binding.sourceObject, brain);
                bound = true;
                Debug.Log($"[TimelineManager] 已绑定 CinemachineBrain 到轨道 '{binding.streamName}'");
            }
        }
        if (!bound)
            Debug.LogWarning("[TimelineManager] BindCinemachineBrain: 未找到 Cinemachine Track，请确认 Timeline 中已添加 Cinemachine Track");
    }

    // ── 内部 ──

    IEnumerator PlayCoroutine(
        PlayableDirector director,
        bool bindPlayer,
        bool disableControl,
        bool manageVCams,
        Action onFinished)
    {
        CurrentDirector = director;

        BindCinemachineBrain(director);

        if (bindPlayer)
            BindPlayerToDirector(director);

        if (disableControl && GameManager.Instance != null)
        {
            _playerControlWasDisabled = true;
            GameManager.Instance.SetPlayerControlEnabled(false);
        }

        GameSceneManager sceneManager = null;
        if (manageVCams)
        {
            sceneManager = FindObjectOfType<GameSceneManager>();
            sceneManager?.DisableAllVCams();
        }

        director.Play();
        OnTimelineStarted?.Invoke(director);

        yield return new WaitUntil(() =>
            director == null || director.state != PlayState.Playing);

        if (manageVCams && sceneManager != null)
            sceneManager.ActivateFollowVCam();

        if (_playerControlWasDisabled && GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerControlEnabled(true);
            _playerControlWasDisabled = false;
        }

        var finishedDirector = CurrentDirector;
        CurrentDirector = null;
        _playCoroutine = null;

        OnTimelineFinished?.Invoke(finishedDirector);
        onFinished?.Invoke();
    }
}
