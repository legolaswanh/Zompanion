using System;
using UnityEngine;

/// <summary>
/// 宝箱触发器：玩家进入 Trigger 范围按 E 打开宝箱 UI，支持拖入拖出物品。
/// </summary>
public class ChestInteractTrigger : MonoBehaviour, ISaveable
{
    [Serializable]
    public class ChestState
    {
        public string[] itemNames;
    }

    [Header("配置")]
    [SerializeField] private int capacity = 20;

    [Header("UI")]
    [SerializeField] private Canvas chestPanel;
    [SerializeField] private StorageDisplay storageDisplay;

    private ChestStorage _storage;
    private InteractButtonCanvasSpawner _spawner;

    public ChestStorage Storage => _storage;

    private void Awake()
    {
        _storage = new ChestStorage(capacity);
        _spawner = GetComponent<InteractButtonCanvasSpawner>();

        if (storageDisplay == null && chestPanel != null)
            storageDisplay = chestPanel.GetComponentInChildren<StorageDisplay>(true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            _spawner?.Show();
            PlayerInteraction.Instance.SetCurrentTrigger(gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            _spawner?.Hide();
            PlayerInteraction.Instance.ClearCurrentTrigger(gameObject);
            CloseChest();
        }
    }

    public void Interact()
    {
        if (storageDisplay == null)
        {
            Debug.LogWarning("[ChestInteractTrigger] storageDisplay 未配置");
            return;
        }

        if (storageDisplay.IsVisible)
        {
            CloseChest();
        }
        else
        {
            OpenChest();
        }
    }

    private void OpenChest()
    {
        storageDisplay.Storage = _storage;
        storageDisplay.Show();
        PlayerMovement.Instance.DisableMove();
    }

    private void CloseChest()
    {
        storageDisplay?.Hide();
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.EnableMove();
    }

    public string CaptureState()
    {
        if (_storage == null)
            return JsonUtility.ToJson(new ChestState { itemNames = Array.Empty<string>() });

        var names = new string[_storage.Slots.Count];
        for (int i = 0; i < _storage.Slots.Count; i++)
        {
            var slot = _storage.Slots[i];
            names[i] = (slot != null && !slot.IsEmpty && slot.itemData != null) ? slot.itemData.itemName : null;
        }
        return JsonUtility.ToJson(new ChestState { itemNames = names });
    }

    public void RestoreState(string stateJson)
    {
        if (string.IsNullOrEmpty(stateJson) || _storage == null)
            return;

        var state = JsonUtility.FromJson<ChestState>(stateJson);
        if (state?.itemNames == null)
            return;

        int count = Mathf.Min(state.itemNames.Length, _storage.Slots.Count);
        for (int i = 0; i < count; i++)
        {
            string name = state.itemNames[i];
            ItemDataSO item = string.IsNullOrEmpty(name) ? null : ItemLookup.Get(name);
            _storage.SetItemAt(i, item);
        }
    }
}
