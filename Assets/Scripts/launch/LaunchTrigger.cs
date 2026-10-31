using UnityEngine;

public class LaunchTrigger : MonoBehaviour
{
    // Ссылка на наш главный скрипт, управляющий запуском
    public RocketLaunchSequence launchManager;

    // Флаг, чтобы триггер сработал только один раз
    private bool hasBeenTriggered = false;

    // Этот метод автоматически вызывается Unity, когда какой-либо коллайдер входит в триггер
    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, не сработал ли триггер ранее, и вошел ли в него объект с тегом "Player"
        if (!hasBeenTriggered && other.CompareTag("Player"))
        {
            Debug.Log("Игрок вошел в триггер. Запускаем последовательность!");

            // Устанавливаем флаг, чтобы избежать повторного запуска
            hasBeenTriggered = true;

            // Вызываем публичный метод из нашего другого скрипта
            launchManager.StartLaunchSequence();
        }
    }

    // (Необязательно) Визуализация триггера в редакторе для удобства
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.25f); // Зеленый, полупрозрачный
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}