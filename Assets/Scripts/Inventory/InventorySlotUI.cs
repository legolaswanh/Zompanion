using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Button slotButton; // 可选：用于点击交互

    /// <summary>Prefab 里给 Image 配的默认图，空槽位时恢复显示这个，不禁用 Image。</summary>
    private Sprite _defaultSlotSprite;
    private InventorySlot currentSlot;

    private void Awake()
    {
        if (iconImage != null)
            _defaultSlotSprite = iconImage.sprite;
    }

    // 刷新格子的显示
    public void UpdateSlotDisplay(InventorySlot slot)
    {
        currentSlot = slot;

        if (slot.itemData != null)
        {
            // 有物品：显示物品图标和数量
            iconImage.sprite = slot.itemData.icon;
            iconImage.color = Color.white;
            iconImage.enabled = true;

            if (slot.quantity > 1)
            {
                amountText.text = slot.quantity.ToString();
                amountText.enabled = true;
            }
            else
            {
                amountText.enabled = false;
            }
        }
        else
        {
            // 无物品：保持 Image 启用，恢复 Prefab 上配置的默认图，不隐藏
            iconImage.sprite = _defaultSlotSprite;
            iconImage.color = new Color(1,1,1,0);
            iconImage.enabled = true;
            amountText.enabled = false;
        }
    }
}
