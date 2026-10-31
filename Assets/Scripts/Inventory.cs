using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int inventorySize = 3;
    public float showDuration = 3f;

    [Header("UI References")]
    public CanvasGroup inventoryPanel;
    public Image[] slotBackgrounds;
    public Image[] slotIcons;
    public TextMeshProUGUI[] slotNumbers;
    public GameObject selectedSlotHighlight;

    [Header("Hand References")]
    public Transform handPosition;
    public float itemShowDistance = 0.5f;
    public Camera playerCamera; // Ссылка на камеру игрока для определения направления броска

    [Header("Throwing")]
    public ThrowPreview throwPreview; // Ссылка на новый компонент ThrowPreview

    private Item[] items;
    private GameObject currentItemInHand;
    private int currentSlot = -1;
    private float hideTimer = 0f;
    private bool isVisible = false;

    void Start()
    {
        items = new Item[inventorySize];

        if (playerCamera == null)
        {
            playerCamera = Camera.main; // Пытаемся найти главную камеру, если не установлена
            if (playerCamera == null)
            {
                Debug.LogError("Inventory: Player Camera not assigned and 'MainCamera' tag not found!");
            }
        }

        if (throwPreview == null)
        {
            throwPreview = FindFirstObjectByType<ThrowPreview>();
            if (throwPreview == null)
            {
                Debug.LogError("Inventory: ThrowPreview component not found in scene!");
            }
        }

        // Скрываем инвентарь при старте
        if (inventoryPanel != null)
        {
            inventoryPanel.alpha = 0f;
            inventoryPanel.interactable = false;
            inventoryPanel.blocksRaycasts = false;
        }

        // Скрываем выделение
        if (selectedSlotHighlight != null)
        {
            selectedSlotHighlight.SetActive(false);
        }

        // Инициализируем слоты
        UpdateUI();

        if (throwPreview != null)
        {
            throwPreview.DisablePreview(); // Убеждаемся, что траектория отключена при старте
        }
    }

    void Update()
    {
        // Таймер для скрытия инвентаря
        if (isVisible)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                HideInventory();
            }
        }

        // Отрисовка траектории, если в руке LureItem
        if (throwPreview != null && currentSlot != -1 && items[currentSlot] is LureItem lureItem)
        {
            if (playerCamera != null && handPosition != null && lureItem != null && !lureItem.IsThrown)
            {
                // Начальная позиция для траектории должна быть немного впереди руки, чтобы не сталкиваться сразу с игроком
                Vector3 startPos = handPosition.position + playerCamera.transform.forward * 0.1f;
                Vector3 initialVel = playerCamera.transform.forward * lureItem.throwForce;
                throwPreview.SimulateTrajectory(startPos, initialVel, lureItem.impactLayers);
            }
            else
            {
                throwPreview.DisablePreview();
            }
        }
    }

    // Публичные методы для вызова из Unity Events
    public void OnSlot1()
    {
        SelectSlot(0);
    }

    public void OnSlot2()
    {
        SelectSlot(1);
    }

    public void OnSlot3()
    {
        SelectSlot(2);
    }

    public void OnDrop()
    {
        DropCurrentItem();
    }

    public void OnUseEquippedItem()
    {
        if (currentSlot >= 0 && currentSlot < inventorySize && items[currentSlot] != null)
        {
            Item itemToUse = items[currentSlot];
            Debug.Log($"Using item in slot {currentSlot}: {itemToUse.itemName}");

            itemToUse.Use();

            if (itemToUse is LureItem lureItem && lureItem.IsThrown)
            {
                if (throwPreview != null) throwPreview.DisablePreview(); // Отключаем траекторию после броска
                RemoveItem(currentSlot);
                currentSlot = -1;
                UpdateUI();
                ShowInventory();
            }
            else
            {
                // Для других типов предметов
            }
        }
        else
        {
            Debug.Log("No item equipped to use.");
        }
    }

    public bool AddItem(Item itemToAdd)
    {
        if (itemToAdd == null)
        {
            Debug.LogError("Trying to add null item to inventory!");
            return false;
        }

        for (int i = 0; i < inventorySize; i++)
        {
            if (items[i] == null)
            {
                items[i] = itemToAdd;

                itemToAdd.OnPickup();

                UpdateUI();
                ShowInventory();
                Debug.Log($"Item '{itemToAdd.itemName}' added to slot {i + 1}");
                return true;
            }
        }

        Debug.Log("Inventory is full! Cannot add: " + itemToAdd.itemName);
        return false;
    }

    public void RemoveItem(int slot)
    {
        if (slot >= 0 && slot < inventorySize)
        {
            if (items[slot] != null)
            {
                if (currentSlot == slot)
                {
                    HideItemInHand();
                }

                items[slot] = null;
                UpdateUI();
            }
        }
    }

    void SelectSlot(int slot)
    {
        if (slot < 0 || slot >= inventorySize) return;

        Debug.Log($"Selecting slot {slot}");

        if (currentSlot == slot)
        {
            HideItemInHand();
            currentSlot = -1;
        }
        else
        {
            HideItemInHand();

            currentSlot = slot;
            if (items[slot] != null)
            {
                ShowItemInHand(items[slot]);
            }
        }

        UpdateUI();
        ShowInventory();
    }

    void ShowItemInHand(Item item)
    {
        if (handPosition == null)
        {
            Debug.LogError("Hand position is not set!");
            return;
        }

        if (item == null)
        {
            Debug.LogError("Trying to show null item in hand!");
            return;
        }

        item.gameObject.SetActive(true);

        Rigidbody itemRb = item.GetComponent<Rigidbody>();
        if (itemRb != null)
        {
            itemRb.isKinematic = true;
            itemRb.useGravity = false;
            itemRb.linearVelocity = Vector3.zero;
            itemRb.angularVelocity = Vector3.zero;
        }
        Collider itemCollider = item.GetComponent<Collider>();
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        item.OnEquip(handPosition);

        currentItemInHand = item.gameObject;

        // Если это LureItem, включаем предпросмотр траектории
        if (item is LureItem && throwPreview != null)
        {
            // Предпросмотр будет обновляться в Update()
        }
        else if (throwPreview != null)
        {
            throwPreview.DisablePreview(); // Отключаем, если это не LureItem
        }

        Debug.Log($"Showing {item.itemName} in hand");
    }

    void HideItemInHand()
    {
        if (currentItemInHand != null)
        {
            Item item = currentItemInHand.GetComponent<Item>();
            if (item != null)
            {
                item.StopUse();

                if (item is LureItem lureItem && lureItem.IsThrown)
                {
                    currentItemInHand = null;
                    Debug.Log("Hiding item from hand (LureItem was thrown)");
                    return;
                }

                Rigidbody itemRb = item.GetComponent<Rigidbody>();
                if (itemRb != null)
                {
                    itemRb.isKinematic = true;
                    itemRb.useGravity = false;
                    itemRb.linearVelocity = Vector3.zero;
                    itemRb.angularVelocity = Vector3.zero;
                }
                Collider itemCollider = item.GetComponent<Collider>();
                if (itemCollider != null)
                {
                    itemCollider.enabled = false;
                }

                currentItemInHand.transform.SetParent(null);
                currentItemInHand.SetActive(false);
            }
            currentItemInHand = null;

            if (throwPreview != null) throwPreview.DisablePreview(); // Отключаем траекторию при скрытии предмета
        }
        else
        {
            if (throwPreview != null) throwPreview.DisablePreview(); // Отключаем траекторию, если currentItemInHand уже null
        }
        Debug.Log("Hiding item from hand"); // Перемещено для единообразия
    }

    void DropCurrentItem()
    {
        if (currentSlot >= 0 && items[currentSlot] != null)
        {
            Debug.Log($"Dropping item from slot {currentSlot}");

            Item itemToDrop = items[currentSlot];

            // --- ИЗМЕНЕНИЕ: Логика возврата предмета ---
            // Получаем компонент ItemPickup на выбрасываемом объекте
            ItemPickup itemPickupComponent = itemToDrop.GetComponent<ItemPickup>();

            if (itemPickupComponent != null)
            {
                // Вызываем наш новый метод, который вернет предмет на его исходное место
                itemPickupComponent.HandleDrop();
            }
            else
            {
                // Если по какой-то причине компонента нет, используем старую логику
                Debug.LogWarning($"Item '{itemToDrop.itemName}' does not have an ItemPickup component. Using default drop logic.");
                itemToDrop.OnDrop(); // OnDrop() должен как минимум активировать объект и отсоединить от родителя
            }

            // --- Конец изменений ---

            if (currentItemInHand == itemToDrop.gameObject)
            {
                currentItemInHand = null;
            }

            // Удаляем предмет из инвентаря
            items[currentSlot] = null;
            currentSlot = -1;
            UpdateUI();

            if (throwPreview != null) throwPreview.DisablePreview();
            Debug.Log($"Dropped {itemToDrop.itemName}");
        }
        else
        {
            Debug.Log("No item to drop");
        }
    }

    void ShowInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.alpha = 1f;
            inventoryPanel.interactable = true;
            inventoryPanel.blocksRaycasts = true;
        }

        isVisible = true;
        hideTimer = showDuration;
    }

    void HideInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.alpha = 0f;
            inventoryPanel.interactable = false;
            inventoryPanel.blocksRaycasts = false;
        }

        isVisible = false;
        hideTimer = 0f;
    }

    void UpdateUI()
    {
        for (int i = 0; i < inventorySize; i++)
        {
            // --- ИЗМЕНЕНИЕ: Логика отображения иконок ---
            if (slotIcons[i] != null)
            {
                if (items[i] != null && items[i].itemIcon != null)
                {
                    // Если предмет есть, включаем Image, назначаем спрайт
                    slotIcons[i].enabled = true;
                    slotIcons[i].sprite = items[i].itemIcon;
                    slotIcons[i].preserveAspect = true;
                }
                else
                {
                    // Если предмета нет, выключаем Image, чтобы он стал невидимым
                    slotIcons[i].enabled = false;
                }
            }

            if (slotNumbers[i] != null)
            {
                slotNumbers[i].text = (i + 1).ToString();
                slotNumbers[i].color = currentSlot == i ? Color.yellow : Color.white;
            }

            // --- ИЗМЕНЕНИЕ: Удалена динамическая смена цвета фона слота ---
            // Теперь цвет и альфа фона настраиваются в редакторе и не меняются здесь
            /*
            if (slotBackgrounds[i] != null)
            {
                slotBackgrounds[i].color = items[i] != null ?
                    new Color(0.3f, 0.3f, 0.3f, 0.8f) :
                    new Color(0.1f, 0.1f, 0.1f, 0.5f);
            }
            */
        }

        UpdateSelectionHighlight();
    }


    void UpdateSelectionHighlight()
    {
        if (selectedSlotHighlight != null)
        {
            if (currentSlot >= 0 && currentSlot < inventorySize && slotBackgrounds[currentSlot] != null)
            {
                selectedSlotHighlight.SetActive(true);

                Transform slotTransform = slotBackgrounds[currentSlot].transform;
                selectedSlotHighlight.transform.SetParent(slotTransform);

                RectTransform highlightRect = selectedSlotHighlight.GetComponent<RectTransform>();
                if (highlightRect != null)
                {
                    highlightRect.anchorMin = Vector2.zero;
                    highlightRect.anchorMax = Vector2.one;
                    highlightRect.offsetMin = Vector2.zero;
                    highlightRect.offsetMax = Vector2.zero;
                    highlightRect.localScale = Vector3.one;
                }

                Debug.Log($"Highlighting slot {currentSlot}");
            }
            else
            {
                selectedSlotHighlight.SetActive(false);
                Debug.Log("No slot to highlight");
            }
        }
        else
        {
            Debug.LogError("Selected slot highlight is not assigned!");
        }
    }

    public Item GetItemInSlot(int slot)
    {
        if (slot >= 0 && slot < inventorySize)
            return items[slot];
        return null;
    }

    public Item GetCurrentItemInHand()
    {
        if (currentSlot >= 0 && currentSlot < inventorySize && items[currentSlot] != null)
        {
            return items[currentSlot];
        }
        return null;
    }

    public void DetachItemFromHandForExamine(Item itemToDetach)
    {
        if (itemToDetach == null || currentItemInHand == null || currentItemInHand != itemToDetach.gameObject)
        {
            Debug.LogError("Inventory: Attempted to detach an item that is not currently in hand or is null.");
            return;
        }

        itemToDetach.StopUse();
        currentItemInHand.transform.SetParent(null);
        Debug.Log($"Inventory: Item '{itemToDetach.itemName}' detached from hand for examination.");
    }

    public void ReAttachItemToHandAfterExamine(Item itemToReattach)
    {
        if (itemToReattach == null) return;

        if (currentSlot >= 0 && currentSlot < inventorySize && items[currentSlot] == itemToReattach)
        {
            if (handPosition == null)
            {
                Debug.LogError("Inventory: Hand position is not set!");
                return;
            }

            itemToReattach.gameObject.SetActive(true);
            itemToReattach.OnEquip(handPosition);
            itemToReattach.Use(); // Возможно, не всегда нужно вызывать Use() здесь, зависит от логики вашего Item
            currentItemInHand = itemToReattach.gameObject;
            Debug.Log($"Inventory: Item '{itemToReattach.itemName}' re-attached to hand after examination.");
        }
        else
        {
            itemToReattach.transform.SetParent(null);
            itemToReattach.gameObject.SetActive(true);
            Debug.LogWarning($"Inventory: Item '{itemToReattach.itemName}' was not in selected slot ({currentSlot}) after examination. It's now detached from any parent. Consider implementing proper drop/re-pickup logic if inventory state changed during examine.");
        }
    }

    public bool HasItem(string itemName)
    {
        // Проверяем наличие предмета по его имени (item.itemName) в массиве items
        // Сравниваем itemName с каждым элементом в инвентаре.
        // Этот подход предполагает, что item.itemName уникален или вы хотите найти любой предмет с таким именем.
        for (int i = 0; i < inventorySize; i++)
        {
            if (items[i] != null && items[i].itemName == itemName)
            {
                return true;
            }
        }
        return false;
    }
}