using UnityEngine;
using Unity.Services.Analytics;

public class LoreItemAnalytics : MonoBehaviour
{
    [Header("ตั้งชื่อไอเทมชิ้นนี้ (ห้ามซ้ำกัน)")]
    public string loreName = "Diary_Page_1";

    private bool _isCollected = false;

    // ฟังก์ชันนี้เอาไว้เรียกตอนที่ผู้เล่น "กดหยิบ/อ่าน" ไอเทม
    public void RecordLorePickup()
    {
        // ป้องกันการกดหยิบซ้ำแล้วส่งข้อมูลเบิ้ล
        if (_isCollected) return;

        CustomEvent loreEvent = new CustomEvent("Lore_Collected")
        {
            { "LoreName", loreName }
        };

        AnalyticsService.Instance.RecordEvent(loreEvent);
        AnalyticsService.Instance.Flush();

        Debug.Log($"<color=#FFD700>📜 [SENT] ผู้เล่นเก็บเนื้อเรื่อง: {loreName}</color>");

        _isCollected = true; // ล็อคไว้ว่าส่งข้อมูลชิ้นนี้ไปแล้ว
    }
}