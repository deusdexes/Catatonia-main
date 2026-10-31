using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    // Здесь мы будем хранить ID точки спавна для следующей сцены
    public string nextSpawnPointId;

    private void Awake()
    {
        // Классический паттерн "Синглтон"
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // <-- Это самая важная строка!
        }
        else
        {
            Destroy(gameObject);
        }
    }
}