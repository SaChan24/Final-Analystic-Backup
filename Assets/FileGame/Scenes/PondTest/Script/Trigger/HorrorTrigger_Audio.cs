using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HorrorTrigger_Audio : MonoBehaviour
{
    public string playerTag = "Player";
    public bool singleUse = true;

    [Header("Sound")]
    public Transform soundPoint;
    public AudioClip clip;
    [Range(0, 1)] public float volume = 1f;
    public float minDistance = 1f;
    public float maxDistance = 50f;
    public bool spatialize = true;

    [Tooltip("AudioSource ที่ให้วางไว้ใน Scene เพื่อเล่นเสียง 3D จริง (แนะนำให้ spatialBlend = 1)")]
    public AudioSource inputAudioSource;

    [Tooltip("ถ้าเปิด = ย้ายตำแหน่ง AudioSource ไปที่ soundPoint/trigger ก่อนเล่นทุกครั้ง")]
    public bool moveSourceToSoundPoint = true;

    [Tooltip("Rolloff ที่ใช้กับ AudioSource ตอนเล่น")]
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    [Header("Objects To Move (Array)")]
    [Tooltip("Object ที่จะถูกขยับเมื่อ Trigger ทำงาน")]
    public Transform[] moveTargets;

    [Tooltip("ตำแหน่งใหม่ (ถ้าเวคเตอร์ว่างๆ จะไม่เปลี่ยนตำแหน่งตัวนั้น)")]
    public Vector3[] targetPositions;

    [Tooltip("Rotation ใหม่เป็นองศา (Euler) (ถ้าเวคเตอร์ว่างๆ จะไม่เปลี่ยน rotation ตัวนั้น)")]
    public Vector3[] targetRotations;

    [Tooltip("ถ้าเปิด = ใช้ LocalPosition / LocalRotation, ถ้าปิด = ใช้ World Position / Rotation")]
    public bool useLocalSpace = false;

    [Header("Debug")]
    public bool logEvents = true;

    bool used;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (!TryGetComponent<Rigidbody>(out var rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (logEvents) Debug.Log($"[AudioTrigger] OnTriggerEnter by {other.name} tag={other.tag}", this);

        if (used && singleUse)
        {
            if (logEvents) Debug.Log("[AudioTrigger] already used", this);
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            if (logEvents) Debug.Log("[AudioTrigger] tag mismatch", this);
            return;
        }

        if (!clip)
        {
            Debug.LogWarning("[AudioTrigger] Missing clip", this);
            return;
        }

        Vector3 pos = soundPoint ? soundPoint.position : transform.position;
        PlayAt(pos);

        // เรียกระบบลดเสียง ambient เหมือนเดิม
        AmbientRoomAudioManager.FocusDuck();

        // ขยับ objects ตามที่ตั้งค่าไว้
        ApplyObjectTransforms();

        used = true;
    }

    [ContextMenu("Test Play Here (ignore trigger)")]
    void TestPlayHere()
    {
        Vector3 pos = soundPoint ? soundPoint.position : transform.position;
        PlayAt(pos);
        ApplyObjectTransforms();
    }

    void PlayAt(Vector3 worldPos)
    {
        // ✅ โหมด 3D จริง: ใช้ AudioSource ที่คุณกำหนดจาก Scene
        if (inputAudioSource != null)
        {
            if (moveSourceToSoundPoint)
                inputAudioSource.transform.position = worldPos;

            // บังคับตั้งค่า 3D ให้ตรงตาม Inspector ของ Trigger นี้
            inputAudioSource.spatialBlend = spatialize ? 1f : 0f;
            inputAudioSource.spatialize = spatialize;
            inputAudioSource.minDistance = minDistance;
            inputAudioSource.maxDistance = maxDistance;
            inputAudioSource.rolloffMode = rolloffMode;
            inputAudioSource.volume = volume;

            // เล่นแบบ one-shot ไม่ทับคลิปหลักของ source
            inputAudioSource.PlayOneShot(clip, 1f);

            if (logEvents) Debug.Log($"[AudioTrigger] PlayOneShot '{clip.name}' via InputAudioSource @ {inputAudioSource.transform.position}", this);
            return;
        }

        // fallback: ถ้าไม่ได้ใส่ AudioSource มาก็ยังเล่นได้ (เหมือนเดิม)
        AudioSource.PlayClipAtPoint(clip, worldPos, volume);
        if (logEvents) Debug.Log($"[AudioTrigger] PlayClipAtPoint '{clip.name}' @ {worldPos} (no inputAudioSource)", this);
    }

    void ApplyObjectTransforms()
    {
        if (moveTargets == null || moveTargets.Length == 0)
            return;

        for (int i = 0; i < moveTargets.Length; i++)
        {
            Transform t = moveTargets[i];
            if (!t) continue;

            // เปลี่ยนตำแหน่ง ถ้ามีค่าใน targetPositions
            if (targetPositions != null && i < targetPositions.Length)
            {
                Vector3 p = targetPositions[i];
                if (p != Vector3.zero)
                {
                    if (useLocalSpace) t.localPosition = p;
                    else t.position = p;
                }
            }

            // เปลี่ยน rotation ถ้ามีค่าใน targetRotations
            if (targetRotations != null && i < targetRotations.Length)
            {
                Vector3 euler = targetRotations[i];
                if (euler != Vector3.zero)
                {
                    Quaternion q = Quaternion.Euler(euler);
                    if (useLocalSpace) t.localRotation = q;
                    else t.rotation = q;
                }
            }

            if (logEvents)
                Debug.Log($"[AudioTrigger] Moved '{t.name}'", this);
        }
    }
}
