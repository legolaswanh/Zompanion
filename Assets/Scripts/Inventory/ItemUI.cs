using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public ItemDataSO itemData; // 运行时赋值
    public Image iconImage;     // 在 Inspector 中拖入当前的 Image 组件

    // 关键方法：背包系统刷新时调用这个
    public void SetItem(ItemDataSO data)
    {
        itemData = data;
        if (itemData != null)
        {
            iconImage.sprite = itemData.icon;
            iconImage.enabled = true; // 显示图片
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false; // 没物品时隐藏
        }
    }
}