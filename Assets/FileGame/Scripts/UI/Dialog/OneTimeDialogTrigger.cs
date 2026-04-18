using System.Collections;
using UnityEngine;
using TMPro;

public class OneTimeDialogTrigger : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMP_Text dialogText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Dialog")]
    [TextArea(2, 5)]
    [SerializeField] private string message = "Text";
    [SerializeField] private float showSeconds = 3f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInTime = 1.2f;
    [SerializeField] private float fadeOutTime = 1.5f;

    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool disableTriggerAfterUse = true;

    private bool hasTriggered = false;
    private Coroutine running;

    private void Awake()
    {
        if (dialogPanel != null)
            dialogPanel.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        hasTriggered = true;

        if (running != null)
            StopCoroutine(running);

        running = StartCoroutine(DialogSequence());

        if (disableTriggerAfterUse)
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    private IEnumerator DialogSequence()
    {
        dialogPanel.SetActive(true);
        dialogText.text = message;

        // Fade In
        yield return Fade(0f, 1f, fadeInTime);

        // Hold
        yield return new WaitForSeconds(showSeconds);

        // Fade Out
        yield return Fade(1f, 0f, fadeOutTime);

        dialogPanel.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0f;
        canvasGroup.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
