using UnityEngine;
using PixelCrushers.DialogueSystem;

public class DialogueInteractTrigger : MonoBehaviour, ISaveable
{
    [System.Serializable]
    public class DialogueInteractTriggerState
    {
        public bool isTalking;
    }


    private bool isTalking = false;
    public Canvas buttonCanvas;

    // 这个方法和 DiggingTrigger 里的 Interact 同构
    public void Interact()
    {
        if (isTalking) return;
        
        // 检查对话是否正在进行（防止重复触发）
        if (DialogueManager.isConversationActive) return;
        
        // 触发对话！
        DialogueSystemTrigger trigger = GetComponent<DialogueSystemTrigger>();
        trigger.OnUse();
        isTalking = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            buttonCanvas.gameObject.SetActive(true);
            PlayerInteraction.Instance.SetCurrentTrigger(this.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        buttonCanvas.gameObject.SetActive(false);
        if (collision.CompareTag("Player"))
        {
            buttonCanvas.gameObject.SetActive(false);
            PlayerInteraction.Instance.ClearCurrentTrigger(this.gameObject);
        }
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