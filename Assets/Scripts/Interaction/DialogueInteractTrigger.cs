using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 对话触发器：玩家进入 Trigger 范围按 E 触发对话。
/// 用于僵尸时，勾选「作为僵尸对话」并设置 definitionId，可在对话内使用「提交物品」相关 Condition 和 Sequencer。
/// </summary>
public class DialogueInteractTrigger : MonoBehaviour, ISaveable
{
    [System.Serializable]
    public class DialogueInteractTriggerState
    {
        public bool isTalking;
    }

    [Header("Zombie（可选）")]
    [Tooltip("作为僵尸对话时勾选，用于对话内「提交物品」选项。definitionId 可留空，有 ZombieAgent 时自动获取。")]
    [SerializeField] private bool isZombieDialogue;
    [SerializeField] private string zombieDefinitionId;

    private bool isTalking;
    private ZombieAgent _zombieAgent;
    private InteractButtonCanvasSpawner _spawner;

    private void Awake()
    {
        if (isZombieDialogue && string.IsNullOrWhiteSpace(zombieDefinitionId))
            _zombieAgent = GetComponent<ZombieAgent>();
        _spawner = GetComponent<InteractButtonCanvasSpawner>();
    }

    private void OnEnable()
    {
        if (DialogueManager.instance != null)
        {
            DialogueManager.instance.conversationStarted += OnConversationStarted;
            DialogueManager.instance.conversationEnded += OnConversationEnded;
        }
    }

    private void OnDisable()
    {
        if (DialogueManager.instance != null)
        {
            DialogueManager.instance.conversationStarted -= OnConversationStarted;
            DialogueManager.instance.conversationEnded -= OnConversationEnded;
        }
    }

    public void Interact()
    {
        if (isTalking) return;
        if (DialogueManager.isConversationActive) return;

        if (isZombieDialogue)
        {
            string definitionId = GetZombieDefinitionId();
            if (!string.IsNullOrWhiteSpace(definitionId))
                DialogueLua.SetVariable("CurrentZombieDefinitionId", definitionId);
        }

        DialogueSystemTrigger trigger = GetComponent<DialogueSystemTrigger>();
        if (trigger != null)
            trigger.OnUse();

        isTalking = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            _spawner?.Show();
            PlayerInteraction.Instance?.SetCurrentTrigger(gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            _spawner?.Hide();
            PlayerInteraction.Instance?.ClearCurrentTrigger(gameObject);
        }
    }

    private void OnConversationStarted(Transform actor)
    {
        isTalking = true;
        if (isZombieDialogue)
        {
            string definitionId = GetZombieDefinitionId();
            if (!string.IsNullOrWhiteSpace(definitionId))
            {
                DialogueLua.SetVariable("CurrentZombieDefinitionId", definitionId);
            }
        }
    }

    private void OnConversationEnded(Transform actor)
    {
        isTalking = false;
        if (isZombieDialogue)
            DialogueLua.SetVariable("CurrentZombieDefinitionId", string.Empty);
    }

    private string GetZombieDefinitionId()
    {
        if (!string.IsNullOrWhiteSpace(zombieDefinitionId))
            return zombieDefinitionId;
        if (_zombieAgent != null && _zombieAgent.Definition != null)
            return _zombieAgent.Definition.DefinitionId;
        return null;
    }

    // ── ISaveable ──

    public string CaptureState()
    {
        return JsonUtility.ToJson(new DialogueInteractTriggerState { isTalking = isTalking });
    }

    public void RestoreState(string stateJson)
    {
        var state = JsonUtility.FromJson<DialogueInteractTriggerState>(stateJson);
        isTalking = state.isTalking;
    }
}