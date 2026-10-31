using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TMP_HoverColorChanger : MonoBehaviour
{
    [Header("Настройки цвета")]
    [Tooltip("Стандартный цвет текста.")]
    public Color normalColor = Color.white;
    [Tooltip("Цвет текста при наведении.")]
    public Color hoverColor = Color.green;

    private TMP_Text m_textComponent;

    void Awake()
    {
        m_textComponent = GetComponent<TMP_Text>();
        if (m_textComponent == null)
        {
            Debug.LogError("TMP_HoverColorChanger требует компонент TextMeshPro!");
            enabled = false;
        }
    }

    void Start()
    {
        // Устанавливаем начальный цвет
        SetColor(normalColor);
    }

    // --- Публичные методы для вызова из других скриптов (например, с кнопок) ---

    public void OnPointerEnter()
    {
        // Debug.Log($"Наведение на текст: {gameObject.name}");
        SetColor(hoverColor);
    }

    public void OnPointerExit()
    {
        // Debug.Log($"Уход с текста: {gameObject.name}");
        SetColor(normalColor);
    }

    private void SetColor(Color color)
    {
        if (m_textComponent != null)
        {
            m_textComponent.color = color;
        }
    }
}