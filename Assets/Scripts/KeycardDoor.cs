using UnityEngine;
using System.Linq; // Для использования LINQ (FirstOrDefault)
using GameSystem;

public class KeycardDoor : SlidingDoor
{
    [Header("Keycard Door Settings")]
    public int requiredKeycardID; // ID ключ-карты, необходимой для открытия этой двери
    public bool unlocked = false; // Состояние замка: true - открыта навсегда, false - требует ключ-карту
    public GameObject lockIndicator; // Объект, который показывает, что дверь заперта (например, красная лампочка)
    public GameObject openIndicator; // Объект, который показывает, что дверь разблокирована (например, зеленая лампочка)
    public Material lockedMaterial; // Материал для lockIndicator (красный)
    public Material unlockedMaterial; // Материал для openIndicator (зеленый)

    [Header("Keycard Door Audio")] // НОВЫЙ РАЗДЕЛ ДЛЯ ЗВУКОВ КАРТОЧНОЙ ДВЕРИ
    public AudioClip lockedSound; // Звук, когда дверь заперта и нет ключ-карты
    public AudioClip unlockSound; // Звук успешной разблокировки

    private Renderer lockIndicatorRenderer;
    private Renderer openIndicatorRenderer;

    protected override void Start()
    {
        base.Start(); // Вызываем Start из базового класса SlidingDoor

        if (lockIndicator != null)
        {
            lockIndicatorRenderer = lockIndicator.GetComponent<Renderer>();
        }
        if (openIndicator != null)
        {
            openIndicatorRenderer = openIndicator.GetComponent<Renderer>();
        }

        UpdateDoorIndicators();
    }

    void UpdateDoorIndicators()
    {
        if (lockIndicator != null) lockIndicator.SetActive(!unlocked);
        if (openIndicator != null) openIndicator.SetActive(unlocked);

        if (lockIndicatorRenderer != null && lockedMaterial != null)
        {
            lockIndicatorRenderer.material = lockedMaterial;
        }
        if (openIndicatorRenderer != null && unlockedMaterial != null)
        {
            openIndicatorRenderer.material = unlockedMaterial;
        }
    }

    public void TryOpenWithKeycard(Inventory playerInventory)
    {
        if (unlocked)
        {
            Debug.Log("KeycardDoor: Дверь уже разблокирована.");
            OpenDoor(); // Используем метод из SlidingDoor для открытия (он уже проигрывает звук)
            return;
        }

        Debug.Log($"KeycardDoor: Попытка открыть дверь. Требуется Keycard ID: {requiredKeycardID}");

        // Проверяем инвентарь игрока на наличие нужной ключ-карты
        for (int i = 0; i < playerInventory.inventorySize; i++)
        {
            Item itemInSlot = playerInventory.GetItemInSlot(i);
            if (itemInSlot != null && itemInSlot is KeycardItem keycard)
            {
                if (keycard.keycardData != null && keycard.keycardData.keycardID == requiredKeycardID)
                {
                    // Найдена нужная ключ-карта!
                    Debug.Log($"KeycardDoor: Найдена ключ-карта ID: {requiredKeycardID} в слоте {i}. Дверь разблокирована!");
                    unlocked = true; // Разблокируем дверь навсегда
                    playerInventory.RemoveItem(i); // Удаляем ключ-карту из инвентаря

                    if (QuestManager.Instance != null)
                    {
                        if (requiredKeycardID == 0) // ID ключ-карты для первого квеста
                        {
                            QuestManager.Instance.CompleteObjective("QUEST_OPEN_KEYCARD_DOOR");
                        }
                        else if (requiredKeycardID == 1) // ID ключ-карты для второго квеста
                        {
                            QuestManager.Instance.CompleteObjective("QUEST_OPEN_ENGINEERING");
                        }
                        else if (requiredKeycardID == 5) // ID ключ-карты для второго квеста
                        {
                            QuestManager.Instance.CompleteObjective("QUEST_OPEN_TECH");
                        }
                    }

                    PlaySound(unlockSound); // Воспроизводим звук разблокировки
                    OpenDoor(); // Открываем дверь сразу после разблокировки (звук открытия будет из базового класса)

                    UpdateDoorIndicators(); // Обновляем индикаторы
                    return;
                }
            }
        }

        Debug.Log("KeycardDoor: У игрока нет нужной ключ-карты или инвентарь пуст.");
        PlaySound(lockedSound); // Воспроизводим звук "заперто"
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (unlocked)
        {
            base.OnTriggerEnter(other); // Если разблокирована, используем логику базового класса
        }
        else
        {
            Debug.Log("KeycardDoor: Дверь заперта, требуется ключ-карта.");
            // Звук запертой двери будет воспроизводиться только при попытке открыть с помощью TryOpenWithKeycard.
            // Если вы хотите, чтобы он играл просто при входе в триггер, раскомментируйте строку ниже:
            // PlaySound(lockedSound);
        }
    }

    protected override void OnTriggerExit(Collider other)
    {
        if (unlocked)
        {
            base.OnTriggerExit(other);
        }
        else
        {
            // Не делаем ничего, если дверь заперта, она не должна реагировать на выход из триггера.
        }
    }
}