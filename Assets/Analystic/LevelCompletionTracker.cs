using UnityEngine;
using Unity.Services.Analytics;

public class LevelCompletionTracker : MonoBehaviour
{
    private float _startTime;
    private string _currentLevelName;
    private bool _isLevel2Started = false;

    void Start()
    {
        // เริ่มจับเวลาด่านที่ 1 ทันทีเมื่อเข้าเกม
        _currentLevelName = "Level_1_Exploration";
        _startTime = Time.time;
        Debug.Log($"⏱️ [Analytics] เริ่มจับเวลา: {_currentLevelName}");
    }

    // ฟังก์ชันสำหรับเรียกใช้จาก Collider (เมื่อจบด่าน 1 และเริ่มด่าน 2 ทันที)
    public void FinishLevel1AndStartLevel2()
    {
        if (_isLevel2Started) return; // ป้องกันการชนซ้ำ

        // 1. ส่งข้อมูลด่านที่ 1
        SendLevelData(_currentLevelName);

        // 2. ตั้งค่าด่านที่ 2 และเริ่มนับเวลาใหม่จาก 0 ตรงจุดนี้
        _currentLevelName = "Level_2_Escape";
        _startTime = Time.time;
        _isLevel2Started = true;

        Debug.Log($"⏱️ [Analytics] จบด่าน 1 และเริ่มจับเวลา: {_currentLevelName}");
    }

    // ฟังก์ชันสำหรับเรียกใช้จากประตูสุดท้าย (จบด่าน 2)
    public void FinishLevel2()
    {
        if (!_isLevel2Started) return;

        // ส่งข้อมูลด่านที่ 2
        SendLevelData(_currentLevelName);
        _isLevel2Started = false; // ปิดการทำงาน
    }

    private void SendLevelData(string levelName)
    {
        float timeTaken = Time.time - _startTime;

        // ⭐️ เปลี่ยนชื่อ Event ตามที่คุณตั้งไว้ใน Cloud แล้วครับ!
        CustomEvent levelData = new CustomEvent("Time_Level_Completed")
        {
            { "LevelName", levelName },
            { "TimeTaken", timeTaken }
        };

        AnalyticsService.Instance.RecordEvent(levelData);
        AnalyticsService.Instance.Flush();

        Debug.Log($"<color=cyan>🏆 [SENT] {levelName} | เวลา: {timeTaken:F2} วินาที</color>");
    }
}