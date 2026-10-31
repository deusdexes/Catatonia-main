using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName = "Test Item";

    private Item item;
    private Inventory playerInventory;
    private bool isPickedUp = false;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;

        item = GetComponent<Item>();
        if (item == null)
        {
            Debug.LogError("No Item component found on " + gameObject.name);
        }

        playerInventory = FindFirstObjectByType<Inventory>();
        if (playerInventory == null)
        {
            Debug.LogError("No Inventory found in scene!");
        }
    }

    public void OnInteract()
    {
        if (isPickedUp) return;

        Debug.Log("Interact called on: " + itemName);
        TryPickup();
    }

    void TryPickup()
    {
        if (isPickedUp) return;

        Debug.Log("Attempting to pickup: " + itemName);

        if (playerInventory == null)
        {
            Debug.LogError("Player inventory is null!");
            return;
        }

        if (item == null)
        {
            Debug.LogError("Item is null!");
            return;
        }

        if (playerInventory.AddItem(item))
        {
            Debug.Log("Successfully picked up: " + itemName);
            isPickedUp = true;

            // ��������� ���������, ����� ������ ���� ��������� �����
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
        else
        {
            Debug.Log("Failed to pickup item: " + itemName);
        }
    }

    public void HandleDrop()
    {
        // ����������� �� ���� ������ (���� �� ��� �����������)
        transform.SetParent(null);

        // ��������������� �������� �������, �������� � �������
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;

        // �������� ������, ����� �� ���� ������� � �������� � �����
        gameObject.SetActive(true);

        // ���������� ������, ���� ���� Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // ��� true, � ����������� �� ����, ����� �� ��� ����������
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ���������� ��������� "��������", ����� ��� ����� ���� ��������� �����
        ResetPickup();

        Debug.Log($"Item '{itemName}' has been returned to its original position.");
    }

    // ��������� ����� ��� ������ ��������� �������
    public void ResetPickup()
    {
        isPickedUp = false;

        // �������� ��������� �������
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        Debug.Log("Reset pickup state for: " + itemName);
    }

    // ���������� ��� �������� ��� ��������� (��� �������)
    void OnMouseEnter()
    {
        if (!isPickedUp)
        {
            Debug.Log("Look at: " + itemName);
        }
    }
}