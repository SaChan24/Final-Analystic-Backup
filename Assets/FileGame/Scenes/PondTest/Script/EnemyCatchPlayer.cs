using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class EnemyCatchPlayer : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onPlayerCaught;

    private void Reset()
    {
        // ให้ collider เป็น trigger โดยอัตโนมัติ
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[EnemyCatchPlayer] Player entered catch zone.");
        onPlayerCaught?.Invoke();
    }
}
