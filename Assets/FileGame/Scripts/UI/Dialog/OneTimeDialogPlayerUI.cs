using System.Collections;
using UnityEngine;
using TMPro;

public class OneTimeDialogPlayerUI : MonoBehaviour
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

    bool _hasPlayed = false;
    Coroutine _running;

    private void Awake()
    {
        if (dialogPanel != null) dialogPanel.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public bool HasPlayed => _hasPlayed;

    public void PlayOnce()
    {
        if (_hasPlayed) return;
        _hasPlayed = true;

        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(DialogSequence());
    }

    public void ResetForTest()
    {
        _hasPlayed = false;
    }

    IEnumerator DialogSequence()
    {
        if (dialogPanel != null) dialogPanel.SetActive(true);
        if (dialogText != null) dialogText.text = message;

        // Fade In
        if (canvasGroup != null)
            yield return Fade(0f, 1f, fadeInTime);

        // Hold
        yield return new WaitForSeconds(showSeconds);

        // Fade Out
        if (canvasGroup != null)
            yield return Fade(1f, 0f, fadeOutTime);

        if (dialogPanel != null) dialogPanel.SetActive(false);
        _running = null;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null || duration <= 0f)
        {
            if (canvasGroup != null) canvasGroup.alpha = to;
            yield break;
        }

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
