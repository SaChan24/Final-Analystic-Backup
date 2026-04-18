using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class DiaryUI : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup canvasGroup;
    public TMP_Text contentText;

    [Header("Player")]
    public PlayerController3D player;

    [Header("Options")]
    [Tooltip("Hide cursor while diary is open")]
    public bool hideCursorWhileOpen = true;

    [Tooltip("Lock look (camera) while diary is open")]
    public bool lockLookWhileOpen = true;

    /// <summary>
    /// มีหน้า Diary ใด ๆ เปิดอยู่หรือไม่ (เผื่อสคริปต์อื่นเอาไปเช็ก)
    /// </summary>
    public static bool AnyDiaryOpen { get; private set; }

    /// <summary>
    /// เฟรมนี้ ESC ถูกใช้ไปแล้วโดย Diary หรือยัง
    /// (ใช้กันไม่ให้ PauseMenu รับ ESC เฟรมเดียวกัน)
    /// </summary>
    public static bool EscapeUsedThisFrame => _lastEscFrame == Time.frameCount;

    static int _lastEscFrame = -1;

    bool _isOpen;
    float _prevSensX;
    float _prevSensY;
    bool _hasSavedLook;

    CursorLockMode _prevCursorLock;
    bool _prevCursorVisible;

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!contentText) contentText = GetComponentInChildren<TMP_Text>();

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        _isOpen = false;
    }

    /// <summary>
    /// แสดงหน้าไดอารี่พร้อมข้อความที่ส่งมา
    /// </summary>
    public void ShowDiary(string text)
    {
        if (!canvasGroup || !contentText) return;

        contentText.text = text;

        if (_isOpen)
            return;

        _isOpen = true;
        AnyDiaryOpen = true;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // เซฟสถานะ cursor เดิม
        _prevCursorLock = Cursor.lockState;
        _prevCursorVisible = Cursor.visible;

        if (hideCursorWhileOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // ล็อกการหมุนกล้อง
        _hasSavedLook = false;
        if (player && lockLookWhileOpen)
        {
            _prevSensX = player.sensX;
            _prevSensY = player.sensY;
            _hasSavedLook = true;

            player.sensX = 0f;
            player.sensY = 0f;
        }
    }

    void Update()
    {
        if (!_isOpen) return;

        // ปิดด้วย ESC
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.Escape))
#endif
        {
            // บันทึกว่าเฟรมนี้ ESC ถูกใช้ไปแล้วโดย Diary
            _lastEscFrame = Time.frameCount;
            CloseDiary();
        }
    }

    public void CloseDiary()
    {
        if (!_isOpen) return;

        _isOpen = false;
        AnyDiaryOpen = false;

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // คืน sens กล้อง
        if (player && lockLookWhileOpen && _hasSavedLook)
        {
            player.sensX = _prevSensX;
            player.sensY = _prevSensY;
            _hasSavedLook = false;
        }

        // คืน cursor state
        if (hideCursorWhileOpen)
        {
            Cursor.lockState = _prevCursorLock;
            Cursor.visible = _prevCursorVisible;
        }
    }
}
