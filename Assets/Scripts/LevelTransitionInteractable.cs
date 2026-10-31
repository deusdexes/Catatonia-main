using UnityEngine;
using UnityEngine.SceneManagement;

// Требуем наличие коллайдера для обнаружения через Raycast
[RequireComponent(typeof(Collider))]
public class LevelTransitionInteractable : MonoBehaviour
{
    [Header("Настройки перехода")]
    [Tooltip("Имя сцены, которую необходимо загрузить при взаимодействии.")]
    public string sceneToLoad;

    [Tooltip("ID точки спавна в следующей сцене, куда будет перемещен игрок.")]
    public string targetSpawnPointId;

    /// <summary>
    /// Этот метод будет вызываться из PlayerInteraction, когда игрок нажимает кнопку взаимодействия.
    /// </summary>
    public void OnInteract()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("LevelTransitionInteractable: Имя сцены для загрузки не указано!", this);
            return;
        }

        // 1. Сохраняем текущее состояние игры перед сменой сцены
        if (SaveLoadManager.Instance != null)
        {
            // Убедимся, что SaveLoadManager сохранит текущее имя сцены
            // и текущую позицию игрока (если SaveLoadManager настроен на это).
            // Здесь мы также можем явно сохранить targetSpawnPointId, если хотим,
            // чтобы он был частью постоянного сохранения (но для обычного перехода это не обязательно,
            // т.к. SpawnManager сам его устанавливает).
            SaveLoadManager.Instance.SaveGame();
            Debug.Log("LevelTransitionInteractable: Игра сохранена перед переходом на новую сцену.");
        }
        else
        {
            Debug.LogWarning("LevelTransitionInteractable: SaveLoadManager не найден. Игра не будет сохранена перед переходом.");
        }


        // 2. Устанавливаем ID точки спавна для следующей сцены
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.nextSpawnPointId = targetSpawnPointId;
        }
        else
        {
            Debug.LogError("SpawnManager не найден в сцене! Невозможно сохранить точку спавна.");
            // Мы можем продолжить загрузку сцены, но игрок может появиться в неправильном месте.
            // В зависимости от логики вашей игры, здесь можно прервать переход.
            // Для примера я продолжу, чтобы сцена все равно загрузилась.
        }

        // 3. Загружаем новую сцену
        Debug.Log($"LevelTransitionInteractable: Загрузка сцены '{sceneToLoad}' с точкой спавна '{targetSpawnPointId}'...");
        SceneManager.LoadScene(sceneToLoad);
    }
}