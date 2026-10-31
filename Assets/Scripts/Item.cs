using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public bool canBePickedUp = true;

    public abstract void Use();
    public abstract void StopUse();

    public virtual void OnEquip(Transform hand)
    {
        // Базовая логика экипировки предмета в руку
        transform.SetParent(hand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        //transform.localScale = Vector3.one;
    }

    public virtual void OnPickup()
    {
        // Базовая логика при подборе предмета
        // Производные классы должны реализовать свою логику
    }

    public virtual void OnDrop()
    {
        // Базовая логика при выбрасывании предмета
        transform.SetParent(null);
        // Производные классы должны реализовать свою логику
    }
}