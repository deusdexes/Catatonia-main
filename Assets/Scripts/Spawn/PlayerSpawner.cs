using UnityEngine;
using System.Linq; // Нужен для использования .FirstOrDefault()

public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("ID точки спавна по умолчанию, если нужная не будет найдена.")]
    public string defaultSpawnPointId = "Default";

    void Start()
    {
        // Находим объект игрока по тегу. Убедитесь, что у вашего игрока есть тег "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("PlayerSpawner: Игрок с тегом 'Player' не найден!");
            return;
        }

        // Проверяем, есть ли у нас SpawnManager и ID для поиска
        if (SpawnManager.Instance == null)
        {
            Debug.LogWarning("SpawnManager не найден. Используется позиция игрока по умолчанию.");
            return;
        }

        string targetSpawnId = SpawnManager.Instance.nextSpawnPointId;

        // Если ID не был задан (например, при первом запуске сцены), используем ID по умолчанию
        if (string.IsNullOrEmpty(targetSpawnId))
        {
            targetSpawnId = defaultSpawnPointId;
        }

        // Ищем все точки спавна в сцене
        var spawnPoints = FindObjectsOfType<SpawnPoint>();
        // Находим нужную точку по ее ID
        SpawnPoint targetPoint = spawnPoints.FirstOrDefault(p => p.spawnPointId == targetSpawnId);

        if (targetPoint != null)
        {
            // Перемещаем игрока в позицию и поворот точки спавна
            player.transform.position = targetPoint.transform.position;
            player.transform.rotation = targetPoint.transform.rotation;
            Debug.Log($"Игрок перемещен в точку спавна: {targetSpawnId}");
        }
        else
        {
            Debug.LogWarning($"Точка спавна с ID '{targetSpawnId}' не найдена! Игрок остался на месте.");
        }

        // Очищаем ID, чтобы он не использовался повторно при следующей загрузке сцены
        SpawnManager.Instance.nextSpawnPointId = null;
    }
}