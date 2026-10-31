using UnityEngine;

public class TeleportOnFall : MonoBehaviour
{
    public Transform teleportDestination;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Пытаемся получить компонент CharacterController
            CharacterController cc = other.GetComponent<CharacterController>();

            // Если CharacterController есть, используем специальный метод
            if (cc != null)
            {
                cc.enabled = false; // Отключаем компонент
                other.transform.position = teleportDestination.position; // Телепортируем
                cc.enabled = true;  // Включаем обратно
            }
            else // Если CharacterController нет, телепортируем как обычно
            {
                other.transform.position = teleportDestination.position;
            }

            // Сброс скорости для Rigidbody (если он есть)
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log("Игрок телепортирован!");
        }
    }
}