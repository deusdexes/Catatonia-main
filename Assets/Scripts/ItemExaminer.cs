using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class ItemExaminer : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public Inventory inventory;
    public Camera playerCamera; // The main player camera
    public Transform examinePosition; // An empty GameObject to place the item during examination
    public CanvasGroup overlayPanel; // UI CanvasGroup for the darkening overlay

    [Header("Examine Settings")]
    public float rotationSpeed = 100f;
    public float moveSpeed = 5f; // Speed for moving item to examine position
    public float fadeInOutSpeed = 2f; // Speed for overlay fade
    public float examinedItemScale = 1.0f; // Scale item during examination (1.0 means no change)

    private bool isExamining = false;
    private GameObject currentExaminedGameObject;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;
    private float originalFOV;

    // Input actions state
    private bool isMouseLeftButtonHeld = false;
    private Vector2 lookInput;

    void Start()
    {
        // Basic error checking
        if (playerController == null) Debug.LogError("ItemExaminer: PlayerController not assigned!");
        if (inventory == null) Debug.LogError("ItemExaminer: Inventory not assigned!");
        if (playerCamera == null) Debug.LogError("ItemExaminer: PlayerCamera not assigned!");
        if (examinePosition == null) Debug.LogError("ItemExaminer: ExaminePosition not assigned! Please create an empty GameObject as a child of the camera.");
        if (overlayPanel == null) Debug.LogWarning("ItemExaminer: OverlayPanel not assigned. Screen will not darken.");

        // Initialize overlay to fully transparent and non-interactive
        if (overlayPanel != null)
        {
            overlayPanel.alpha = 0f;
            overlayPanel.interactable = false;
            overlayPanel.blocksRaycasts = false;
        }
    }

    void Update()
    {
        // Only rotate the item if examining, an item is present, and LMB is held
        if (isExamining && currentExaminedGameObject != null && isMouseLeftButtonHeld)
        {
            RotateItem();
        }
    }

    // Input System Callback for 'I' key (Examine/Unexamine)
    public void OnExamine(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (isExamining)
            {
                StopExamine();
            }
            else
            {
                // Only start examining if there's an item currently held in hand
                Item itemInHand = inventory.GetCurrentItemInHand();
                if (itemInHand != null)
                {
                    StartExamine(itemInHand);
                }
            }
        }
    }

    // Input System Callback for mouse movement (used for item rotation)
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    // Input System Callback for mouse left button (used to enable rotation)
    public void OnFire(InputAction.CallbackContext context) // Assuming 'Fire' is bound to LMB
    {
        if (context.started)
        {
            isMouseLeftButtonHeld = true;
        }
        else if (context.canceled)
        {
            isMouseLeftButtonHeld = false;
        }
    }

    // Public method to check if a specific item is being examined
    public bool IsExaminingItem(Item item)
    {
        return isExamining && currentExaminedGameObject == item.gameObject;
    }

    private void StartExamine(Item itemToExamine)
    {
        if (itemToExamine == null)
        {
            Debug.LogWarning("Cannot examine a null item.");
            return;
        }

        isExamining = true;
        currentExaminedGameObject = itemToExamine.gameObject;

        // Store original transform data to restore later
        originalParent = currentExaminedGameObject.transform.parent;
        originalLocalPosition = currentExaminedGameObject.transform.localPosition;
        originalLocalRotation = currentExaminedGameObject.transform.localRotation;
        originalLocalScale = currentExaminedGameObject.transform.localScale;

        // Disable player movement and camera look
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
            playerController.SetLookEnabled(false);
        }

        // Inform the inventory to detach the item from the hand without deactivating it
        inventory.DetachItemFromHandForExamine(itemToExamine);

        // Move item to the examine position smoothly
        StartCoroutine(MoveItemToExaminePosition(currentExaminedGameObject.transform));

        // Darken the screen using the overlay
        StartCoroutine(FadeOverlay(0.8f)); // Fade to 80% opacity (adjust as needed)

        // Adjust camera FOV for a closer look (optional)
        originalFOV = playerCamera.fieldOfView;
        playerCamera.fieldOfView = 60f; // Example FOV, adjust for desired zoom

        // Unlock cursor for item rotation
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log($"Started examining {itemToExamine.itemName}");
    }

    private void StopExamine()
    {
        if (!isExamining) return;

        isExamining = false;

        // Re-enable player movement and camera look
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
            playerController.SetLookEnabled(true);
        }

        // Restore camera FOV
        playerCamera.fieldOfView = originalFOV;

        // Hide the darkening overlay
        StartCoroutine(FadeOverlay(0f));

        // Restore item to its original position (back to hand if it was selected)
        if (currentExaminedGameObject != null)
        {
            StartCoroutine(MoveItemBackToHand(currentExaminedGameObject.transform));
        }

        currentExaminedGameObject = null;

        // Lock cursor back
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Stopped examining.");
    }

    // Coroutine to smoothly move the item to the examine position
    private IEnumerator MoveItemToExaminePosition(Transform itemTransform)
    {
        itemTransform.gameObject.SetActive(true); // Ensure item is active
        itemTransform.SetParent(examinePosition); // Parent to the examine position

        Vector3 startPos = itemTransform.localPosition;
        Quaternion startRot = itemTransform.localRotation;
        Vector3 startScale = itemTransform.localScale;

        Vector3 targetPos = Vector3.zero; // Local position of examinePosition is its origin
        Quaternion targetRot = Quaternion.identity; // Reset rotation for examination
        Vector3 targetScale = Vector3.one * examinedItemScale; // Apply examination scale

        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * moveSpeed;
            itemTransform.localPosition = Vector3.Lerp(startPos, targetPos, timer);
            itemTransform.localRotation = Quaternion.Slerp(startRot, targetRot, timer);
            itemTransform.localScale = Vector3.Lerp(startScale, targetScale, timer);
            yield return null;
        }

        // Ensure final position, rotation, and scale are exact
        itemTransform.localPosition = targetPos;
        itemTransform.localRotation = targetRot;
        itemTransform.localScale = targetScale;
    }

    // Coroutine to smoothly move the item back to its original parent/position
    private IEnumerator MoveItemBackToHand(Transform itemTransform)
    {
        Vector3 startPos = itemTransform.localPosition;
        Quaternion startRot = itemTransform.localRotation;
        Vector3 startScale = itemTransform.localScale;

        // First, smoothly move it back to the original local position/rotation/scale relative to examiner's parent (examinePosition)
        Vector3 targetPos = originalLocalPosition;
        Quaternion targetRot = originalLocalRotation;
        Vector3 targetScale = originalLocalScale;

        // Temporarily set its parent to its original parent for smooth movement relative to it
        // This is a bit tricky, better to just move it directly and then parent
        // For simplicity and matching current inventory logic, we will smoothly move and then re-parent.

        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * moveSpeed;
            itemTransform.localPosition = Vector3.Lerp(startPos, targetPos, timer);
            itemTransform.localRotation = Quaternion.Slerp(startRot, targetRot, timer);
            itemTransform.localScale = Vector3.Lerp(startScale, targetScale, timer);
            yield return null;
        }

        // Then re-parent it to its original parent and set its original transform values
        itemTransform.SetParent(originalParent);
        itemTransform.localPosition = originalLocalPosition;
        itemTransform.localRotation = originalLocalRotation;
        itemTransform.localScale = originalLocalScale;

        // Notify inventory to re-attach and reactivate the item's use
        Item item = itemTransform.GetComponent<Item>();
        if (item != null)
        {
            inventory.ReAttachItemToHandAfterExamine(item);
        }
        else
        {
            // Fallback: If it's not an Item, just ensure it's active
            itemTransform.gameObject.SetActive(true);
        }
    }

    // Handles item rotation based on mouse input
    private void RotateItem()
    {
        // Mouse Y movement rotates around item's local X-axis (pitch)
        float rotX = lookInput.y * rotationSpeed * Time.deltaTime;
        // Mouse X movement rotates around item's world Y-axis (yaw), inverted for intuitive control
        float rotY = -lookInput.x * rotationSpeed * Time.deltaTime;

        // Apply rotation
        currentExaminedGameObject.transform.Rotate(Vector3.up, rotY, Space.World); // Rotate around global Y
        currentExaminedGameObject.transform.Rotate(Vector3.right, rotX, Space.Self); // Rotate around local X
    }

    // Coroutine to smoothly fade the overlay in or out
    private IEnumerator FadeOverlay(float targetAlpha)
    {
        if (overlayPanel == null) yield break;

        float startAlpha = overlayPanel.alpha;

        // Make panel interactive/block raycasts only when it's fading in or fully opaque
        overlayPanel.interactable = (targetAlpha > 0);
        overlayPanel.blocksRaycasts = (targetAlpha > 0);

        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * fadeInOutSpeed;
            overlayPanel.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer);
            yield return null;
        }
        overlayPanel.alpha = targetAlpha; // Ensure final alpha is exact
    }
}