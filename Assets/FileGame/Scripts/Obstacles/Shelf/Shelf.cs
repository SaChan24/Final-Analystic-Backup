using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Shelf : MonoBehaviour
{
    public enum Axis { X, Y, Z }
    public enum MotionType { Rotation, Slide }

    [System.Serializable]
    public class DoorConfig
    {
        [Header("Target")]
        public Transform door;

        [Header("Motion")]
        public MotionType motion = MotionType.Rotation;

        [Tooltip("แกนสำหรับ Rotation หรือ Slide")]
        public Axis axis = Axis.Y;

        [Tooltip("สลับทิศทาง (เช่น ซ้าย/ขวา)")]
        public bool invert = false;

        [Header("Rotation Settings")]
        [Tooltip("องศาที่เปิดจากตำแหน่งปิด (สัมพัทธ์)")]
        public float openAngle = 90f;

        [Header("Slide Settings")]
        [Tooltip("ระยะเลื่อนจากตำแหน่งปิด (หน่วยเมตร)")]
        public float slideDistance = 0.3f;

        [HideInInspector] public Quaternion closedRot;
        [HideInInspector] public Quaternion openRot;
        [HideInInspector] public Vector3 closedPos;
        [HideInInspector] public Vector3 openPos;
    }

    [Header("Doors")]
    public List<DoorConfig> doors = new List<DoorConfig>();

    [Header("Interact")]
    [Tooltip("อนุญาตให้กดสลับ เปิด/ปิด ได้")]
    public bool allowToggle = true;

    [Header("Lock")]
    [Tooltip("เริ่มต้นเป็นตู้ล็อกอยู่หรือไม่")]
    public bool isLocked = false;

    [Tooltip("ไอเท็ม ID ของกุญแจที่ต้องใช้ (ใช้ string ให้เข้ากับ InventoryLite)")]
    public string requiredKeyId = "Key01";

    [Tooltip("จำนวนที่ต้องใช้เพื่อปลดล็อค")]
    [Min(1)] public int requiredKeyAmount = 1;

    [Tooltip("ใช้กุญแจแล้วให้หายไปหรือไม่")]
    public bool consumeKeyOnUse = false;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip openSfx;
    public AudioClip closeSfx;
    public AudioClip lockedSfx;
    public AudioClip openWoodCrackSfx;

    [Header("Destroy When Open")]
    public GameObject Wood;

    [Tooltip("เสียงล็อกตัวที่ 2 (จะเล่นตามหลังเสียงแรก)")]
    public AudioClip lockedSfx2;

    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Tooltip("ดีเลย์ระหว่าง lockedSfx -> lockedSfx2 (วินาที)")]
    [Min(0f)] public float lockedSfx2Delay = 0.15f;

    [Header("UI Feedback (Lock Message)")]
    public GameObject messageRoot;

    [Tooltip("ใส่ TMP_Text (แนะนำ)")]
    public TMPro.TMP_Text messageTextTMP;

    [Tooltip("เวลาที่แสดงข้อความก่อนหาย (วินาที)")]
    [Range(0.2f, 5f)] public float messageDuration = 1.5f;

    Coroutine _msgCo;

    [Header("One-time Locked Dialog (Optional)")]
    [Tooltip("ถ้าใส่ไว้: ครั้งแรกที่กดตอนล็อก + ไม่มีกุญแจพอ จะเล่น Dialog นี้ครั้งเดียว แล้วครั้งถัดไปค่อย ShowMsg Require Item")]
    public OneTimeDialogPlayerUI firstLockedDialog;

    [Header("Events")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;
    public UnityEvent onLockedTry;

    [Header("Debug / Test")]
    public bool debugLogs = false;

    [Tooltip("ทดสอบ: ให้เปิดเองทันทีเมื่อกด Play (ไว้เช็คว่าบานขยับจริง)")]
    public bool autoOpenOnPlay = false;

    [Header("Interact Guard")]
    [Tooltip("กันสแปม: เวลาขั้นต่ำระหว่างการกด (วินาที)")]
    [Min(0f)] public float interactCooldown = 0.35f;

    [Tooltip("บล็อกการกดระหว่างอนิเมชันกำลังเล่นอยู่")]
    public bool blockWhileAnimating = true;

    [Header("Animation")]
    [Tooltip("ความเร็วเปิด/ปิด (ตัวคูณเวลา)")]
    public float openSpeed = 3f;

    // runtime
    bool _isOpen = false;
    bool _isBusy = false;
    float _lastInteractTime = -999f;

    InventoryLite _playerInv;
    Coroutine _anim;

    void Awake()
    {
        EnsureAudioSource();

        if (messageRoot) messageRoot.SetActive(false);
        if (doors == null) doors = new List<DoorConfig>();

        NormalizeConfigs();
        CacheAllDoorStates();

        if (autoOpenOnPlay) OpenNow();
    }

    void EnsureAudioSource()
    {
        if (audioSource) return;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void NormalizeConfigs()
    {
        foreach (var d in doors)
        {
            if (!d.door) continue;
            if (d.openAngle < 0) d.openAngle = -d.openAngle;
            if (d.slideDistance < 0) d.slideDistance = -d.slideDistance;
        }
    }

    void CacheAllDoorStates()
    {
        foreach (var d in doors)
        {
            if (!d.door) continue;

            d.closedRot = d.door.localRotation;
            d.closedPos = d.door.localPosition;

            switch (d.motion)
            {
                case MotionType.Rotation:
                    {
                        Vector3 axis = AxisToVector(d.axis);
                        float sign = d.invert ? -1f : 1f;
                        d.openRot = d.closedRot * Quaternion.AngleAxis(sign * d.openAngle, axis);
                        d.openPos = d.closedPos;
                        break;
                    }
                case MotionType.Slide:
                    {
                        Vector3 dir = AxisToVector(d.axis) * (d.invert ? -1f : 1f);
                        d.openPos = d.closedPos + dir * d.slideDistance;
                        d.openRot = d.closedRot;
                        break;
                    }
            }
        }
    }

    static Vector3 AxisToVector(Axis a)
    {
        switch (a)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            default: return Vector3.forward;
        }
    }

    // ===== Interact entry =====
    public void TryInteract(GameObject playerGO)
    {
        if (debugLogs) Debug.Log("[Shelf] TryInteract");

        if (IsBlockedByCooldown()) return;
        if (IsBlockedByAnimation()) return;

        _lastInteractTime = Time.time;

        if (!_isOpen)
        {
            if (isLocked && !TryUnlock(playerGO))
                return; // ยังล็อกอยู่/ยังไม่ผ่าน

            OpenNow();
        }
        else if (allowToggle)
        {
            CloseNow();
        }
    }

    bool IsBlockedByCooldown()
    {
        if (Time.time - _lastInteractTime >= interactCooldown) return false;
        if (debugLogs) Debug.Log("[Shelf] Interact ignored: cooldown");
        return true;
    }

    bool IsBlockedByAnimation()
    {
        if (!blockWhileAnimating) return false;
        if (!_isBusy && _anim == null) return false;
        if (debugLogs) Debug.Log("[Shelf] Interact ignored: animating");
        return true;
    }

    // return true = unlock success or not locked
    // return false = locked and blocked (show message/dialog)
    bool TryUnlock(GameObject playerGO)
    {
        if (!_playerInv && playerGO)
            _playerInv = playerGO.GetComponentInParent<InventoryLite>();

        int need = Mathf.Max(1, requiredKeyAmount);
        int have = (_playerInv != null) ? _playerInv.GetCount(requiredKeyId) : 0;

        // no key / not enough
        if (_playerInv == null || have < need)
        {
            PlayLockedFeedback();

            // ✅ one-time dialog (ถ้ามี)
            if (firstLockedDialog != null && !firstLockedDialog.HasPlayed)
            {
                firstLockedDialog.PlayOnce();
                return false;
            }

            // ✅ default message (เดิมของเธอ)
            ShowMsg($"Require Item: {requiredKeyId}");
            if (debugLogs) Debug.Log($"[Shelf] Locked: need {requiredKeyId} x{need}, have {have}");
            return false;
        }

        // enough -> consume (optional) then unlock
        if (consumeKeyOnUse)
        {
            bool ok = _playerInv.Consume(requiredKeyId, need);
            if (debugLogs) Debug.Log($"[Shelf] Consume key: {ok} for {requiredKeyId} x{need}");
        }

        isLocked = false;
        return true;
    }

    void PlayLockedFeedback()
    {
        PlayOneShot(lockedSfx);
        if (lockedSfx2) StartCoroutine(PlayLockedSecond());
        onLockedTry?.Invoke();
    }

    void PlayOneShot(AudioClip clip)
    {
        if (!clip) return;
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    void ShowMsg(string msg)
    {
        if (!messageRoot || !messageTextTMP)
        {
            if (debugLogs) Debug.Log(msg);
            return;
        }

        if (_msgCo != null) StopCoroutine(_msgCo);
        _msgCo = StartCoroutine(ShowMessageRoutine(msg, messageDuration));
    }

    IEnumerator ShowMessageRoutine(string msg, float duration)
    {
        messageRoot.SetActive(true);
        messageTextTMP.text = msg;

        yield return new WaitForSeconds(duration);

        messageRoot.SetActive(false);
        _msgCo = null;
    }

    // ===== Open/Close =====
    [ContextMenu("TEST / Open Now")]
    public void OpenNow()
    {
        if (blockWhileAnimating && _anim != null) return;
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(AnimateDoors(true));
    }

    [ContextMenu("TEST / Close Now")]
    public void CloseNow()
    {
        if (blockWhileAnimating && _anim != null) return;
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(AnimateDoors(false));
    }

    IEnumerator AnimateDoors(bool toOpen)
    {
        _isBusy = true;
        if (debugLogs) Debug.Log("[Shelf] AnimateDoors " + (toOpen ? "Open" : "Close"));

        PlayOneShot(toOpen ? openSfx : closeSfx);
        PlayOneShot(openWoodCrackSfx);
        DestroyWood();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            float k = Mathf.SmoothStep(0, 1, t);
            ApplyProgressAll(toOpen ? k : 1f - k);
            yield return null;
        }
        ApplyProgressAll(toOpen ? 1f : 0f);

        _isOpen = toOpen;
        if (toOpen) onOpened?.Invoke();
        else onClosed?.Invoke();

        _isBusy = false;
        _anim = null;
    }

    IEnumerator PlayLockedSecond()
    {
        if (lockedSfx2Delay > 0f)
            yield return new WaitForSeconds(lockedSfx2Delay);

        PlayOneShot(lockedSfx2);
    }

    void ApplyProgressAll(float k01)
    {
        foreach (var d in doors)
            ApplyProgressOne(d, k01);
    }

    static void ApplyProgressOne(DoorConfig d, float k01)
    {
        if (!d.door) return;

        switch (d.motion)
        {
            case MotionType.Rotation:
                d.door.localRotation = Quaternion.Slerp(d.closedRot, d.openRot, k01);
                break;

            case MotionType.Slide:
                d.door.localPosition = Vector3.Lerp(d.closedPos, d.openPos, k01);
                break;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (doors == null) return;
        foreach (var d in doors)
        {
            if (!d.door) continue;
            DrawDoorAxisGizmo(d);
        }
    }

    void DrawDoorAxisGizmo(DoorConfig d)
    {
        Vector3 axisVec;
        switch (d.axis)
        {
            case Axis.X: axisVec = d.door.right; break;
            case Axis.Y: axisVec = d.door.up; break;
            default: axisVec = d.door.forward; break;
        }
        if (d.invert) axisVec = -axisVec;

        Gizmos.color = (d.motion == MotionType.Rotation)
            ? new Color(1f, 0.85f, 0.2f, 0.9f)
            : new Color(0.2f, 0.85f, 1f, 0.9f);

        var p = d.door.position;
        Gizmos.DrawLine(p, p + axisVec * 0.25f);
        Gizmos.DrawSphere(p + axisVec * 0.25f, 0.01f);
    }

    void DestroyWood()
    {
        if (Wood) Destroy(Wood);
    }
}
