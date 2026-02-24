using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CameraControl : MonoBehaviour
{
    public static CameraControl Instance;

    /// <summary>
    /// 获取挂在本物体上的 CinemachineCamera 所在 GameObject。
    /// 用于跨场景时 GameSceneManager 等无法在 Inspector 拖入 Persistent 相机时，在运行时解析引用。
    /// </summary>
    public GameObject VCamGameObject => gameObject;

    private void Awake() 
    {
        if(Instance != null) 
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 实际渲染的 Main Camera（Camera + CinemachineBrain）在 Persistent 里是另一个物体；
            // 只 DontDestroyOnLoad 本物体（VCam）会导致切场景时 Main Camera 被销毁、画面全黑。
            // 这里把带 Tag MainCamera 的物体也持久化，保证跨场景后仍有可用的 Camera。
            GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCam != null && mainCam != gameObject)
            {
                DontDestroyOnLoad(mainCam);
            }
        }
    }

    public void SwitchConfinerShape()
    {
        PolygonCollider2D confinerShape = GameObject.FindGameObjectWithTag("Bounds").GetComponent<PolygonCollider2D>();
        CinemachineConfiner2D confiner = GetComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = confinerShape;

        confiner.InvalidateLensCache();
    }

    public void FollowMainRole()
    {
        Transform mainRole = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        CinemachineCamera virtualCamera = GetComponent<CinemachineCamera>();
        virtualCamera.Follow = mainRole;
    }
}

