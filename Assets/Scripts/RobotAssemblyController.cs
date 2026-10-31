using UnityEngine;
using System.Collections.Generic;
using GameSystem;

public class RobotAssemblyController : MonoBehaviour
{
    // Сериализуемый класс для определения каждого "гнезда" для части робота
    [System.Serializable]
    public class RobotPartSocket
    {
        [Tooltip("Уникальный идентификатор для этого гнезда (например, 'LeftArm'). Должен совпадать с partIdentifier у RobotPartItem.")]
        public string partIdentifier; // Например, "LeftArm", "Head" - ДОЛЖЕН совпадать с RobotPartItem.partIdentifier
        [Tooltip("Ссылка на GameObject, который является визуальной частью на основной модели робота.")]
        public GameObject partGameObject; // Актуальная mesh/модель части на основном роботе
        [Tooltip("Указывает, присоединена ли эта часть уже к роботу.")]
        public bool isAttached = false; // Состояние этой конкретной части
    }

    [Header("Robot Assembly Settings")]
    [Tooltip("Список всех частей, необходимых для сборки робота.")]
    public List<RobotPartSocket> partSockets; // Список всех частей, необходимых для робота

    [Header("Dependencies")]
    [Tooltip("Ссылка на инвентарь игрока. Если не задано, будет найдено автоматически.")]
    public Inventory playerInventory; // Ссылка на инвентарь игрока
    [Tooltip("Ссылка на CreatureController в сцене.")] // Новый параметр
    public CreatureController creatureController; // Ссылка на контроллер монстра

    [Header("Audio")]
    [Tooltip("Звук, проигрываемый при успешном присоединении части.")]
    public AudioClip attachSound;
    private AudioSource audioSource;
     
    [Header("Quest Integration")] // Новый заголовок
    [Tooltip("Конфигурация сокращения таймера при успешном присоединении части.")]
    public TimerReductionConfig attachReductionConfig; // Конфигурация для вставки части

    [Header("Ссылки на тележку")] // Новый заголовок
    public CartController cartController; // Новое поле для ссылки на CartController

    private bool questCompleted = false; // Флаг завершения квеста

    void Awake()
    {
        // Попытка найти CartController, если не назначен в Инспекторе
        if (cartController == null)
        {
            cartController = FindAnyObjectByType<CartController>();
            if (cartController == null)
            {
                Debug.LogWarning("CartController не найден в сцене или не назначен RobotAssemblyController!", this);
            }
        }
    }
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        foreach (var part in partSockets)
        {
            if (part.partGameObject != null)
            {
                part.partGameObject.SetActive(part.isAttached);
            }
            else
            {
                Debug.LogWarning($"RobotAssemblyController: Объект части (GameObject) равен null для идентификатора '{part.partIdentifier}'. Пожалуйста, назначьте его в Инспекторе.");
            }
        }


        if (playerInventory == null)
        {
            playerInventory = FindAnyObjectByType<Inventory>();
            if (playerInventory == null)
            {
                Debug.LogError("RobotAssemblyController: Инвентарь игрока не назначен и не найден в сцене!");
            }
        }
        // Попытка найти CreatureController, если не назначен вручную
        if (creatureController == null)
        {
            creatureController = FindAnyObjectByType<CreatureController>();
            if (creatureController == null)
            {
                Debug.LogWarning($"RobotAssemblyController: CreatureController не назначен. Сокращение таймера при сборке работать не будет.");
            }
        }
        if (attachReductionConfig == null)
        {
            Debug.LogWarning($"RobotAssemblyController: Attach Reduction Config не назначен. Сокращение таймера при сборке не будет настроено.");
        }


        CheckQuestCompletion();
    }

    public void OnInteract(Inventory interactingInventory)
    {
        if (questCompleted)
        {
            Debug.Log("Робот уже полностью собран!");
            return;
        }

        Debug.Log("Взаимодействие с контроллером сборки робота. Проверка инвентаря на наличие частей...");
        playerInventory = interactingInventory;

        bool partAttachedThisInteraction = false;

        for (int i = 0; i < playerInventory.inventorySize; i++)
        {
            Item itemInSlot = playerInventory.GetItemInSlot(i);

            if (itemInSlot != null && itemInSlot is RobotPartItem robotPart)
            {
                foreach (var socket in partSockets)
                {
                    if (!socket.isAttached && socket.partIdentifier == robotPart.partIdentifier)
                    {
                        Debug.Log($"Присоединение {robotPart.itemName} ({robotPart.partIdentifier}) к роботу.");

                        playerInventory.RemoveItem(i);

                        if (socket.partGameObject != null)
                        {
                            socket.partGameObject.SetActive(true);
                        }
                        socket.isAttached = true;
                        partAttachedThisInteraction = true;

                        if (attachSound != null)
                        {
                            audioSource.PlayOneShot(attachSound);
                        }

                        // <<<--- НОВЫЙ КОД ДЛЯ СОКРАЩЕНИЯ ТАЙМЕРА ПРИ ВСТАВКЕ
                        if (creatureController != null && attachReductionConfig != null)
                        {
                            float reductionAmount = Random.Range(attachReductionConfig.minReduction, attachReductionConfig.maxReduction);
                            creatureController.ReduceTimer(reductionAmount);
                        }
                        else if (creatureController == null)
                        {
                            Debug.LogWarning($"CreatureController не назначен, таймер монстра не сокращен при присоединении части.");
                        }
                        else if (attachReductionConfig == null)
                        {
                            Debug.LogWarning($"Attach Reduction Config не назначен, таймер монстра не сокращен при присоединении части.");
                        }
                        // <<<--- КОНЕЦ НОВОГО КОДА

                        Destroy(robotPart.gameObject);

                        goto EndInteractionCheck;
                    }
                }
            }
        }

    EndInteractionCheck:
        if (partAttachedThisInteraction)
        {
            Debug.Log("Часть робота успешно присоединена.");
        }
        else
        {
            Debug.Log("В инвентаре не найдено подходящих частей робота, или все части уже присоединены для этого слота.");
        }

        CheckQuestCompletion();
    }

    void CheckQuestCompletion()
    {
        bool allPartsAreAttached = true;
        foreach (var part in partSockets)
        {
            if (part.partGameObject == null)
            {
                Debug.LogWarning($"RobotAssemblyController: Part GameObject для '{part.partIdentifier}' не назначен, поэтому робот не может быть полностью собран.");
                allPartsAreAttached = false;
                continue;
            }

            if (!part.isAttached)
            {
                allPartsAreAttached = false;
                break;
            }
        }

        if (allPartsAreAttached && !questCompleted)
        {
            questCompleted = true;
            Debug.Log("Робот полностью собран! Квест завершен!");

            // НОВОЕ: Информируем QuestManager о завершении финального квеста
            if (QuestManager.Instance != null)
            {
                // ID должен совпадать с ID, который вы настроите в Инспекторе
                QuestManager.Instance.CompleteObjective("QUEST_ATTACH_ROBOT_PARTS");
            }
            // КОНЕЦ НОВОГО КОДА

            // НОВОЕ: Информируем CartController о том, что робот собран
            if (cartController != null)
            {
                cartController.SetRobotAssembled(true);
            }

            // TODO: Запустить следующий этап квеста, активировать функционал робота, проиграть анимацию победы и т.д.
        }
        else if (allPartsAreAttached && questCompleted)
        {
            Debug.Log("Робот остается полностью собранным.");
        }
        else
        {
            Debug.Log("Робот еще не полностью собран.");
        }
    }

}