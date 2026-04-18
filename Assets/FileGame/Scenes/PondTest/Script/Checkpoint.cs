using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool oneTimeUse = true;

    private bool activated = false;

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
        if (activated && oneTimeUse) return;

        var player = other.GetComponent<PlayerController3D>();
        if (player == null) return;

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SetCheckpoint(player);
            activated = true;
        }
    }
}
