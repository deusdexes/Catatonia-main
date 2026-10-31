using UnityEngine;
using UnityEngine.AI; // Нам нужен доступ к NavMesh

[RequireComponent(typeof(SlidingDoor))] // Гарантирует, что на объекте есть и основной скрипт
public class SlidingDoorMonster : MonoBehaviour
{
    // Ссылка на оригинальный скрипт двери, за которым мы будем следить
    private SlidingDoor originalDoor;

    // Ссылки на компоненты, которыми мы будем управлять
    private NavMeshObstacle leftDoorObstacle;
    private NavMeshObstacle rightDoorObstacle;

    // Храним последнее известное состояние, чтобы реагировать только на изменения
    private bool lastKnownClosedState;

    void Start()
    {
        // 1. Получаем доступ к оригинальному скрипту на этом же объекте
        originalDoor = GetComponent<SlidingDoor>();

        // 2. Проверяем, что у него есть ссылки на створки
        if (originalDoor.leftDoor == null || originalDoor.rightDoor == null)
        {
            Debug.LogError($"[SlidingDoorMonster] На оригинальном скрипте SlidingDoor ({name}) не назначены створки (leftDoor/rightDoor)!", this);
            this.enabled = false; // Отключаем этот компонент, чтобы избежать ошибок
            return;
        }

        // 3. Находим компоненты NavMeshObstacle на створках
        leftDoorObstacle = originalDoor.leftDoor.GetComponent<NavMeshObstacle>();
        rightDoorObstacle = originalDoor.rightDoor.GetComponent<NavMeshObstacle>();

        if (leftDoorObstacle == null || rightDoorObstacle == null)
        {
            Debug.LogError($"[SlidingDoorMonster] На створках двери ({name}) отсутствует компонент NavMeshObstacle!", this);
            this.enabled = false;
            return;
        }

        // 4. Устанавливаем начальное состояние
        lastKnownClosedState = originalDoor.isClosed;
        SetObstacleState(lastKnownClosedState);
    }

    void Update()
    {
        // 5. Постоянно проверяем, не изменилось ли состояние основной двери
        if (originalDoor.isClosed != lastKnownClosedState)
        {
            // Состояние изменилось! Обновляем наши препятствия.
            lastKnownClosedState = originalDoor.isClosed;
            SetObstacleState(lastKnownClosedState);
        }
    }

    // Метод, который включает или выключает препятствия
    private void SetObstacleState(bool isActive)
    {
        leftDoorObstacle.enabled = isActive;
        rightDoorObstacle.enabled = isActive;
    }
}
