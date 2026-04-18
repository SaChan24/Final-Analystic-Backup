using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class DoorCodeLock : MonoBehaviour
{
    [Header("=== Door Code Settings ===")]
    [SerializeField] private string correctCode = "1234";
    [SerializeField] private int codeLength = 4;

    [Header("=== UI Panel ===")]
    [Tooltip("Panel หลักของหน้ารหัส (พื้นหลัง + ตัวเลข + hint ข้างใน panel)")]
    [SerializeField] private GameObject codePanelRoot;

    [Tooltip("Text แสดงตัวเลขที่พิมพ์อยู่")]
    [SerializeField] private TMP_Text codeDisplayText;

    [Tooltip("Hint ที่อยู่ใน Panel (เช่น 'พิมพ์ตัวเลข 0-9 แล้วกด Enter...')")]
    [SerializeField] private TMP_Text panelHintText;

    [Header("=== Door Hint (นอก Panel) ===")]
    [Tooltip("ข้อความเล็ก ๆ ที่โผล่ตอนยืนหน้า door เช่น 'กด E เพื่อใส่รหัสประตู'")]
    [SerializeField] private TMP_Text doorHintText;

    [Header("=== Door Animator ===")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerName = "Open";

    [Header("=== Audio ===")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;
    [SerializeField] private AudioClip typingClip;

    [Header("=== Events ===")]
    public UnityEvent onDoorUnlocked;

    [Header("=== Camera Control (optional) ===")]
    [SerializeField] private Behaviour cameraLookScript;

    [Header("=== Scripts to Disable While Typing (optional) ===")]
    [Tooltip("สคริปต์อื่น ๆ ที่ต้องการปิดชั่วคราวตอนใส่รหัส เช่น PlayerController3D, MapViewerHold ฯลฯ")]
    [SerializeField] private Behaviour[] componentsToDisableWhileTyping;

    private bool playerInRange = false;
    private bool isUnlocked = false;
    private bool isTyping = false;
    private string currentInput = "";

    private void Start()
    {
        // ปิดทุกอย่างตอนเริ่ม
        if (codePanelRoot != null) codePanelRoot.SetActive(false);
        if (codeDisplayText != null) codeDisplayText.text = "";
        if (panelHintText != null)
        {
            panelHintText.text = "";
            panelHintText.gameObject.SetActive(false);
        }
        if (doorHintText != null)
        {
            doorHintText.text = "";
            doorHintText.gameObject.SetActive(false);
        }

        SetTypingModeDependencies(false);
    }

    private void Update()
    {
        if (isUnlocked) return;

        if (playerInRange)
        {
            HandleInteractionInput();

            if (isTyping)
            {
                HandleCodeTyping();
            }
        }
    }

    // ================== INPUT ==================

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isTyping)
            {
                if (doorHintText != null)
                {
                    doorHintText.text = "";
                    doorHintText.gameObject.SetActive(false);
                }
                StartTypingMode();
            }
            else
            {
                CancelTypingMode();
            }
        }
    }

    private void StartTypingMode()
    {
        isTyping = true;
        currentInput = "";

        // เปิด Panel รหัส
        if (codePanelRoot != null)
            codePanelRoot.SetActive(true);

        if (codeDisplayText != null)
            codeDisplayText.text = "";

        // ซ่อน door hint ไปเลย (แก้ปัญหาข้อที่ 2 ไม่ให้แสดงพร้อมกัน)
        //

        // แสดง hint ใน Panel อย่างเดียว
        if (panelHintText != null)
        {
            panelHintText.gameObject.SetActive(true);
            panelHintText.text = "พิมพ์ตัวเลข 0-9 แล้วกด Enter\nกด E อีกครั้งเพื่อยกเลิก";
        }

        // ล็อกกล้อง
        if (cameraLookScript != null)
            cameraLookScript.enabled = false;

        // ปิดสคริปต์อื่นชั่วคราว
        SetTypingModeDependencies(true);
    }

    private void CancelTypingMode()
    {
        isTyping = false;
        currentInput = "";

        // ปิด Panel รหัส
        if (codePanelRoot != null)
            codePanelRoot.SetActive(false);

        if (codeDisplayText != null)
            codeDisplayText.text = "";

        // ปิด hint ใน Panel
        if (panelHintText != null)
        {
            panelHintText.text = "";
            panelHintText.gameObject.SetActive(false);
        }

        // ยังอยู่ใน Trigger แต่ไม่ได้ใส่รหัสแล้ว → แสดง hint ข้างนอกประตูอย่างเดียว
        if (playerInRange && doorHintText != null)
        {
            doorHintText.gameObject.SetActive(true);
            doorHintText.text = "กด E เพื่อใส่รหัสประตู";
        }

        // ปลดล็อกกล้อง
        if (cameraLookScript != null)
            cameraLookScript.enabled = true;

        // เปิดสคริปต์อื่นกลับมา
        SetTypingModeDependencies(false);
    }

    private void HandleCodeTyping()
    {
        // ตัวเลข 0–9
        for (KeyCode key = KeyCode.Alpha0; key <= KeyCode.Alpha9; key++)
        {
            if (Input.GetKeyDown(key))
            {
                if (currentInput.Length >= codeLength) return;

                char digit = (char)('0' + (key - KeyCode.Alpha0));
                currentInput += digit;

                if (typingClip != null && audioSource != null)
                    audioSource.PlayOneShot(typingClip);

                if (codeDisplayText != null)
                    codeDisplayText.text = currentInput;

                return;
            }
        }

        // Backspace
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                if (codeDisplayText != null)
                    codeDisplayText.text = currentInput;
            }
        }

        // Enter = เช็กรหัส
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            CheckCode();
        }
    }

    private void CheckCode()
    {
        if (currentInput == correctCode)
        {
            // ใช้ Coroutine เพื่อให้เสียง correct เล่นจบก่อนค่อย Invoke (แก้ปัญหาข้อที่ 1)
            StartCoroutine(CorrectRoutine());
        }
        else
        {
            // รหัสผิด
            if (audioSource != null && wrongClip != null)
                audioSource.PlayOneShot(wrongClip);

            currentInput = "";
            if (codeDisplayText != null)
                codeDisplayText.text = "";

            if (panelHintText != null)
                panelHintText.text = "รหัสผิด! พิมพ์ใหม่แล้วกด Enter\nกด E อีกครั้งเพื่อยกเลิก";
        }
    }

    private System.Collections.IEnumerator CorrectRoutine()
    {
        isUnlocked = true;
        isTyping = false;

        // เล่นเสียง correct ก่อน
        float waitTime = 0.15f;
        if (audioSource != null && correctClip != null)
        {
            audioSource.PlayOneShot(correctClip);
            waitTime = Mathf.Max(correctClip.length, 0.15f);
        }

        // เปิดประตู
        if (doorAnimator != null && !string.IsNullOrEmpty(openTriggerName))
            doorAnimator.SetTrigger(openTriggerName);

        // ปิด UI ทั้งหมด
        if (codePanelRoot != null) codePanelRoot.SetActive(false);
        if (codeDisplayText != null) codeDisplayText.text = "";
        if (panelHintText != null)
        {
            panelHintText.text = "";
            panelHintText.gameObject.SetActive(false);
        }
        if (doorHintText != null)
        {
            doorHintText.text = "";
            doorHintText.gameObject.SetActive(false);
        }

        // ปลดล็อกกล้อง + เปิดสคริปต์อื่นกลับมา
        if (cameraLookScript != null)
            cameraLookScript.enabled = true;
        SetTypingModeDependencies(false);

        // รอให้เสียงเล่นจบก่อนค่อยยิง Event (กันโดนตัดเสียง)
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        onDoorUnlocked?.Invoke();

        // ค่อยปิดสคริปต์ตัวนี้
        enabled = false;
    }

    // ================== TRIGGER ==================

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (!isTyping && doorHintText != null)
        {
            doorHintText.gameObject.SetActive(true);
            doorHintText.text = "กด E เพื่อใส่รหัสประตู";
        }

        // กัน panel ค้างจากก่อนหน้า
        if (!isTyping && codePanelRoot != null)
            codePanelRoot.SetActive(false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        isTyping = false;
        currentInput = "";

        if (codePanelRoot != null) codePanelRoot.SetActive(false);
        if (codeDisplayText != null) codeDisplayText.text = "";
        if (panelHintText != null)
        {
            panelHintText.text = "";
            panelHintText.gameObject.SetActive(false);
        }
        if (doorHintText != null)
        {
            doorHintText.text = "";
            doorHintText.gameObject.SetActive(false);
        }

        if (cameraLookScript != null)
            cameraLookScript.enabled = true;

        SetTypingModeDependencies(false);
    }

    // ================== HELPER ==================

    private void SetTypingModeDependencies(bool typingActive)
    {
        if (componentsToDisableWhileTyping == null) return;

        bool enable = !typingActive;

        for (int i = 0; i < componentsToDisableWhileTyping.Length; i++)
        {
            var comp = componentsToDisableWhileTyping[i];
            if (comp == null) continue;
            comp.enabled = enable;
        }
    }
}
