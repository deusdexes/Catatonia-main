using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // Для TextMeshProUGUI
using System.Collections;

public class LoadingScreenManager : MonoBehaviour
{
    // Ссылки на элементы UI
    public GameObject loadingScreenPanel; // Панель, содержащая все элементы загрузки (чтобы можно было включать/выключать)
    public Slider progressBar;          // Слайдер для отображения прогресса
    public TextMeshProUGUI progressText; // Текст для отображения процента загрузки

    // Статическая переменная для хранения индекса или имени сцены, которую нужно загрузить
    public static string sceneToLoad; // Имя сцены, которую нужно загрузить

    void Start()
    {
        // Убедимся, что панель загрузки активна при старте этой сцены
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("LoadingScreenManager: LoadingScreenPanel не назначена!");
            return;
        }

        // Запускаем корутину для загрузки сцены
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            StartCoroutine(LoadAsyncScene());
        }
        else
        {
            Debug.LogError("LoadingScreenManager: Не указана сцена для загрузки! Загрузка отменена.");
            // Можно добавить логику для возврата в главное меню или стартовую сцену
            // Например: SceneManager.LoadScene("MainMenu");
        }
    }

    IEnumerator LoadAsyncScene()
    {
        // Мы хотим, чтобы загружаемая сцена не активировалась сразу
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false; // Ждем, пока мы сами не захотим её активировать

        float progress = 0;

        while (!operation.isDone)
        {
            // Прогресс загрузки сцены. Clamp01 гарантирует, что значение не выйдет за пределы [0, 1]
            // Прогресс идет только до 0.9, так как последние 0.1 - это активация сцены
            progress = Mathf.Clamp01(operation.progress / 0.9f);

            // Обновляем UI
            if (progressBar != null)
            {
                progressBar.value = progress;
            }
            if (progressText != null)
            {
                progressText.text = $"Загрузка... {(progress * 100):F0}%";
            }

            // Когда прогресс почти завершен (т.е. сцена готова к активации)
            if (operation.progress >= 0.9f)
            {
                // Можно добавить задержку здесь, чтобы показать "нажмите любую кнопку для продолжения"
                // yield return new WaitForSeconds(1f); // Например, подождать 1 секунду

                // Если нужно активировать по нажатию кнопки:
                // if (Input.anyKeyDown)
                // {
                //    operation.allowSceneActivation = true;
                // }

                // В простом случае, активируем сцену сразу
                operation.allowSceneActivation = true;
            }

            yield return null; // Ждем следующего кадра
        }
    }
}