using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ItemPickup3D : MonoBehaviour
{
    [Header("Item")]
    public string itemId = "Key";
    [Min(1)] public int amount = 1;

    [Header("FX & Behavior")]
    public bool destroyOnPickup = true;
    public GameObject pickupVfxPrefab;
    public AudioClip pickupSfx;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;

    [Header("Pickup Dialog (Optional)")]
    [Tooltip("เปิด/ปิดการแสดง Dialog ตอนเก็บไอเทม")]
    public bool showPickupDialog = false;

    [Tooltip("ถ้าใส่ไว้: ตอนเก็บไอเทม จะ PlayOnce() (ตัวนี้จะกันซ้ำเอง)")]
    public OneTimeDialogPlayerUI pickupDialog;

    [Tooltip("ถ้าเปิดไว้: ไอเทมจะ 'หายทันที' แต่จะรอให้ Dialog เล่นจบก่อนค่อย Destroy จริง")]
    public bool delayDestroyUntilDialogFinished = true;

    [Header("Events")]
    public UnityEvent onPicked;

    bool _picked = false;

    // เรียกจาก PlayerAimPickup เมื่อต้องการเก็บ (เล็งโดน + อยู่ในระยะ + กดปุ่มที่ฝั่งผู้เล่น)
    public void TryPickup(GameObject playerGO)
    {
        if (_picked) return; // กันกดซ้ำ
        if (!playerGO) return;

        var inv = playerGO.GetComponentInParent<InventoryLite>();
        if (!inv)
        {
            Debug.LogWarning($"[ItemPickup3D] No InventoryLite on {playerGO.name} or its parents.");
            return;
        }
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("[ItemPickup3D] itemId is empty.");
            return;
        }

        _picked = true;

        // ===== Logic เดิม: เพิ่มไอเทม =====
        inv.AddItem(itemId, Mathf.Max(1, amount));

        // ===== Logic เดิม: FX / SFX / Event =====
        if (pickupVfxPrefab) Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);
        if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position, sfxVolume);
        onPicked?.Invoke();

        // ===== เพิ่ม: Dialog ตอนเก็บ (เลือกได้) =====
        float dialogDuration = 0f;
        if (showPickupDialog && pickupDialog != null)
        {
            // สำคัญ: ตัว Dialog ต้อง "ไม่อยู่ใต้ไอเทม" ถ้าไอเทมโดน Destroy
            pickupDialog.PlayOnce();

            // ถ้าเราจะรอให้จบ ต้องรู้ระยะเวลาเล่นจริง
            // แนะนำให้เพิ่ม getter ใน OneTimeDialogPlayerUI (ดูหมายเหตุท้าย) 
            // แต่ถ้าเธอยังไม่อยากแก้ OneTimeDialogPlayerUI -> ใช้วิธี fallback: 0
            dialogDuration = GetDialogTotalSecondsFallback();
        }

        // ===== Fix: ทำให้ไอเทมหายทันที แต่ยังไม่ Destroy ทันที =====
        HideItemInstantly();

        // ===== Destroy/Disable ตามเดิม แต่ "หน่วง" ได้ =====
        if (destroyOnPickup)
        {
            if (delayDestroyUntilDialogFinished && dialogDuration > 0f)
                StartCoroutine(DestroyAfterSeconds(dialogDuration));
            else
                Destroy(gameObject);
        }
        else
        {
            // แบบเดิมคือ SetActive(false) อยู่แล้ว
            gameObject.SetActive(false);
        }
    }

    void HideItemInstantly()
    {
        // ปิด collider กันกดซ้ำ/ชนซ้ำ
        var c = GetComponent<Collider>();
        if (c) c.enabled = false;

        // ปิด renderer ทุกตัวให้ "หายจากโลกทันที"
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;

        // ถ้ามีไฟ/particle ใต้ไอเทมก็ปิดได้ (กันหลงเหลือ)
        var lights = GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
            lights[i].enabled = false;
    }

    IEnumerator DestroyAfterSeconds(float seconds)
    {
        // ใช้ unscaled time เผื่อเกมหยุดเวลา
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end)
            yield return null;

        Destroy(gameObject);
    }

    // ---------- Fallback ระยะเวลารอ dialog ----------
    // วิธีที่ดีที่สุด: ให้ OneTimeDialogPlayerUI มี property TotalDuration
    // ตอนนี้ถ้ายังไม่แก้ OneTimeDialogPlayerUI จะคืน 0 = ไม่หน่วง
    float GetDialogTotalSecondsFallback()
    {
        // ถ้าเธอยอมแก้ OneTimeDialogPlayerUI ฉันจะให้ใช้ pickupDialog.TotalDuration ได้เลย
        // ตอนนี้ขอคืน 0 ก่อน (เพื่อไม่ทำให้โปรเจกต์พัง)
        return 0f;
    }

    void Reset()
    {
        // ใช้ Raycast จากฝั่งผู้เล่น ไม่จำเป็นต้องเป็น Trigger
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (amount < 1) amount = 1;
    }
#endif
}
