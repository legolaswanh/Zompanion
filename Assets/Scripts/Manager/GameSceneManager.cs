using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections;
using System.Collections.Generic;
using Code.Scripts;
using Unity.Cinemachine;

/// <summary>
/// 每关/每场景一个：负责本场景的出生点、入口/出口 Timeline、关卡结束时切到下一场景；
/// 以及 Cinemachine VCam 切换（激活某台/全部关闭/恢复跟拍）、游戏中触发的 Timeline 播放。
/// 入口/出口 Timeline 会在玩家加载后，将角色轨绑定到当前 Player 的 Animator（相机由场景/Cinemachine 控制），再播放。
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [Header("Next Scene")]
    [Tooltip("玩家到达 LevelEndTrigger 后要加载的场景名；留空则只播出口 Timeline 不切场景（如结束界面）")]
    [SerializeField] string nextSceneName;

    [Header("过渡场景")]
    [Tooltip("过渡场景名；配置后先加载此场景显示文字，再加载 nextSceneName")]
    [SerializeField] string transitionSceneName;
    // [Tooltip("过渡场景要显示的文字 Key，与 TransitionSceneController 的 Text Mappings 对应")]
    // [SerializeField] string transitionTextKey;
    [Tooltip("关卡结束→过渡场景的淡入黑屏时长（秒）")]
    [SerializeField] [Min(0f)] float fadeToTransitionDuration = 0.6f;

    // [Header("Timeline (Optional)")]
    // [Tooltip("进入本场景时播放的运镜/过场")]
    // [SerializeField] PlayableDirector entryTimeline;
    // [Tooltip("玩家到达结束点后、切场景前播放的运镜/过场")]
    // [SerializeField] PlayableDirector exitTimeline;

    // [Header("Timeline Runtime Binding (Player Only)")]
    // [Tooltip("Timeline 里角色/胶囊轨的名称；留空则不绑定角色轨（相机由场景/Cinemachine 控制）")]
    // [SerializeField] string playerTrackName = "Player";
    // [Tooltip("Player 下要绑到角色轨的子路径（如 Capsule）；留空则绑 Player 根 Transform")]
    // [SerializeField] string playerBindingPath = "Capsule";

    // [Header("Player Control During Timeline")]
    // [Tooltip("播放入口/出口 Timeline 时是否禁用玩家控制")]
    // [SerializeField] bool disablePlayerControlDuringTimeline = true;

    [Header("Cinemachine VCams")]
    [Tooltip("本场景所有 Virtual Camera 的 GameObject（用于切换/关闭）；播 Timeline 前会全部 SetActive(false)，播完后恢复 followVCam")]
    [SerializeField] List<GameObject> vcamGameObjects = new List<GameObject>();
    [Tooltip("默认/跟拍 VCam；Timeline 播完后会只激活这一台")]
    [SerializeField] GameObject followVCam;
    [Tooltip("Player 下作为 VCam Follow 目标的子路径（留空用 Player 根）；与 playerBindingPath 可一致")]
    [SerializeField] string playerFollowPath = "";

    [Tooltip("若未在 Inspector 指定 followVCam（例如相机在 Persistent 无法拖入），是否从 CameraControl.Instance 自动解析持久化相机")]
    [SerializeField] bool resolvePersistentVCamIfEmpty = true;


    bool _isExiting;

    private Transform _activatedTelepoint;

    // ----- VCam 切换（供 CameraSwitchTrigger 或其它脚本调用） -----

    /// <summary>只激活指定 VCam，其它全部 SetActive(false)。</summary>
    public void ActivateVCam(GameObject vcam)
    {
        if (vcam == null) return;
        if (vcamGameObjects == null || vcamGameObjects.Count == 0) return;
        foreach (var go in vcamGameObjects)
            if (go != null) go.SetActive(go == vcam);
        Debug.Log($"[LevelSceneManager] ActivateVCam: {vcam.name}");
    }

    /// <summary>全部 VCam SetActive(false)；播 Timeline 前调用，让 Brain 只被 Timeline/Animator 驱动。</summary>
    public void DisableAllVCams()
    {
        if (vcamGameObjects == null) return;
        foreach (var go in vcamGameObjects)
            if (go != null) go.SetActive(false);
        Debug.Log("[LevelSceneManager] DisableAllVCams");
    }

    /// <summary>只激活跟拍 VCam；Timeline 播完后调用。</summary>
    public void ActivateFollowVCam()
    {
        if (followVCam == null) return;
        if (vcamGameObjects == null || vcamGameObjects.Count == 0) return;
        foreach (var go in vcamGameObjects)
            if (go != null) go.SetActive(go == followVCam);
        Debug.Log($"[LevelSceneManager] ActivateFollowVCam: {followVCam.name}");
    }

    /// <summary>游戏中触发一段 Timeline（运镜）；会先 DisableAllVCams，播完后 ActivateFollowVCam。可选：播前 BindTimelineToRuntimePlayer。</summary>
    public void PlayTimeline(PlayableDirector timeline)
    {
        if (timeline == null) return;
        DisableAllVCams();
        timeline.stopped += OnTriggeredTimelineStopped;
        timeline.Play();
        Debug.Log("[LevelSceneManager] PlayTimeline (triggered)");
    }

    void OnTriggeredTimelineStopped(PlayableDirector director)
    {
        director.stopped -= OnTriggeredTimelineStopped;
        ActivateFollowVCam();
        Debug.Log("[LevelSceneManager] 触发的 Timeline 播完，恢复跟拍");
    }

    /// <summary>玩家加载后，将所有带 CinemachineCamera 的 VCam 的 Follow 设为当前 Player 的 Transform（或 playerFollowPath 子节点）。</summary>
    public void BindFollowVCamsToPlayer()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayer == null) return;
        if (vcamGameObjects == null || vcamGameObjects.Count == 0) return;

        GameObject player = GameManager.Instance.CurrentPlayer;
        Transform followTarget = string.IsNullOrEmpty(playerFollowPath)
            ? player.transform
            : player.transform.Find(playerFollowPath);
        if (followTarget == null) followTarget = player.transform;

        int bound = 0;
        foreach (var go in vcamGameObjects)
        {
            if (go == null) continue;
            var vcam = go.GetComponent<CinemachineCamera>();
            if (vcam == null) continue;
            vcam.Follow = followTarget;
            bound++;
            Debug.Log($"[LevelSceneManager] BindFollow: {go.name} -> {followTarget.name}");
        }
        if (bound > 0) Debug.Log($"[LevelSceneManager] BindFollowVCamsToPlayer 完成，共 {bound} 台");
    }

    void Start()
    {
        // Debug.Log("[LevelSceneManager] Start");
        // if (entryTimeline != null)
        // {
        //     Debug.Log("[LevelSceneManager] 有入口 Timeline，等待玩家后绑定并播放");
        //     StartCoroutine(PlayEntryTimelineThenFinish());
        // }
        // else
        // {
        //     Debug.Log("[LevelSceneManager] 无入口 Timeline，等待玩家后绑定 VCam Follow 并恢复跟拍");
        //     StartCoroutine(WaitForPlayerThenInitCamera());
        // }

        StartCoroutine(WaitForPlayerThenInitCamera());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (_activatedTelepoint == null)
            {
                Debug.Log("No active telepoint");
            }
            else
            {
                Debug.Log("R triggered - teleporting to last telepoint");
                OnPlayerTeleport();
            }
            
        }
    }

    /// <summary>
    /// 当 followVCam 或 vcamGameObjects 未在 Inspector 指定时，从 Persistent 的 CameraControl 解析相机引用。
    /// 这样新场景中的 GameSceneManager 无需也不能把 Persistent 里的 CinemachineCamera 拖进去，运行时自动拿到引用。
    /// </summary>
    void TryResolvePersistentVCam()
    {
        if (!resolvePersistentVCamIfEmpty) return;
        if (CameraControl.Instance == null) return;

        GameObject persistentVCam = CameraControl.Instance.VCamGameObject;
        if (persistentVCam == null) return;

        if (followVCam == null)
        {
            followVCam = persistentVCam;
            Debug.Log("[LevelSceneManager] 已从 CameraControl 解析 followVCam（Persistent 相机）");
        }

        if (vcamGameObjects == null) vcamGameObjects = new List<GameObject>();
        if (vcamGameObjects.Count == 0 || !vcamGameObjects.Contains(persistentVCam))
        {
            if (!vcamGameObjects.Contains(persistentVCam))
                vcamGameObjects.Add(persistentVCam);
            Debug.Log("[LevelSceneManager] 已将 Persistent VCam 加入 vcamGameObjects");
        }
    }

    IEnumerator WaitForPlayerThenInitCamera()
    {
        TryResolvePersistentVCam();

        while (GameManager.Instance == null || GameManager.Instance.CurrentPlayer == null)
            yield return null;
        BindFollowVCamsToPlayer();
        ActivateFollowVCam();
        EnsurePlayerControlEnabled();
    }

    // IEnumerator PlayEntryTimelineThenFinish()
    // {
    //     Debug.Log("[LevelSceneManager] 等待 GameManager 与玩家就绪...");
    //     while (GameManager.Instance == null || GameManager.Instance.CurrentPlayer == null)
    //         yield return null;
    //     Debug.Log("[LevelSceneManager] 玩家已就绪，绑定 VCam Follow 与入口 Timeline");

    //     BindFollowVCamsToPlayer();
    //     BindTimelineToRuntimePlayer(entryTimeline);

    //     if (disablePlayerControlDuringTimeline && GameManager.Instance != null)
    //         GameManager.Instance.SetPlayerControlEnabled(false);

    //     DisableAllVCams();
    //     Debug.Log("[LevelSceneManager] 播放入口 Timeline");
    //     entryTimeline.Play();
    //     yield return new WaitUntil(() => entryTimeline.state != PlayState.Playing);
    //     Debug.Log("[LevelSceneManager] 入口 Timeline 播完");

    //     ActivateFollowVCam();
    //     if (disablePlayerControlDuringTimeline && GameManager.Instance != null)
    //         GameManager.Instance.SetPlayerControlEnabled(true);
    // }

    /// <summary>
    /// 将 PlayableDirector 的角色轨绑定到当前场景的 Player 的 Animator（运行时生成的对象）。相机由场景/Cinemachine 控制，不在此绑定。
    /// Timeline 轨需为 Animation Track 等绑定 Animator 的类型；Track 名称需与 playerTrackName 一致。
    /// </summary>
    // public void BindTimelineToRuntimePlayer(PlayableDirector director)
    // {
    //     if (director == null || director.playableAsset == null)
    //     {
    //         Debug.LogWarning("[LevelSceneManager] BindTimeline: director 或 playableAsset 为空，跳过");
    //         return;
    //     }
    //     if (GameManager.Instance == null || GameManager.Instance.CurrentPlayer == null)
    //     {
    //         Debug.LogWarning("[LevelSceneManager] BindTimeline: GameManager 或 CurrentPlayer 为空，跳过");
    //         return;
    //     }

    //     GameObject player = GameManager.Instance.CurrentPlayer;
    //     Transform playerBinding = string.IsNullOrEmpty(playerBindingPath)
    //         ? player.transform
    //         : player.transform.Find(playerBindingPath);
    //     if (playerBinding == null) playerBinding = player.transform;

    //     Animator playerAnimator = playerBinding.GetComponent<Animator>();
    //     if (playerAnimator == null) playerAnimator = playerBinding.GetComponentInChildren<Animator>();

    //     var timeline = director.playableAsset as TimelineAsset;
    //     if (timeline == null)
    //     {
    //         Debug.LogWarning("[LevelSceneManager] BindTimeline: playableAsset 不是 TimelineAsset，跳过");
    //         return;
    //     }

    //     int bound = 0;
    //     foreach (var binding in timeline.outputs)
    //     {
    //         var track = binding.sourceObject as TrackAsset;
    //         if (track == null) continue;

    //         if (!string.IsNullOrEmpty(playerTrackName) && track.name == playerTrackName)
    //         {
    //             if (playerAnimator != null)
    //             {
    //                 director.SetGenericBinding(track, playerAnimator);
    //                 bound++;
    //                 Debug.Log($"[LevelSceneManager] 已绑定轨 \"{track.name}\" -> {playerBinding.name} Animator");
    //             }
    //             else
    //                 Debug.LogWarning($"[LevelSceneManager] 角色轨 \"{track.name}\" 需要 Animator，未在 {playerBinding.name} 上找到，跳过");
    //         }
    //     }
    //     Debug.Log($"[LevelSceneManager] BindTimeline 完成，共绑定 {bound} 条轨（仅 Player）");
    // }

    void EnsurePlayerControlEnabled()
    {
        // if (disablePlayerControlDuringTimeline && GameManager.Instance != null)
        //     GameManager.Instance.SetPlayerControlEnabled(true);
        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerControlEnabled(true);
    }

    /// <summary>
    /// 由 LevelEndTrigger 在玩家进入结束区域时调用。
    /// </summary>
    public void OnPlayerReachedEnd()
    {
        if (_isExiting) return;
        _isExiting = true;
        Debug.Log("[LevelSceneManager] 玩家到达结束点");

        // if (exitTimeline != null)
        // {
        //     Debug.Log("[LevelSceneManager] 绑定并播放出口 Timeline");
        //     BindTimelineToRuntimePlayer(exitTimeline);

        //     if (disablePlayerControlDuringTimeline && GameManager.Instance != null)
        //         GameManager.Instance.SetPlayerControlEnabled(false);

        //     DisableAllVCams();
        //     exitTimeline.stopped += OnExitTimelineStopped;
        //     exitTimeline.Play();
        // }
        // else
        // {
        //     Debug.Log("[LevelSceneManager] 无出口 Timeline，直接切场景");
        //     LoadNextScene();
        // }

        LoadNextScene();
    }

    // void OnExitTimelineStopped(PlayableDirector director)
    // {
    //     director.stopped -= OnExitTimelineStopped;
    //     Debug.Log("[LevelSceneManager] 出口 Timeline 播完，切场景");
    //     if (disablePlayerControlDuringTimeline && GameManager.Instance != null)
    //         GameManager.Instance.SetPlayerControlEnabled(true);
    //     LoadNextScene();
    // }

    void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log("[LevelSceneManager] nextSceneName 为空，不加载场景");
            return;
        }

        if (!string.IsNullOrEmpty(transitionSceneName))
        {
            StartCoroutine(LoadTransitionSceneCoroutine());
            return;
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void UpdateCurrentTelepoint(Transform currentEnabledTelepoint)
    {
        Debug.Log("[LevelSceneManager] OnPlayerEnableTrigger");
        _activatedTelepoint = currentEnabledTelepoint;
    }

    public void OnPlayerTeleport()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayer == null) return;
        GameManager.Instance.PlayerTeleport(_activatedTelepoint);
    }

    IEnumerator LoadTransitionSceneCoroutine()
    {
        // TransitionSceneData.Set(nextSceneName, string.IsNullOrEmpty(transitionTextKey) ? nextSceneName : transitionTextKey);
        TransitionSceneData.Set(nextSceneName);

        // 从 AudioManager（跨场景存在）直接保存当前播放的音乐，不依赖场景内 SceneMusicConfig
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.CarryOverCurrentMusic();
        }

        if (TransitionFadeManager.Instance != null && fadeToTransitionDuration > 0f)
        {
            yield return TransitionFadeManager.Instance.FadeIn(fadeToTransitionDuration);
        }

        // Debug.Log($"[LevelSceneManager] 加载过渡场景 {transitionSceneName}，TextKey={transitionTextKey ?? nextSceneName}");
        SceneManager.LoadScene(transitionSceneName);
    }

}
