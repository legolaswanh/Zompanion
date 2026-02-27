using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ItemDisplayUI : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    private Action onWindowClosed; // 用于存储关闭窗口后要执行的回调

    public void ShowItem(Sprite icon, string name, string description, Action callback)
    {
        itemIcon.sprite = icon;
        itemNameText.text = name;
        itemDescriptionText.text = description;
        onWindowClosed = callback;
        gameObject.SetActive(true);
        
        // 暂停游戏时间
        Time.timeScale = 0; 
    }


    public void CloseWindow()
    {
        // gameObject.SetActive(false);
        Time.timeScale = 1;
        // 执行后续动作（比如触发对话）
        onWindowClosed?.Invoke();
        Destroy(this.gameObject);
    }
}
