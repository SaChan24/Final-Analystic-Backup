using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DiaryInteractable : MonoBehaviour
{
    [Header("UI")]
    public DiaryUI diaryUI;

    [TextArea]
    public string diaryText;

    [Header("Item Pickup (optional)")]
    [Tooltip("ถ้าติ๊กไว้ และมี ItemPickup3D อยู่บน object เดียวกัน จะให้ของเข้ากระเป๋าด้วย")]
    public bool giveItemOnInteract = true;

    [Tooltip("ให้ Diary เป็นคนซ่อน object หลังอ่าน (ไม่ใช้ destroyOnPickup ของ ItemPickup3D)")]
    public bool hideAfterRead = false;

    ItemPickup3D _itemPickup;

    void Awake()
    {
        // ถ้ามี ItemPickup3D อยู่บน object เดียวกัน จะ cache ไว้ใช้
        _itemPickup = GetComponent<ItemPickup3D>();
    }

    public void TryInteract(GameObject player)
    {
        if (!diaryUI) return;

        // 1) เปิดหน้า UI Diary ก่อน
        diaryUI.ShowDiary(diaryText);

        // 2) ถ้าอยากให้ Add item เข้า Inventory ด้วย → ใช้ ItemPickup3D ที่มีอยู่แล้ว
        if (giveItemOnInteract && player != null && _itemPickup != null)
        {
            // ไม่ให้มันทำลายตัวเองทันที เพราะเราคุมการซ่อนผ่าน hideAfterRead
            bool originalDestroy = _itemPickup.destroyOnPickup;
            _itemPickup.destroyOnPickup = false;

            _itemPickup.TryPickup(player);

            _itemPickup.destroyOnPickup = originalDestroy;
        }

        // 3) ถ้าอยากให้ไดอารี่หายไปจากฉากหลังอ่าน
        if (hideAfterRead)
        {
            gameObject.SetActive(false);
        }
    }
}
