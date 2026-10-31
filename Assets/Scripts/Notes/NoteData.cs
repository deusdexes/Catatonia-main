// NoteData.cs
using UnityEngine;
using TMPro; // Добавляем, чтобы дать подсказку о TMP, хотя напрямую не используется в ScriptableObject

// Атрибут для создания ассетов NoteData через меню Unity Editor
[CreateAssetMenu(fileName = "NewNoteData", menuName = "Game Data/Note Data")]
public class NoteData : ScriptableObject
{
    [Header("Note Identification")]
    [Tooltip("Уникальный идентификатор этой записки. Используется для сохранения/загрузки. Должен быть уникальным для каждой записки.")]
    public string noteID;

    [Header("Note Content")]
    [Tooltip("Заголовок записки, отображаемый в журнале.")]
    [TextArea(1, 3)]
    public string noteTitle;

    [Tooltip("Полное содержимое записки, отображаемое при чтении. Поддерживает разметку TextMeshPro.")] // <-- Добавлена эта часть Tooltip
    [TextArea(10, 20)]
    public string noteContent;

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        NoteData other = (NoteData)obj;
        return noteID == other.noteID;
    }

    public override int GetHashCode()
    {
        return noteID.GetHashCode();
    }
}