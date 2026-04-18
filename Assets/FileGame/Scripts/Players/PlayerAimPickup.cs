using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerAimPickup : MonoBehaviour
{
    [Header("Input (Input System)")]
    public InputActionReference interactAction; // ปุ่ม Interact หลัก (เช่น E)

    [Header("References")]
    public Camera playerCamera;
    public InventoryLite inventory;

    [Header("UI Prompt")]
    public GameObject promptRoot;
#if TMP_PRESENT || UNITY_2021_1_OR_NEWER
    public TMPro.TMP_Text promptText;
#endif

    [Header("Pickup Toast UI")]
    [Tooltip("UI ที่เด้งขึ้นมาบอกว่าเก็บไอเทมอะไร (ถ้าไม่ใช้ปล่อยว่างได้)")]
    public ItemPickupToastUI pickupToast;

    [Header("Raycast Settings")]
    [Min(0.5f)] public float maxPickupDistance = 3f;
    public LayerMask hitMask = ~0;
    public bool includeTriggers = false;

    [Header("Fallback Input (ถ้าไม่มี Input System Action)")]
    public KeyCode interactLegacy = KeyCode.E;
#if ENABLE_INPUT_SYSTEM
    public Key fallbackInteract = Key.E;
    Keyboard kb => Keyboard.current;
#endif

    [Header("Debug")]
    public bool drawRay = false;

    void OnEnable()  => interactAction?.action.Enable();
    void OnDisable() => interactAction?.action.Disable();

    void Awake()
    {
        if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();
        if (!inventory) inventory = GetComponentInParent<InventoryLite>();

        if (promptRoot) promptRoot.SetActive(false);

        if (!pickupToast)
            pickupToast = FindObjectOfType<ItemPickupToastUI>();
    }

    void LateUpdate()
    {
        if (!playerCamera) return;

        // ยิง Ray ออกจากกล้องไปข้างหน้า
        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        var query = includeTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        RaycastHit hit;
        DiaryInteractable diaryTarget = null;
        ItemPickup3D itemTarget = null;
        RadioInteractable radioTarget = null;
        CircuitBreakerInteractable breakerTarget = null;
        ShelfInteractable shelfTarget = null;
        DoorExitInteractable doorTarget = null;

        if (Physics.Raycast(ray, out hit, maxPickupDistance, hitMask, query))
        {
            var tr = hit.collider.transform;

            // ✅ ให้ Diary เด่นสุด: ถ้าวัตถุมีทั้ง DiaryInteractable + ItemPickup3D
            // จะมองเป็น Diary ก่อน
            diaryTarget = tr.GetComponentInParent<DiaryInteractable>();

            if (!diaryTarget)
                itemTarget = tr.GetComponentInParent<ItemPickup3D>();

            if (!diaryTarget && !itemTarget)
            {
                radioTarget   = tr.GetComponentInParent<RadioInteractable>();
                breakerTarget = tr.GetComponentInParent<CircuitBreakerInteractable>();
                shelfTarget   = tr.GetComponentInParent<ShelfInteractable>();
                doorTarget    = tr.GetComponentInParent<DoorExitInteractable>();
            }
        }

        bool hasTarget = diaryTarget || itemTarget || radioTarget ||
                         breakerTarget || shelfTarget || doorTarget;

        if (promptRoot) promptRoot.SetActive(hasTarget);

#if TMP_PRESENT || UNITY_2021_1_OR_NEWER
        if (promptText)
        {
            if (diaryTarget)      promptText.text = "Press E to read diary";
            else if (itemTarget)  promptText.text = $"Press E to pick up {itemTarget.itemId}";
            else if (radioTarget) promptText.text = radioTarget.promptText;
            else if (breakerTarget) promptText.text = breakerTarget.promptText;
            else if (shelfTarget)   promptText.text = shelfTarget.promptText;
            else if (doorTarget)    promptText.text = doorTarget.promptText;
            else                    promptText.text = "";
        }
#endif

        if (drawRay)
        {
            Color c = hasTarget ? Color.green : Color.red;
            Debug.DrawRay(ray.origin, ray.direction * maxPickupDistance, c);
        }

        // ยังไม่มีอะไรให้กด หรือยังไม่ได้กดปุ่ม Interact → ไม่ทำอะไรต่อ
        if (!hasTarget || !PressedInteract())
            return;

        // 🔹 เรียงลำดับความสำคัญของ Interact
        if (diaryTarget)
        {
            diaryTarget.TryInteract(gameObject);
        }
        else if (itemTarget)
        {
            if (pickupToast)
                pickupToast.Show($"{itemTarget.itemId} +{itemTarget.amount}");

            itemTarget.TryPickup(gameObject);
        }
        else if (radioTarget)
        {
            radioTarget.TryInteract(gameObject);
        }
        else if (breakerTarget)
        {
            breakerTarget.TryInteract(gameObject);
        }
        else if (shelfTarget)
        {
            shelfTarget.TryInteract(gameObject);
        }
        else if (doorTarget)
        {
            doorTarget.TryInteract(gameObject);
        }
    }

    bool PressedInteract()
    {
        if (interactAction && interactAction.action.enabled)
            return interactAction.action.WasPressedThisFrame();

#if ENABLE_INPUT_SYSTEM
        return kb != null && kb[fallbackInteract].wasPressedThisFrame;
#else
        return Input.GetKeyDown(interactLegacy);
#endif
    }
}
