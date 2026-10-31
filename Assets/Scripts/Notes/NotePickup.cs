// NotePickup.cs
using UnityEngine;

public class NotePickup : MonoBehaviour
{
    [Header("Note Data")]
    [Tooltip("Перетащите сюда ScriptableObject 'NoteData' для этой записки.")]
    public NoteData noteData; // Ссылка на ScriptableObject с данными этой конкретной записки

    // Присваиваем noteID из NoteData к имени GameObject для удобства в редакторе
    // Этот метод вызывается в редакторе при создании или изменении
    void OnValidate()
    {
        if (noteData != null && !string.IsNullOrEmpty(noteData.noteID))
        {
            // Установка имени GameObject, если оно не совпадает с noteID или изначально пустое
            if (gameObject.name == "GameObject" || gameObject.name != noteData.noteID)
            {
                gameObject.name = "Note_Pickup_" + noteData.noteID;
            }
        }
    }
}