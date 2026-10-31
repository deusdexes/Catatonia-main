using UnityEngine;

public class PuzzleHintTrigger : MonoBehaviour
{
    [Tooltip("Уникальный ID, который соответствует ID в PuzzleHintManager.")]
    public int hintID = 1;

    [Tooltip("Слой(и), который(ые) представляет игрока.")]
    public LayerMask playerLayer;

    // Используем OnTriggerEnter для обнаружения игрока
    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, что вошедший объект находится на слое игрока
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            if (PuzzleHintManager.Instance != null)
            {
                PuzzleHintManager.Instance.ShowHint(hintID);

                // Опционально: Отключаем триггер после активации,
                // чтобы избежать повторного срабатывания, если игрок стоит внутри.
                // Хотя менеджер сам проверяет "единоразовость", это полезно.
                // GetComponent<Collider>().enabled = false; 
            }
            else
            {
                Debug.LogError("PuzzleHintTrigger: PuzzleHintManager не найден в сцене!");
            }
        }
    }

    // Рекомендуется иметь Collider с IsTrigger = true и Rigidbody на этом объекте или на игроке
    // для корректной работы OnTriggerEnter.
}