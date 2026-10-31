// GameData.cs (убедитесь, что этот файл существует и имеет такой вид)
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    public string currentSceneName;
    public Vector3 playerPosition;
    public float playerHealth;
    public int playerLevel;
    public List<string> inventoryItems;
    public Dictionary<string, bool> completedQuests;
    public GameSettings gameSettings; // <-- Важно: ссылка на настройки!

    public GameData()
    {
        currentSceneName = "MainMenu"; // По умолчанию, если нет сохранения
        playerPosition = new Vector3(0, 1, 0);
        playerHealth = 100f;
        playerLevel = 1;
        inventoryItems = new List<string>(); // Пусть будет пустым по умолчанию
        completedQuests = new Dictionary<string, bool>();
        gameSettings = new GameSettings(); // Инициализация вложенного класса!
    }
}

// GameSettings.cs (убедитесь, что этот файл существует и имеет такой вид)

[System.Serializable]
public class GameSettings
{
    public float masterVolume;
    public float sfxVolume;
    public float musicVolume;
    public float mouseSensitivity;

    public GameSettings()
    {
        // Значения по умолчанию для новых настроек
        masterVolume = 1.0f;
        sfxVolume = 1.0f;
        musicVolume = 1.0f;
        mouseSensitivity = 0.5f;
    }
}