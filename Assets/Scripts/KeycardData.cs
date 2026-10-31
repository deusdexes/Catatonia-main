using UnityEngine;

[CreateAssetMenu(fileName = "NewKeycardData", menuName = "Inventory/Keycard Data")]
public class KeycardData : ScriptableObject
{
    public string keycardName = "Keycard";
    public int keycardID; // ”никальный ID дл€ каждой ключ-карты (например, 1 дл€ синей, 2 дл€ красной)
    public Color displayColor = Color.white; // ÷вет дл€ индикации (UI, эффекты)
    public Sprite keycardIcon; // »конка дл€ инвентар€
    public string description = "A keycard used to open specific doors.";
}