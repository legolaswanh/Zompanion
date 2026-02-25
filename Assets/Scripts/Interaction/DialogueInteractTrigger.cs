using UnityEngine;
using PixelCrushers.DialogueSystem;

public class DialogueInteractTrigger : MonoBehaviour
{
    [Header("对话配置")]
    [Tooltip("在 Dialogue Database 中创建的 Conversation 标题")]
    [SerializeField] private string conversationTitle;
    
    // 对话目标（通常是这个 NPC 自己），DS 摄像机会用到
    [SerializeField] private Transform conversant; 

    private bool isTalking = false;
    public Canvas buttonCanvas;

    // 这个方法和 DiggingTrigger 里的 Interact 同构
    public void Interact()
    {
        if (isTalking) return;
        
        // 检查对话是否正在进行（防止重复触发）
        if (DialogueManager.isConversationActive) return;

        Debug.Log($"[Dialogue] 开始对话: {conversationTitle}");
        
        // 触发对话！
        DialogueManager.StartConversation(conversationTitle, PlayerInteraction.Instance.transform, conversant);
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
}