using UnityEngine;
using UnityEngine.UI; // Этот using теперь необязателен, если вы не используете другие UI-элементы Legacy
using TMPro; // <<< ДОБАВЬТЕ ЭТУ СТРОКУ

public class NumberSlot : MonoBehaviour
{
    public TextMeshProUGUI numberText; // <<< ИЗМЕНИТЕ ТИП ПОЛЯ НА TextMeshProUGUI

    private int currentValue = 0; // Текущее значение этого слота

    // Метод для инициализации (вызывается извне)
    public void Initialize()
    {
        // Проверка на null, чтобы избежать ошибки, если numberText не назначен
        if (numberText == null)
        {
            Debug.LogError($"NumberSlot: Поле 'numberText' не назначено для объекта {gameObject.name}. Пожалуйста, перетащите компонент TextMeshProUGUI в инспекторе.", this);
            return; // Выходим, если ссылка null, чтобы не было NullReferenceException
        }
        UpdateNumberText();
    }

    // Метод для увеличения числа
    public void IncreaseNumber()
    {
        currentValue++;
        if (currentValue > 9)
        {
            currentValue = 0; // Переход от 9 к 0
        }
        UpdateNumberText();
    }

    // Метод для уменьшения числа
    public void DecreaseNumber()
    {
        currentValue--;
        if (currentValue < 0)
        {
            currentValue = 9; // Переход от 0 к 9
        }
        UpdateNumberText();
    }

    // Обновление текстового отображения числа
    private void UpdateNumberText()
    {
        // Повторная проверка на null, так как этот метод может быть вызван напрямую или через Initialize
        if (numberText == null)
        {
            return;
        }
        numberText.text = currentValue.ToString();
    }

    // Метод для получения текущего значения (для менеджера загадки)
    public int GetCurrentValue()
    {
        return currentValue;
    }
}