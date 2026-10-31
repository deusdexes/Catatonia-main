using UnityEngine;
using TMPro; 

public class SimpleDisplayText : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Панель или текстовый объект, который нужно показать/скрыть.")]
    public GameObject uiElement; // Ссылка на InteractionTextPanel

    [Tooltip("Текстовое поле для вывода сообщения. Необязательно, если вы просто показываете картинку.")]
    public TextMeshProUGUI displayText; // Ссылка на Text (TMP)

    [Header("Content")]
    [Tooltip("Текст, который будет отображаться при входе в триггер.")]
    [TextArea(3, 10)] // Делает поле для текста в инспекторе больше и удобнее
    public string messageToShow;

    [Header("Settings")]
    [Tooltip("Тег объекта, который может активировать этот триггер (обычно 'Player').")]
    public string requiredTag = "Player";

    private void Start()
    {
        // Убедимся, что UI элемент выключен при старте сцены
        if (uiElement != null)
        {
            uiElement.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, что в триггер вошел объект с нужным тегом
        if (other.CompareTag(requiredTag))
        {
            // Обновляем текст, если он задан
            if (displayText != null && !string.IsNullOrEmpty(messageToShow))
            {
                displayText.text = messageToShow;
            }

            // Включаем UI элемент
            if (uiElement != null)
            {
                uiElement.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Когда объект с нужным тегом выходит из триггера, выключаем UI
        if (other.CompareTag(requiredTag))
        {
            if (uiElement != null)
            {
                uiElement.SetActive(false);
            }
        }
    }
}