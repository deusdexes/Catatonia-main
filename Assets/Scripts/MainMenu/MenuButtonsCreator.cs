using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuButtonsCreator : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private string[] buttonNames = { "ИГРАТЬ", "НАСТРОЙКИ", "ТИТРЫ", "ВЫХОД" };

    void Start()
    {
        CreateMenuButtons();
    }

    void CreateMenuButtons()
    {
        foreach (string buttonName in buttonNames)
        {
            // Создаем кнопку как дочерний объект
            GameObject button = Instantiate(buttonPrefab, transform);

            // Настраиваем текст кнопки
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = buttonName;
            }

            // Настраиваем обработчик клика
            Button btnComponent = button.GetComponent<Button>();
            if (btnComponent != null)
            {
                // Добавляем обработчик в зависимости от имени кнопки
                switch (buttonName)
                {
                    case "ИГРАТЬ":
                        btnComponent.onClick.AddListener(PlayGame);
                        break;
                    case "НАСТРОЙКИ":
                        btnComponent.onClick.AddListener(ShowOptions);
                        break;
                    case "ТИТРЫ":
                        btnComponent.onClick.AddListener(ShowCredits);
                        break;
                    case "ВЫХОД":
                        btnComponent.onClick.AddListener(QuitGame);
                        break;
                }
            }
        }
    }

    void PlayGame() { /* реализация */ }
    void ShowOptions() { /* реализация */ }
    void ShowCredits() { /* реализация */ }
    void QuitGame() { /* реализация */ }
}