using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class MasterVolumeSettings : MonoBehaviour, IPointerUpHandler
{
    [Header("UI")]
    [SerializeField] private Slider masterSlider; // 0..100

    [Header("Persistence")]
    [SerializeField] private bool usePlayerPrefs = true;
    [SerializeField] private string prefsKey = "MasterVolPercent";

    [Header("Defaults")]
    [Range(0f, 100f)]
    [SerializeField] private float defaultPercent = 100f;

    bool _initialized;

    void Awake()
    {
        if (!masterSlider)
        {
            Debug.LogWarning("[MasterVolumeSettings] Missing slider reference.");
            return;
        }

        masterSlider.minValue = 0f;
        masterSlider.maxValue = 100f;
        masterSlider.wholeNumbers = false;

        float percent = usePlayerPrefs ? PlayerPrefs.GetFloat(prefsKey, defaultPercent) : defaultPercent;
        percent = Mathf.Clamp(percent, 0f, 100f);

        masterSlider.SetValueWithoutNotify(percent);
        Apply(percent);

        masterSlider.onValueChanged.AddListener(Apply);
        _initialized = true;
    }

    void OnDestroy()
    {
        if (_initialized && masterSlider)
            masterSlider.onValueChanged.RemoveListener(Apply);
    }

    // เรียกทุกครั้งที่ลาก slider (Realtime)
    void Apply(float percent)
    {
        percent = Mathf.Clamp(percent, 0f, 100f);

        // ✅ 100 = 1.0 -> ใช้ baseline volume ของ AudioSource ที่ตั้งไว้ใน Scene
        AudioListener.volume = percent / 100f;

        // เก็บค่าไว้ก่อน (ยังไม่ Save หนัก ๆ)
        if (usePlayerPrefs)
            PlayerPrefs.SetFloat(prefsKey, percent);
    }

    // ✅ เซฟจริงตอน "ปล่อย" slider (ลดหน่วง/กระตุก)
    public void OnPointerUp(PointerEventData eventData)
    {
        if (usePlayerPrefs)
            PlayerPrefs.Save();
    }

    // optional: ปุ่ม Reset
    public void ResetTo100()
    {
        if (!masterSlider) return;
        masterSlider.value = 100f; // จะเรียก Apply ให้อัตโนมัติ
        if (usePlayerPrefs) PlayerPrefs.Save();
    }
}
