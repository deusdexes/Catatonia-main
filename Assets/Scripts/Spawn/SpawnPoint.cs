using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Уникальный идентификатор этой точки спавна. Например, 'FromLevel1_MainDoor' или 'CaveExit'.")]
    public string spawnPointId;

    // Опционально: Нарисуем Гизмо, чтобы видеть точку спавна в редакторе
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawSphere(Vector3.zero, 0.5f); // Шарик в точке спавна
        Gizmos.DrawFrustum(Vector3.zero, 60f, 0.8f, 0.1f, 1.5f); // Конус, показывающий направление
    }
}