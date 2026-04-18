using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    void Start()
    {
        Debug.Log("<color=yellow>⭐ [เช็คระบบ] CheckpointTrigger ทำงานแล้ว!</color>");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"<color=red><size=14>🚨 มีคนมาชน!: ชื่อ {other.gameObject.name} | ป้ายคือ {other.tag}</size></color>");

        if (other.CompareTag("Player"))
        {
            Debug.Log("<color=green>✅ ใช่ Player จริงๆ! กำลังสั่งตัดจบด่าน 1...</color>");

            var tracker = FindObjectOfType<LevelCompletionTracker>();
            if (tracker != null)
            {
                tracker.FinishLevel1AndStartLevel2();
                gameObject.SetActive(false);
            }
        }
    }
}