using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    public GameObject menuRoot;        // กล่อง UI ของ Pause Menu
    public CanvasGroup menuGroup;      // ถ้ามี: ใช้เฟดนุ่ม ๆ ตอนเปิด/ปิด
    [Range(1f, 30f)]
    public float fadeSpeed = 12f;

    [Header("Input")]
    public InputActionReference pauseAction; // ปุ่ม Pause (เช่น ESC ใน InputSystem)
#if ENABLE_INPUT_SYSTEM
    public Key fallbackKey = Key.Escape;     // ถ้าไม่มี Action
#else
    public KeyCode legacyKey = KeyCode.Escape;
#endif

    [Header("Flow")]
    public string mainMenuSceneName = ""; // ใส่ชื่อซีนเมนูหลัก ถ้าไม่ใช้ปล่อยว่าง
    public int mainMenuSceneIndex = -1;   // หรือใช้ index แทน (>= 0)
    public bool pauseAudio = true;

    [Header("Player Control Lock")]
    public MonoBehaviour[] scriptsToDisable; // พวก PlayerController3D / กล้อง ฯลฯ ที่ต้องปิดตอน Pause

    [Header("Cursor")]
    public bool unlockCursorOnPause = true;

    [Header("Debug")]
    public bool debugLogs = false;

    bool _isPaused;
    float _prevTimeScale = 1f;

#if ENABLE_INPUT_SYSTEM
    Keyboard _kb;
#endif

    void Awake()
    {
        if (menuRoot) menuRoot.SetActive(false);
        if (menuGroup)
        {
            menuGroup.alpha = 0f;
            menuGroup.blocksRaycasts = false;
            menuGroup.interactable = false;
        }
#if ENABLE_INPUT_SYSTEM
        _kb = Keyboard.current;
#endif
    }

    void OnEnable()  => pauseAction?.action?.Enable();
    void OnDisable() => pauseAction?.action?.Disable();

    void Update()
    {
        // ถ้า Diary เปิดอยู่ → ไม่ให้ PauseMenu ทำงานเลย
        if (DiaryUI.AnyDiaryOpen)
            return;

        // ถ้าเฟรมนี้ ESC ถูกใช้ไปโดย Diary แล้ว → ข้าม ไม่ต้องรับซ้ำ
        if (DiaryUI.EscapeUsedThisFrame)
            return;

        // เช็กปุ่ม Pause
        if (PressedPauseThisFrame())
            TogglePause();

        // ถ้ามี CanvasGroup → ทำเฟด (นุ่มกว่าเปิด/ปิดทีเดียว)
        if (menuGroup)
        {
            float target = _isPaused ? 1f : 0f;
            if (!Mathf.Approximately(menuGroup.alpha, target))
            {
                float k = 1f - Mathf.Exp(-fadeSpeed * Time.unscaledDeltaTime);
                menuGroup.alpha = Mathf.Lerp(menuGroup.alpha, target, k);
            }
        }
    }

    bool PressedPauseThisFrame()
    {
        if (pauseAction && pauseAction.action != null)
            return pauseAction.action.WasPressedThisFrame();

#if ENABLE_INPUT_SYSTEM
        return _kb != null && _kb[fallbackKey].wasPressedThisFrame;
#else
        return Input.GetKeyDown(legacyKey);
#endif
    }

    public void TogglePause()
    {
        if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;

        _prevTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f;

        if (pauseAudio) AudioListener.pause = true;

        if (menuRoot) menuRoot.SetActive(true);
        if (menuGroup)
        {
            menuGroup.blocksRaycasts = true;
            menuGroup.interactable = true;
        }

        SetScriptsEnabled(false);

        if (unlockCursorOnPause)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (debugLogs) Debug.Log("[PauseMenu] Paused");
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;

        Time.timeScale = _prevTimeScale;
        if (pauseAudio) AudioListener.pause = false;

        if (menuGroup)
        {
            menuGroup.blocksRaycasts = false;
            menuGroup.interactable = false;
        }
        else if (menuRoot)
        {
            // ถ้าไม่มี CanvasGroup ให้ปิด GameObject ไปเลย
            menuRoot.SetActive(false);
        }

        SetScriptsEnabled(true);

        if (unlockCursorOnPause)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (debugLogs) Debug.Log("[PauseMenu] Resumed");
    }

    void SetScriptsEnabled(bool enabled)
    {
        if (scriptsToDisable == null) return;

        for (int i = 0; i < scriptsToDisable.Length; i++)
        {
            if (scriptsToDisable[i])
                scriptsToDisable[i].enabled = enabled;
        }
    }

    // ===== Buttons =====

    public void OnClick_Continue()
    {
        Resume();
    }

    public void OnClick_MainMenu()
    {
        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else if (mainMenuSceneIndex >= 0)
        {
            SceneManager.LoadScene(mainMenuSceneIndex);
        }
        else
        {
            Debug.LogWarning("[PauseMenu] MainMenu scene is not set.");
        }
    }

    public void OnClick_Exit()
    {
        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
